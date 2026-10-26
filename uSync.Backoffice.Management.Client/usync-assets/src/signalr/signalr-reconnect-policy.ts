import type { IRetryPolicy, RetryContext } from '@jumoo/uSync/external/signalr';

const RECONNECT_DELAYS_MS = [0, 2000, 5000, 10000];
const MAX_RECONNECT_DELAY_MS = 30000;

/**
 * Reconnects indefinitely with a capped backoff.
 *
 * Local copy of Umbraco's UmbSignalRReconnectPolicy (added in 17.8), because
 * importing it from the backoffice breaks on 17.3 - 17.7. The default
 * `withAutomaticReconnect()` policy gives up after ~40 seconds.
 */
export class uSyncSignalRReconnectPolicy implements IRetryPolicy {
	nextRetryDelayInMilliseconds(retryContext: RetryContext): number {
		return RECONNECT_DELAYS_MS[retryContext.previousRetryCount] ?? MAX_RECONNECT_DELAY_MS;
	}
}
