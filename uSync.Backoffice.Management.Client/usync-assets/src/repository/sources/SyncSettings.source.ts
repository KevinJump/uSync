import { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { tryExecuteAndNotify } from '@umbraco-cms/backoffice/resources';
import {
	SettingsService,
	uSyncHandlerSetSettings,
	uSyncSettings,
} from '../../api/index.js';

export interface SyncSettingsDataSource {
	getSettings(): Promise<UmbDataSourceResponse<uSyncSettings>>;
	getHandlerSettings(
		setName: string,
	): Promise<UmbDataSourceResponse<uSyncHandlerSetSettings>>;
}

export class uSyncSettingsDataSource implements SyncSettingsDataSource {
	#host: UmbControllerHost;

	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	async getSettings(): Promise<UmbDataSourceResponse<uSyncSettings>> {
		return await tryExecuteAndNotify(this.#host, SettingsService.getSettings());
	}

	async getHandlerSettings(
		setName: string,
	): Promise<UmbDataSourceResponse<uSyncHandlerSetSettings>> {
		return await tryExecuteAndNotify(
			this.#host,
			SettingsService.getHandlerSetSettings({ id: setName }),
		);
	}
}
