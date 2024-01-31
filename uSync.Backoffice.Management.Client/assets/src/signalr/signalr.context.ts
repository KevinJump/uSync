import { UmbBaseController } from "@umbraco-cms/backoffice/class-api";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

import * as signalR from "@microsoft/signalr"
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbObjectState } from "@umbraco-cms/backoffice/observable-api";

export interface ISyncUpdateMessage {
    message: string;
    count: number;
    total: number;
}

export class uSyncSignalRContext extends UmbBaseController {

    #connection : signalR.HubConnection | null = null;

    constructor(host:UmbControllerHost) 
    {
        console.log('signalR');
        super(host)
        this.provideContext(USYNC_SIGNALR_CONTEXT_TOKEN, this);
    }

    hostConnected(): void {
        super.hostConnected();
        console.log('host-connected');
        this.#setupConnection('/umbraco/SyncHub')
    }
    
    hostDisconnected(): void {
        super.hostDisconnected();
        console.log('host-disconnected');       
        this.#connection?.stop()
            .then(() => {
                console.log('connection closed');
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
                console.log('connection started');
            });
    }
}

export default uSyncSignalRContext;

export const USYNC_SIGNALR_CONTEXT_TOKEN = 
    new UmbContextToken<uSyncSignalRContext>(uSyncSignalRContext.name);