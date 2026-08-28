import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';
import {
	USYNC_SIGNALR_CONTEXT_TOKEN,
	SyncUpdateMessage,
	SyncProgressSummary,
	SyncCompleteMessage,
} from '@jumoo/uSync';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import * as signalR from '@jumoo/uSync/external/signalr';

export class uSyncSignalRContext extends UmbControllerBase {
	#connection?: signalR.HubConnection;

	constructor(host: UmbControllerHost) {
		super(host);
		this.provideContext(USYNC_SIGNALR_CONTEXT_TOKEN, this);

		this.consumeContext(UMB_AUTH_CONTEXT, async (auth) => {
			if (!auth) return;

			const authConfig = auth?.getOpenApiConfiguration();
			if (!authConfig) return;
			this.#setupConnection('/umbraco/SyncHub', await auth.getLatestToken());
		});
	}

	hostConnected(): void {
		super.hostConnected();
	}

	hostDisconnected(): void {
		super.hostDisconnected();
		this.#connection?.stop().then(() => {
			this.#connected.setValue(false);
		});
	}

	getClientId(): string | null {
		return this.#connection?.connectionId ?? null;
	}

	/**
	 * Synchronous read of the current connection state - used by callers
	 * (e.g. status polling) that need to decide whether to fall back to a
	 * server round-trip right now, rather than subscribing to the observable.
	 */
	getConnected(): boolean {
		return this.#connected.getValue() ?? false;
	}

	/**
	 * Publish an update message sourced from status polling rather than a
	 * live SignalR push - shares the same `update` state/observable that
	 * `usync-progress-box` already renders, so a background run whose socket
	 * isn't delivering (e.g. pinned to a different server under a
	 * load-balanced backoffice) still shows *something* under the handler
	 * icons instead of leaving the message blank/stale for the whole run.
	 * Coarser than a real push - polling only knows the per-handler-step
	 * message ("Processing Import"/"Completed"), not per-item progress - so
	 * count/total are left at a fixed 0/1 rather than implying granularity
	 * we don't have.
	 */
	setPolledUpdate(message: string): void {
		this.#update.setValue({ message, count: 0, total: 1 });
	}

	#connected = new UmbObjectState<boolean>(false);
	public readonly connected = this.#connected.asObservable();

	#update = new UmbObjectState<SyncUpdateMessage | undefined>(undefined);
	public readonly update = this.#update.asObservable();

	#add = new UmbObjectState<SyncProgressSummary | undefined>(undefined);
	public readonly add = this.#add.asObservable();

	#complete = new UmbObjectState<SyncCompleteMessage | undefined>(undefined);
	public readonly complete = this.#complete.asObservable();

	#setupConnection(url: string, token: string) {
		this.#connection = new signalR.HubConnectionBuilder()
			.withUrl(url, { accessTokenFactory: () => token })
			.configureLogging(signalR.LogLevel.Warning)
			.build();

		this.#connection.on('add', (data) => {
			this.#add.setValue(data);
		});

		this.#connection.on('update', (data) => {
			this.#update.setValue(data);
		});

		this.#connection.on('complete', (data) => {
			this.#complete.setValue(data);
		});

		this.#connection.start().then(() => {
			this.#connected.setValue(true);
		});

		this.#connection.onclose(() => {
			this.#connected.setValue(false);
		});
	}
}

export default uSyncSignalRContext;
