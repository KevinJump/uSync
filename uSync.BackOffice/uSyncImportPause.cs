﻿using System;

using uSync.BackOffice.Services;

namespace uSync.BackOffice
{
    /// <summary>
    ///  wraps code running that might trigger events, pausing uSyncs capture of those events.
    /// </summary>
    public class uSyncImportPause : IDisposable
    {
        private readonly uSyncEventService _mutexService;

        /// <summary>
        /// Generate a pause object
        /// </summary>
        public uSyncImportPause(uSyncEventService mutexService)
        {
            _mutexService = mutexService;
            _mutexService.Pause();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            _mutexService.UnPause();
        }
    }
}
