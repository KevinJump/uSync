import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import {
	uSyncActionDataSource,
	uSyncSettingsDataSource,
	uSyncMigrationDataSource,
} from '@jumoo/uSync';

/**
 * Request object when peforming an action.
 */
export type SyncPerformRequest = {
	/** Id of the request */
	id: string;

	/** group (e.g settings, content) */
	group: string;

	/** action (report, export, etc) */
	action: string;

	/** current step number */
	step: number;

	/** force (import) */
	force?: boolean;

	/** clean disk first (export) */
	clean?: boolean;

	/** name of the set to use */
	set?: string;

	/** signalR client id */
	clientId: string;
};

/**
 * Repository for all things actions.
 */
export class uSyncActionRepository extends UmbControllerBase {
	#actionDataSource: uSyncActionDataSource;
	#settingsDataSource: uSyncSettingsDataSource;
	#migrartionDataSource: uSyncMigrationDataSource;

	constructor(host: UmbControllerHost) {
		super(host);
		this.#actionDataSource = new uSyncActionDataSource(this);
		this.#settingsDataSource = new uSyncSettingsDataSource(this);
		this.#migrartionDataSource = new uSyncMigrationDataSource(this);
	}

	/**
	 * Get the list of possible actions from the server
	 * @returns Promise
	 */
	async getActions() {
		return this.#actionDataSource.getActions();
	}

	/**
	 * Request of the action to perform
	 * @returns PerformActionResponse.
	 */
	async performAction(request: SyncPerformRequest) {
		return this.#actionDataSource.performAction({
			requestId: request.id,
			action: request.action,
			options: {
				group: request.group,
				force: request.force ?? false,
				clean: request.clean ?? false,
				clientId: request.clientId,
				set: request.set ?? 'Default',
			},
			stepNumber: request.step,
		});
	}

	/**
	 * Retreives the current uSync settings
	 * @returns the current uSync settings
	 */
	async getSettings() {
		return await this.#settingsDataSource.getSettings();
	}

	async getAddons() {
		return await this.#settingsDataSource.getAddons();
	}

	/**
	 * Get the handler settings based on the set.
	 * @param setName name of the handler set in the configuration
	 * @returns the settings for the named handler set.
	 */
	async getHandlerSettings(setName: string) {
		return await this.#settingsDataSource.getHandlerSettings(setName);
	}

	/**
	 * Checks to see if there are legacy datatypes on disk.
	 * @returns results of a check for legacy files
	 */
	async checkLegacy() {
		return await this.#migrartionDataSource.checkLegacy();
	}

	/**
	 * sets a .ignore folder in the legacy folder so we don't detect it next time.
	 * @returns true if the legacy folder is ignored.
	 */
	async ignoreLegacy() {
		return await this.#migrartionDataSource.ignoreLegacy();
	}

	/**
	 * copies the legacy folder to the new v14 folder.
	 * @returns true if the legacy folder is copied to the new folder.
	 */
	async copyLegacy() {
		return await this.#migrartionDataSource.copyLegacy();
	}
}
