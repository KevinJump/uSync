import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import {
	UmbConditionConfigBase,
	UmbConditionControllerArguments,
} from '@umbraco-cms/backoffice/extension-api';
import { UmbConditionBase } from '@umbraco-cms/backoffice/extension-registry';
import { uSyncMigrationDataSource } from '../repository/sources/SyncMigration.source';

export type SyncLegacyFilesConditionConfig = UmbConditionConfigBase & {
	hasLegacyFiles: boolean;
};

export class SyncLegacyFilesCondition extends UmbConditionBase<SyncLegacyFilesConditionConfig> {
	#migrationDataSource: uSyncMigrationDataSource;

	config: SyncLegacyFilesConditionConfig;

	constructor(
		host: UmbControllerHost,
		args: UmbConditionControllerArguments<SyncLegacyFilesConditionConfig>,
	) {
		super(host, args);
		this.config = args.config;

		this.#migrationDataSource = new uSyncMigrationDataSource(host);
		this.#migrationDataSource.checkLegacy().then((response) => {
			console.log(response.data);
			this.permitted = response.data?.hasLegacy ?? false;
		});
	}
}
