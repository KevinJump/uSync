import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { MigrationsService, SyncLegacyCheckResponse } from '@jumoo/uSync';
import { tryExecute } from '@umbraco-cms/backoffice/resources';

export interface SyncMigrationDataSource {
	checkLegacy(): Promise<UmbDataSourceResponse<SyncLegacyCheckResponse>>;
}

export class uSyncMigrationDataSource {
	#host: UmbControllerHost;

	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	async checkLegacy(): Promise<UmbDataSourceResponse<SyncLegacyCheckResponse>> {
		return await tryExecute(this.#host, MigrationsService.checkLegacy());
	}

	async ignoreLegacy(): Promise<UmbDataSourceResponse<boolean>> {
		return await tryExecute(this.#host, MigrationsService.ignoreLegacy());
	}

	async copyLegacy(): Promise<UmbDataSourceResponse<boolean>> {
		return await tryExecute(this.#host, MigrationsService.copyLegacy());
	}
}
