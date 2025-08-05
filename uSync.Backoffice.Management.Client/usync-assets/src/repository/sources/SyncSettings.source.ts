import { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { SettingsService, USyncHandlerSetSettings, USyncSettings } from '@jumoo/uSync';

export interface SyncSettingsDataSource {
	getSettings(): Promise<UmbDataSourceResponse<USyncSettings>>;
	getHandlerSettings(
		setName: string,
	): Promise<UmbDataSourceResponse<USyncHandlerSetSettings>>;
}

export class USyncSettingsDataSource implements SyncSettingsDataSource {
	#host: UmbControllerHost;

	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	async getSettings(): Promise<UmbDataSourceResponse<USyncSettings>> {
		return await tryExecute(this.#host, SettingsService.getSettings());
	}

	async getHandlerSettings(
		setName: string,
	): Promise<UmbDataSourceResponse<USyncHandlerSetSettings>> {
		return await tryExecute(
			this.#host,
			SettingsService.getHandlerSetSettings({ query: { id: setName } }),
		);
	}

	async getAddons() {
		return await tryExecute(this.#host, SettingsService.getAddOns());
	}

	async getSets() {
		return await tryExecute(this.#host, SettingsService.getSets());
	}
}
