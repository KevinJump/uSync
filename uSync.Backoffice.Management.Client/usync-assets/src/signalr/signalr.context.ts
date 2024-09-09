import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';

import * as signalR from '@jumoo/uSync/external/signalr';
import { USYNC_SIGNALR_CONTEXT_TOKEN, SyncUpdateMessage } from '@jumoo/uSync';

export class uSyncSignalRContext extends UmbControllerBase {
	#connection?: signalR.HubConnection;

	constructor(host: UmbControllerHost) {
		super(host);
		this.provideContext(USYNC_SIGNALR_CONTEXT_TOKEN, this);
	}

	hostConnected(): void {
		super.hostConnected();
		this.#setupConnection('/umbraco/SyncHub');
	}

	hostDisconnected(): void {
		super.hostDisconnected();
		this.#connection?.stop().then(() => {
			console.debug('connection closed');
		});
	}

	getClientId(): string | null {
		return this.#connection?.connectionId ?? null;
	}

	#update = new UmbObjectState<SyncUpdateMessage | undefined>(undefined);
	public readonly update = this.#update.asObservable();

	#add = new UmbObjectState({});
	public readonly add = this.#add.asObservable();

	#setupConnection(url: string) {
		this.#connection = new signalR.HubConnectionBuilder().withUrl(url).build();

		this.#connection.on('add', (data) => {
			this.#add.setValue(data);
		});

		this.#connection.on('update', (data) => {
			this.#update.setValue(data);
		});

		this.#connection.start().then(() => {
			console.debug('connection started');
		});
	}
}

export default uSyncSignalRContext;
