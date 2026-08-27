import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import {
	getActionsBySet,
	getRunning,
	getStatus,
	getSyncFileInfo,
	PerformActionRequest,
	PerformActionResponse,
	postDownload,
	postImport,
	postPerform,
	postProcessUpload,
	SyncActionGroup,
	SyncOperationStatusResponse,
	SyncRunningOperationResponse,
	USyncActionView,
} from '@jumoo/uSync';

export interface SyncActionDataSource {
	getActionsBySet(setName: string): Promise<UmbDataSourceResponse<unknown>>;
	performAction(
		request: PerformActionRequest,
	): Promise<UmbDataSourceResponse<PerformActionResponse>>;
	getOperationStatus(
		operationId: string,
	): Promise<UmbDataSourceResponse<SyncOperationStatusResponse>>;
	getRunningOperation(): Promise<
		UmbDataSourceResponse<SyncRunningOperationResponse>
	>;
}

export class uSyncActionDataSource implements SyncActionDataSource {
	#host: UmbControllerHost;

	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	async getSyncFileInfo() {
		return await tryExecute(this.#host, getSyncFileInfo());
	}

	async getActionsBySet(
		setName: string,
	): Promise<UmbDataSourceResponse<Array<SyncActionGroup>>> {
		return await tryExecute(
			this.#host,
			getActionsBySet({
				query: { setName: setName },
			}),
		);
	}

	async performAction(
		request: PerformActionRequest,
	): Promise<UmbDataSourceResponse<PerformActionResponse>> {
		return await tryExecute(
			this.#host,
			postPerform({
				body: request,
			}),
		);
	}

	async downloadFile() {
		return await tryExecute(this.#host, postDownload());
	}

	async getOperationStatus(
		operationId: string,
	): Promise<UmbDataSourceResponse<SyncOperationStatusResponse>> {
		return await tryExecute(
			this.#host,
			getStatus({
				query: { operationId: operationId },
			}),
		);
	}

	async getRunningOperation(): Promise<
		UmbDataSourceResponse<SyncRunningOperationResponse>
	> {
		return await tryExecute(this.#host, getRunning());
	}

	async processUpload(fileId: string) {
		return await tryExecute(
			this.#host,
			postProcessUpload({
				query: {
					tempKey: fileId,
				},
			}),
		);
	}

	async importSingle(view: USyncActionView) {
		return await tryExecute(
			this.#host,
			postImport({
				body: view,
			}),
		);
	}
}
