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
import { UMB_SERVER_CONTEXT } from '@umbraco-cms/backoffice/server';
import * as signalR from '@jumoo/uSync/external/signalr';
import { uSyncSignalRReconnectPolicy } from './signalr-reconnect-policy.js';

export class uSyncSignalRContext extends UmbControllerBase {
	#connection?: signalR.HubConnection;

	constructor(host: UmbControllerHost) {
		super(host);
		this.provideContext(USYNC_SIGNALR_CONTEXT_TOKEN, this);

		this.consumeContext(UMB_AUTH_CONTEXT, (auth) => {
			this.#auth = auth;
			this.#connect();
		});

		this.consumeContext(UMB_SERVER_CONTEXT, (server) => {
			this.#server = server;
			this.#connect();
		});
	}

	#auth?: typeof UMB_AUTH_CONTEXT.TYPE;
	#server?: typeof UMB_SERVER_CONTEXT.TYPE;

	#connect() {
		// either context being re-provided replaces the connection, so
		// stop the old one rather than leave it running alongside.
		if (this.#connection) {
			this.#connection.stop();
			this.#connection = undefined;
			this.#clientId = null;
			this.#connected.setValue(false);
		}

		if (!this.#auth || !this.#server) return;
		if (!this.#auth.getOpenApiConfiguration()) return;

		const connection = this.#server.getServerConnection();
		// Older Umbraco versions do not expose this setting.
		const skipNegotiation =
			'getSignalRSkipNegotiation' in connection &&
			typeof connection.getSignalRSkipNegotiation === 'function' &&
			connection.getSignalRSkipNegotiation() === true;

		this.#setupConnection('/umbraco/SyncHub', skipNegotiation);
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
		return this.#clientId;
	}

	#clientId: string | null = null;

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

	#setupConnection(url: string, skipNegotiation: boolean) {
		// backoffice auth is cookie-only (Umbraco 19): the httpOnly auth cookie
		// is sent with every (re)connect, so there is no token to supply.
		this.#connection = new signalR.HubConnectionBuilder()
			.withUrl(url, {
				withCredentials: true,
				skipNegotiation,
				transport: skipNegotiation ? signalR.HttpTransportType.WebSockets : undefined,
			})
			.withAutomaticReconnect(new uSyncSignalRReconnectPolicy())
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

		const connection = this.#connection;

		// a stopped connection can still report state while it winds down,
		// only the current one may update connected.
		const setConnected = (connected: boolean) => {
			if (this.#connection !== connection) return;
			if (!connected) this.#clientId = null;
			this.#connected.setValue(connected);
		};

		// the connection id comes from the negotiate response, so when
		// negotiation is skipped we have to ask the hub for it.
		const onConnected = async () => {
			const clientId =
				connection.connectionId ??
				(await connection.invoke<string>('GetConnectionId').catch(() => null));
			if (this.#connection !== connection) return;
			this.#clientId = clientId;
			setConnected(true);
		};

		connection
			.start()
			.then(onConnected)
			.catch((error) => {
				// with negotiation skipped there is no transport fallback, so a
				// blocked WebSocket ends here; status polling covers progress.
				console.warn('uSync: SignalR connection failed', error);
				setConnected(false);
			});

		// the client id changes on reconnect, requests made after this
		// pick up the new one via getClientId().
		connection.onreconnecting(() => setConnected(false));
		connection.onreconnected(onConnected);
		connection.onclose(() => setConnected(false));
	}
}

export default uSyncSignalRContext;
