using System;

namespace uSync8.BackOffice
{
    /// <summary>
    ///  causes a section of code to be ran while the uSync events are paused. 
    /// </summary>
    public class uSyncImportPause : IDisposable
    {
        public uSyncImportPause()
        {
            uSync8BackOffice.eventsPaused = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            uSync8BackOffice.eventsPaused = false;
        }
    }
}
