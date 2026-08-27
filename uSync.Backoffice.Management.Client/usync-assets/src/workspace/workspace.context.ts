import {
	UmbArrayState,
	UmbBooleanState,
	UmbObjectState,
} from '@umbraco-cms/backoffice/observable-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import {
	PerformActionResponse,
	SyncActionGroup,
	SyncHandlerSummary,
	SyncLegacyCheckResponse,
	USyncActionView,
	USyncHandlerSetSettings,
	USyncSettings,
	uSyncActionRepository,
	uSyncConstants,
	SyncPerformActionOptions,
	SyncSelectableSet,
	SyncFileVersionCheckResult,
} from '@jumoo/uSync';
import uSyncSignalRContext from '../signalr/signalr.context';
import {
	UMB_WORKSPACE_CONTEXT,
	UmbWorkspaceContext,
} from '@umbraco-cms/backoffice/workspace';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import { USYNC_IMPORT_MODAL } from './dialogs';

/**
 * Status-poll interval bounds while a background run is active and SignalR
 * isn't connected. Kept modest (rather than a tight fixed interval) because
 * each poll is an authenticated DB round-trip that can contend with the
 * run's own writes - see the SQLite notes in the background-mode docs.
 */
const POLL_INTERVAL_MIN_MS = 5000;
const POLL_INTERVAL_MAX_MS = 20000;

/**
 * Context for getting and seting up actions.
 */
