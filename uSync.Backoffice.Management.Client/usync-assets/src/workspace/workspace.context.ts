import {
	UmbArrayState,
	UmbBooleanState,
	UmbObjectState,
} from '@umbraco-cms/backoffice/observable-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import {
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
} from '@jumoo/uSync';
import uSyncSignalRContext from '../signalr/signalr.context';
import {
	UMB_WORKSPACE_CONTEXT,
	UmbWorkspaceContext,
} from '@umbraco-cms/backoffice/workspace';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import { USYNC_IMPORT_MODAL } from './dialogs';

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
	 * list of actions that have been returned from the process
	 */
	#actions = new UmbArrayState<SyncActionGroup>([], (x) => x.key);
	public readonly actions = this.#actions.asObservable();

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
				this.#completed.setValue(true);
				this.#working.setValue(false);
				this.#inBackground.setValue(false);

				this.#results.setValue(complete.actions ?? []);
			}
		});
	}

	/**
	 * Return the current actions from the repository
	 */
	async getActions(setName: string) {
		const { data } = await this.#repository.getActions(setName);

		if (data) {
			this.#actions.setValue(data);
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

				this.#inBackground.setValue(data.inBackground);

				if (complete) {
					this.#results.setValue(data?.actions ?? []);
				}
			} else {
				complete = true;
			}
		} while (!complete);

		// post action
		if (options.file && options.action === 'Export') {
			// post export , open the dialog offer the download.
			await this.downloadFile(id);
		}

		if (!this.#inBackground.getValue()) {
			this.#completed.setValue(true);
			this.#working.setValue(false);
		}
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

	async downloadFile(requestId: string) {
		const response = await this.#repository.downloadFile(requestId);

		if (!response) return;

		const url = window.URL.createObjectURL(response);

		const download = document.createElement('a');
		download.href = url;
		download.download = 'usync-export.zip';
		document.body.appendChild(download);
		download.dispatchEvent(new MouseEvent('click'));
		download.remove();
		window.URL.revokeObjectURL(url);
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
