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
	uSyncActionView,
	uSyncHandlerSetSettings,
	uSyncSettings,
	uSyncActionRepository,
	uSyncConstants,
	uSyncIconRegistry,
	SyncPerformActionOptions,
} from '@jumoo/uSync';
import uSyncSignalRContext from '../signalr/signalr.context';
import {
	UMB_WORKSPACE_CONTEXT,
	UmbWorkspaceContext,
} from '@umbraco-cms/backoffice/workspace';

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
	#uSyncIconRegistry: uSyncIconRegistry;
	#signalRContext: uSyncSignalRContext | null = null;

	/**
	 * list of actions that have been returned from the process
	 */
	#actions = new UmbArrayState<SyncActionGroup>([], (x) => x.key);
	public readonly actions = this.#actions.asObservable();

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

	/**
	 * Flag to say that the last run has been completed (so results will show)
	 */
	#completed = new UmbBooleanState(false);
	public readonly completed = this.#completed.asObservable();

	/**
	 * The results of a run.
	 */
	#results = new UmbArrayState<uSyncActionView>([], (x) => x.name);
	public readonly results = this.#results.asObservable();

	/**
	 * Current settings for uSync
	 */
	#settings = new UmbObjectState<uSyncSettings | undefined>(undefined);
	public readonly settings = this.#settings?.asObservable();

	/**
	 * Handler settings object
	 */
	#handlerSettings = new UmbObjectState<uSyncHandlerSetSettings | undefined>(undefined);
	public readonly handlerSettings = this.#handlerSettings?.asObservable();

	#legacy = new UmbObjectState<SyncLegacyCheckResponse | undefined>(undefined);
	public readonly legacy = this.#legacy?.asObservable();

	constructor(host: UmbControllerHost) {
		super(host);

		this.provideContext(USYNC_CORE_CONTEXT_TOKEN, this);
		this.provideContext(UMB_WORKSPACE_CONTEXT, this);

		this.#repository = new uSyncActionRepository(this);
		this.#uSyncIconRegistry = new uSyncIconRegistry();
		this.#uSyncIconRegistry.attach(this);

		this.#signalRContext = new uSyncSignalRContext(this);
	}

	/**
	 * Return the current actions from the repository
	 */
	async getActions() {
		const { data } = await this.#repository.getActions();

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
	async getDefaultHandlerSetSettings() {
		const { data } = await this.#repository.getHandlerSettings('Default');

		if (data) {
			this.#handlerSettings.setValue(data);
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

		var complete = false;
		var id = '';
		var step: number = 0;

		do {
			const { data } = await this.#repository.performAction({
				id: id,
				action: options.action,
				group: options.group.key,
				force: options.force,
				clean: options.clean,
				step: step,
				clientId: clientId,
			});

			if (data) {
				step++;

				let summary = data.status ?? [];

				this.#workingActions.setValue(summary);

				id = data.requestId;
				complete = data.complete;

				if (complete) {
					this.#results.setValue(data?.actions ?? []);
				}
			} else {
				complete = true;
			}
		} while (!complete);

		this.#completed.setValue(true);
		this.#working.setValue(false);
	}
}

export default uSyncWorkspaceContext;

export const USYNC_CORE_CONTEXT_TOKEN = new UmbContextToken<uSyncWorkspaceContext>(
	'uSyncWorkspaceContext',
);
