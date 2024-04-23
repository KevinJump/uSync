import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { tryExecuteAndNotify } from '@umbraco-cms/backoffice/resources';

import { MigrationsService, SyncLegacyCheckResponse } from '../../api/index.js';
export interface SyncMigrationDataSource {
	checkLegacy(): Promise<UmbDataSourceResponse<SyncLegacyCheckResponse>>;
}

export class uSyncMigrationDataSource {
	#host: UmbControllerHost;

	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	async checkLegacy(): Promise<UmbDataSourceResponse<SyncLegacyCheckResponse>> {
		return await tryExecuteAndNotify(this.#host, MigrationsService.checkLegacy());
	}
}
