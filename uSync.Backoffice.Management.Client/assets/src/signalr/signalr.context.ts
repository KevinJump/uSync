import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

import * as signalR from "@microsoft/signalr"
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbObjectState } from "@umbraco-cms/backoffice/observable-api";

export interface ISyncUpdateMessage {
    message: string;
    count: number;
    total: number;
}

export class uSyncSignalRContext extends UmbControllerBase {

    #connection : signalR.HubConnection | null = null;

    constructor(host:UmbControllerHost) 
    {
        super(host)
        this.provideContext(USYNC_SIGNALR_CONTEXT_TOKEN, this);
    }

    hostConnected(): void {
        super.hostConnected();
        this.#setupConnection('/umbraco/SyncHub')
    }
    
    hostDisconnected(): void {
        super.hostDisconnected();
        this.#connection?.stop()
            .then(() => {
                console.debug('connection closed');
            });
    }

    getClientId(): string | null
    {
        return this.#connection?.connectionId ?? null;
    }

    #update = new UmbObjectState<ISyncUpdateMessage | null>(null);
    public readonly update = this.#update.asObservable();

    #add = new UmbObjectState({});
    public readonly add = this.#add.asObservable();


    #setupConnection(url: string) {

        this.#connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .build();

        this.#connection.on('add', data => {
            this.#add.setValue(data);
        })

        this.#connection.on('update', data => {
            this.#update.setValue(data);
        });

        this.#connection.start()
            .then(() => {
                console.debug('connection started');
            });
    }
}

export default uSyncSignalRContext;

export const USYNC_SIGNALR_CONTEXT_TOKEN = 
    new UmbContextToken<uSyncSignalRContext>(uSyncSignalRContext.name);