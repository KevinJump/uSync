import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import {
	ActionsService,
	PerformActionRequest,
	PerformActionResponse,
	SyncActionGroup,
	USyncActionView,
} from '@jumoo/uSync';

export interface SyncActionDataSource {
	getActionsBySet(setName: string): Promise<UmbDataSourceResponse<unknown>>;
	performAction(
		request: PerformActionRequest,
	): Promise<UmbDataSourceResponse<PerformActionResponse>>;
}

export class uSyncActionDataSource implements SyncActionDataSource {
	#host: UmbControllerHost;

	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	async getActionsBySet(
		setName: string,
	): Promise<UmbDataSourceResponse<Array<SyncActionGroup>>> {
		return await tryExecute(
			this.#host,
			ActionsService.getActionsBySet({
				query: { setName: setName },
			}),
		);
	}

	async performAction(
		request: PerformActionRequest,
	): Promise<UmbDataSourceResponse<PerformActionResponse>> {
		return await tryExecute(
			this.#host,
			ActionsService.performAction({
				body: request,
			}),
		);
	}

	async downloadFile(requestId: string) {
		return await tryExecute(
			this.#host,
			ActionsService.download({
				query: {
					requestId: requestId,
				},
			}),
		);
	}

	async processUpload(fileId: string) {
		return await tryExecute(
			this.#host,
			ActionsService.processUpload({
				query: {
					tempKey: fileId,
				},
			}),
		);
	}

	async importSingle(view: USyncActionView) {
		return await tryExecute(
			this.#host,
			ActionsService.importSingle({
				body: view,
			}),
		);
	}
}
