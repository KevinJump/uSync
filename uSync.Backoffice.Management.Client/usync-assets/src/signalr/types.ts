/**
 * @description format of the update message coming from uSync via SignalR.
 */
export type SyncUpdateMessage = {
    message: string
    count: number
    total: number
}
