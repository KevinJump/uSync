import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import { uSyncSignalRContext } from '@jumoo/uSync';

export const USYNC_SIGNALR_CONTEXT_TOKEN = new UmbContextToken<uSyncSignalRContext>(
	'uSyncSignalRContext',
);
