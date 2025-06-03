import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';

import * as signalR from '@jumoo/uSync/external/signalr';
import { USYNC_SIGNALR_CONTEXT_TOKEN, SyncUpdateMessage } from '@jumoo/uSync';
import { TokenError } from '@umbraco-cms/backoffice/external/openid';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

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
			console.debug('connection stopped');
		});
	}

	getClientId(): string | null {
		return this.#connection?.connectionId ?? null;
	}

	#update = new UmbObjectState<SyncUpdateMessage | undefined>(undefined);
	public readonly update = this.#update.asObservable();

	#add = new UmbObjectState({});
	public readonly add = this.#add.asObservable();

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

		this.#connection.start().then(() => {
			// console.debug('connection started');
		});
	}
}

export default uSyncSignalRContext;
