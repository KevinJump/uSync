import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import {
	UmbConditionConfigBase,
	UmbConditionControllerArguments,
} from '@umbraco-cms/backoffice/extension-api';
import { UmbConditionBase } from '@umbraco-cms/backoffice/extension-registry';
import { USYNC_CORE_CONTEXT_TOKEN } from '../workspace/workspace.context';

export type SyncLegacyFilesConditionConfig = UmbConditionConfigBase & {
	hasLegacyFiles: boolean;
};

export class SyncLegacyFilesCondition extends UmbConditionBase<SyncLegacyFilesConditionConfig> {
	config: SyncLegacyFilesConditionConfig;

	constructor(
		host: UmbControllerHost,
		args: UmbConditionControllerArguments<SyncLegacyFilesConditionConfig>,
	) {
		super(host, args);
		this.config = args.config;

		this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
			// consuming the context means it only happens when the context exists.
			_instance.checkLegacy().then((response) => {
				this.permitted = response?.hasLegacy ?? false;
			});
		});
	}
}