export class uSyncWorkspaceContext
	extends UmbControllerBase
	implements UmbWorkspaceContext
{
	public readonly workspaceAlias: string = uSyncConstants.workspace.alias;

	getEntityType(): string {
		return uSyncConstants.workspace.rootElement;
	}

	#repository: uSyncActionRepository;
	#signalRContext: uSyncSignalRContext | null = null;

	/**
	 * poll timer used while a background operation is running - this is the
	 * safety net (and the page-reload reattach mechanism) alongside SignalR,
	 * which is the fast path for live progress.
	 */
	#pollTimer: ReturnType<typeof setTimeout> | null = null;

	/**
	 * true when the current background run needs to trigger a file download
	 * once it completes (export-to-file can't download until the export has
	 * actually finished writing, which - in background mode - is not when
	 * the enqueue call returns).
	 */
	#pendingDownload = false;

	/**
	 * list of actions that have been returned from the process
	 */
	#actions = new UmbArrayState<SyncActionGroup>([], (x) => x.key);
	public readonly actions = this.#actions.asObservable();
	private _currentSetName: string = '';

	/**
	 * the working group name
	 */
	#workingGroup = new UmbObjectState<SyncActionGroup | undefined>(undefined);
	public readonly workingGroup = this.#workingGroup.asObservable();

	/**
	 * The summary objects that show the handler boxes
	 */
	#workingActions = new UmbArrayState<SyncHandlerSummary>([], (x) => x.name);
	public readonly currentAction = this.#workingActions.asObservable();

	/**
	 * Flag to say if things are currently being processed
	 */
	#working = new UmbBooleanState(false);
	public readonly working = this.#working.asObservable();

	#inBackground = new UmbBooleanState(false);
	public readonly inBackground = this.#inBackground.asObservable();

	/**
	 * Flag to say that the last run has been completed (so results will show)
	 */
	#completed = new UmbBooleanState(false);
	public readonly completed = this.#completed.asObservable();

	/**
	 * The results of a run.
	 */
	#results = new UmbArrayState<USyncActionView>([], (x) => x.name);
	public readonly results = this.#results.asObservable();

	/**
	 * Current settings for uSync
	 */
	#settings = new UmbObjectState<USyncSettings | undefined>(undefined);
	public readonly settings = this.#settings?.asObservable();

	/**
	 * Handler settings object
	 */
	#handlerSettings = new UmbObjectState<USyncHandlerSetSettings | undefined>(undefined);
	public readonly handlerSettings = this.#handlerSettings?.asObservable();

	#handlerSets = new UmbArrayState<SyncSelectableSet>([], (x) => x);
	public readonly sets = this.#handlerSets.asObservable();

	#legacy = new UmbObjectState<SyncLegacyCheckResponse | undefined>(undefined);
	public readonly legacy = this.#legacy?.asObservable();

	#syncFileInfo = new UmbObjectState<SyncFileVersionCheckResult | undefined>(undefined);
	public readonly syncFileInfo = this.#syncFileInfo?.asObservable();

	constructor(host: UmbControllerHost) {
		super(host);

		this.provideContext(USYNC_CORE_CONTEXT_TOKEN, this);
		this.provideContext(UMB_WORKSPACE_CONTEXT, this);

		this.#repository = new uSyncActionRepository(this);

		this.#signalRContext = new uSyncSignalRContext(this);

		this.observe(this.#signalRContext.connected, (connected) => {
			console.debug('SignalR connected', connected);
		});

		this.observe(this.#signalRContext.complete, (complete) => {
			if (!complete) return;

			if (complete.success) {
				this.#onRunComplete(complete.actions ?? []);
			}
		});

		// on load, ask the server if a background run is already active -
		// this is what lets the dashboard reattach after a page reload.
		this.#checkForRunningOperation();
	}

	hostDisconnected(): void {
		super.hostDisconnected();
		this.#stopPolling();
	}

	/**
	 * Return the current actions from the repository
	 */
	async getActions(setName: string) {
		const { data } = await this.#repository.getActions(setName);

		if (data) {
			this.#actions.setValue(data);
			this._currentSetName = setName;
		}
	}

	/**
	 * Get the current uSync settings
	 */
	async getSettings() {
		const { data } = await this.#repository.getSettings();

		if (data) {
			this.#settings.setValue(data);
		}

		return data;
	}

	async getAddons() {
		const { data } = await this.#repository.getAddons();
		return data;
	}

	async getSyncFileInfo() {
		const { data } = await this.#repository.getSyncFileInfo();
		if (data) {
			this.#syncFileInfo.setValue(data);
		}
		return data;
	}

	/**
	 * Check to see if there is a legacy uSync folder on disk.
	 */
	async checkLegacy() {
		const { data } = await this.#repository.checkLegacy();
		if (data) {
			this.#legacy.setValue(data);
		}

		return data;
	}

	async ignoreLegacy() {
		const { data } = await this.#repository.ignoreLegacy();
		return data ?? false;
	}

	async copyLegacy() {
		const { data } = await this.#repository.copyLegacy();
		return data ?? false;
	}

	/**
	 * Get handler defaults.
	 */
	async getDefaultHandlerSetSettings(setName: string) {
		const { data } = await this.#repository.getHandlerSettings(setName);

		if (data) {
			this.#handlerSettings.setValue(data);
		}
	}

	async getHandlerSets() {
		const { data } = await this.#repository.getSets();
		if (data) {
			this.#handlerSets.setValue(data);
		}
	}

	/**
	 * Perform an action (e.g import, export, etc) with options
	 * @param options options for the action
	 */
	async performAction(options: SyncPerformActionOptions) {
		var clientId = this.#signalRContext?.getClientId() ?? '';

		this.#working.setValue(true);
		this.#completed.setValue(false);
		this.#results.setValue([]);
		this.#workingGroup.setValue(options.group);

		var complete = false;
		var id = '';
		var step: number = 0;
		var lastResponse: PerformActionResponse | undefined;

		// pre-action checks, for files.
		if (options.file && options.action === 'Import') {
			// imports need to open the file dialog to get the file first.
			const uploadResult = await this.uploadFile();
			if (!uploadResult) {
				this.#completed.setValue(true);
				this.#working.setValue(false);
				this.#results.setValue([]);
				return;
			}
		}

		do {
			const { data } = await this.#repository.performAction({
				id: id,
				set: options.setName,
				action: options.action,
				group: options.group.key,
				force: options.force,
				clean: options.clean,
				file: options.file,
				step: step,
				clientId: clientId,
			});

			if (data) {
				step++;

				let summary = data.status ?? [];

				this.#workingActions.setValue(summary);

				id = data.requestId;
				complete = data.complete;
				lastResponse = data;

				this.#inBackground.setValue(data.inBackground);

				if (complete && !data.inBackground) {
					this.#results.setValue(data?.actions ?? []);
					this.getActions(this._currentSetName);
				}
			} else {
				complete = true;
			}
		} while (!complete);

		if (lastResponse?.message) {
			// the run couldn't be started (e.g. one is already in progress) -
			// nothing happened, so don't report a completed run.
			console.warn('[uSync]', lastResponse.message);
			this.#working.setValue(false);
			this.#completed.setValue(false);
			this.#inBackground.setValue(false);
			return;
		}

		if (lastResponse?.inBackground && lastResponse?.operationId) {
			// enqueued - poll (and listen via SignalR) until it completes.
			// export-to-file must wait until then too, it can't download yet.
			this.#startPolling(lastResponse.operationId, options);
			return;
		}

		// normal (stepped, foreground) mode - already complete.
		if (options.file && options.action === 'Export') {
			await this.downloadFile();
		}

		this.#completed.setValue(true);
		this.#working.setValue(false);
	}

	/**
	 * Ask the server if a background run is already active, and if so,
	 * reattach to it (used on load, to recover from a page reload).
	 */
	async #checkForRunningOperation() {
		const { data } = await this.#repository.getRunningOperation();
		if (!data?.operationId) return;

		this.#working.setValue(true);
		this.#completed.setValue(false);
		this.#inBackground.setValue(true);

		this.#startPolling(data.operationId);
	}

	#startPolling(operationId: string, options?: SyncPerformActionOptions) {
		this.#stopPolling();

		this.#pendingDownload =
			options?.file === true && options?.action === 'Export';

		let interval = POLL_INTERVAL_MIN_MS;

		const poll = async () => {
			// SignalR is the fast path for progress/completion - polling is only
			// the fallback (initial reattach before the socket is up, or if it
			// drops mid-run). Every poll is an authenticated request, and on
			// SQLite a background run can hold locks long enough that a steady
			// stream of unrelated queries starts timing out, so we only spend
			// that request when there's no live connection doing the job for us.
			if (!this.#signalRContext?.getConnected()) {
				const { data } =
					await this.#repository.getOperationStatus(operationId);

				if (data) {
					this.#workingActions.setValue(data.status ?? []);

					if (data.complete) {
						this.#onRunComplete(
							data.actions ?? [],
							data.operationStatus === 'Success',
							data.message,
						);
						return;
					}
				}

				// back off while a run drags on, so a long import doesn't sustain
				// a steady stream of extra queries against the same database.
				interval = Math.min(interval * 1.5, POLL_INTERVAL_MAX_MS);
			}

			this.#pollTimer = setTimeout(poll, interval);
		};

		this.#pollTimer = setTimeout(poll, interval);
	}

	#stopPolling() {
		if (this.#pollTimer) {
			clearTimeout(this.#pollTimer);
			this.#pollTimer = null;
		}
	}

	/**
	 * called once, however the completion was learned about - SignalR (fast
	 * path, live connection) or polling (safety net, and what makes reattach
	 * after a reload work). Idempotent, since both can fire for the same run.
	 *
	 * @param success false for a run that failed or went stale (e.g. the
	 *   server crashed/restarted mid-run) - the results shown are whatever
	 *   was captured before that happened, not a completed set.
	 */
	#onRunComplete(
		actions: USyncActionView[],
		success: boolean = true,
		message?: string | null,
	) {
		if (this.#completed.getValue()) return;

		this.#stopPolling();

		if (!success) {
			console.warn('[uSync]', message ?? 'The background run did not complete successfully.');
		}

		this.#results.setValue(actions);
		this.#completed.setValue(true);
		this.#working.setValue(false);
		this.#inBackground.setValue(false);
		this.getActions(this._currentSetName);

		if (success && this.#pendingDownload) {
			this.#pendingDownload = false;
			this.downloadFile();
		}
	}

	/**
	 * Manually clear a stuck "running in background" state - a workaround for
	 * when the server that was running the sync crashed or restarted and this
	 * client is left showing it as still active. This only resets what THIS
	 * client shows; it does not (and cannot - Umbraco's long-running-operation
	 * service has no cancel API) force the server-side operation to end. If
	 * the run really is still active elsewhere, starting a new one will still
	 * be rejected until the server-side run actually finishes or its
	 * expiration window passes (a few minutes by default).
	 */
	dismissBackgroundRun() {
		this.#stopPolling();
		this.#pendingDownload = false;
		this.#completed.setValue(false);
		this.#working.setValue(false);
		this.#inBackground.setValue(false);
		this.#workingActions.setValue([]);
	}

	async uploadFile() {
		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		const importModal = modalManager.open(this, USYNC_IMPORT_MODAL, {
			data: {},
		});

		const data = await importModal.onSubmit().catch(() => {
			return false;
		});

		if (!data) return false;

		return true;
	}

	async downloadFile() {
		const response = await this.#repository.downloadFile();

		if (!response) return;

		console.log('Downloading file', response);

		const url = window.URL.createObjectURL(response);

		const download = document.createElement('a');
		download.href = url;
		download.download = this.#getFileName();
		document.body.appendChild(download);
		download.dispatchEvent(new MouseEvent('click'));
		download.remove();
		window.URL.revokeObjectURL(url);
	}

	#getFileName() {
		const now = new Date();
		const year = now.getUTCFullYear().toString();
		const month = (now.getUTCMonth() + 1).toString().padStart(2, '0');
		const day = now.getUTCDate().toString().padStart(2, '0');
		const hours = now.getUTCHours().toString().padStart(2, '0');
		const minutes = now.getUTCMinutes().toString().padStart(2, '0');
		const seconds = now.getUTCSeconds().toString().padStart(2, '0');
		const timestamp = `${year}${month}${day}_${hours}${minutes}${seconds}`;
		return `usync_export_${timestamp}.zip`;
	}

	async importSingle(item: USyncActionView) {
		if (!item) return;

		const data = await this.#repository.importSingle(item);
		return data;
	}
}

export default uSyncWorkspaceContext;

export const USYNC_CORE_CONTEXT_TOKEN = new UmbContextToken<uSyncWorkspaceContext>(
	'uSyncWorkspaceContext',
);
