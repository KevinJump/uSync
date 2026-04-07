import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
  UmbConditionConfigBase,
  UmbConditionControllerArguments,
} from "@umbraco-cms/backoffice/extension-api";
import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import { History } from "../api";

export type SyncHistoryEnabledConditionConfig = UmbConditionConfigBase & {
  isEnabled: boolean;
};

export class SyncHistoryEnabledCondition extends UmbConditionBase<SyncHistoryEnabledConditionConfig> {
  constructor(
    host: UmbControllerHost,
    args: UmbConditionControllerArguments<SyncHistoryEnabledConditionConfig>,
  ) {
    super(host, args);

    History.historyIsEnabled().then((response) => {
      const isEnabled = response.data ?? false;
      this.permitted = isEnabled && args.config.isEnabled;
    });
  }
}

export default SyncHistoryEnabledCondition;
