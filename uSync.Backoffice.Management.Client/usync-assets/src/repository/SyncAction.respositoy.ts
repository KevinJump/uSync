import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { uSyncActionDataSource } from "./sources/SyncAction.source";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { uSyncSettingsDataSource } from "./sources/SyncSettings.source";
import { uSyncMigrationDataSource } from "./sources/SyncMigration.source";


export type SyncPerformRequest = {
    id: string, 
    group: string, 
    action: string, 
    step: number, 
    force?: boolean,
    clean?: boolean,
    set?: string,
    clientId: string
}

/**
 * @export
 * @class uSyncActionRepository
 * @description repository for all things actions.
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
     * @method getActions
     * @description get the list of possible actions from the server
     * @returns Promise
     */
    async getActions() {
        return this.#actionDataSource.getActions();
    }

    /**
     * @method performAction
     * @param request request of the action to perform
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
                set: request.set ?? 'default'
            },
            stepNumber: request.step
        });
    }

    /**
     * @method getSettings
     * @description retreives the current uSync settings
     * @returns the current uSync settings
     */
    async getSettings() {
        return await this.#settingsDataSource.getSettings();
    }

    /**
     * @method getHandlerSetSettings
     * @param setName name of the handler set in the configuration
     * @returns the settings for the named handler set.
     */
    async getHandlerSettings(setName : string) {
        return await this.#settingsDataSource.getHandlerSettings(setName);
    }

    /**
     * @method checkLegacy
     * @description checks to see if there are legacy datatypes on disk.
     * @returns results of a check for legacy files
     */
    async checkLegacy() {
        return await this.#migrartionDataSource.checkLegacy();
    }
}