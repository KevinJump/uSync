import { UmbContextToken } from '@umbraco-cms/backoffice/context-api'
import uSyncSignalRContext from './signalr.context'

export const USYNC_SIGNALR_CONTEXT_TOKEN =
    new UmbContextToken<uSyncSignalRContext>('uSyncSignalRContext')
