using System;

using uSync.BackOffice.Services;

namespace uSync.BackOffice
{
    /// <summary>
    ///  wraps code running that might trigger events, pausing uSyncs capture of those events.
    /// </summary>
    public class uSyncImportPause : IDisposable
    {
        private readonly uSyncMutexService _mutexService;

        public uSyncImportPause(uSyncMutexService mutexService)
        {
            _mutexService = mutexService;
            _mutexService.Pause();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _mutexService.UnPause();
        }
    }
}
