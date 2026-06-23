import { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import {
	getAddOns,
	getHandlerSettings,
	getSets,
	getSettings,
	USyncHandlerSetSettings,
	USyncSettings,
} from '@jumoo/uSync';

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
		return await tryExecute(this.#host, getSettings());
	}

	async getHandlerSettings(
		setName: string,
	): Promise<UmbDataSourceResponse<USyncHandlerSetSettings>> {
		return await tryExecute(this.#host, getHandlerSettings({ query: { id: setName } }));
	}

	async getAddons() {
		return await tryExecute(this.#host, getAddOns());
	}

	async getSets() {
		return await tryExecute(this.#host, getSets());
	}
}
