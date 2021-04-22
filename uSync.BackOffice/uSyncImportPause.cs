using System;

namespace uSync.BackOffice
{
    /// <summary>
    ///  causes a section of code to be ran while the uSync events are paused. 
    /// </summary>
    public class uSyncImportPause : IDisposable
    {
        public uSyncImportPause()
        {
            uSyncBackOffice.eventsPaused = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            uSyncBackOffice.eventsPaused = false;
        }
    }
}
