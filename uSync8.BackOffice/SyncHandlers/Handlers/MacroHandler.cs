using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice.Services;
using uSync8.Core.Serialization;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("macroHandler", "Macro Handler", "Macros", uSyncBackOfficeConstants.Priorites.Macros)]
    public class MacroHandler : SyncHandlerBase<IMacro>, ISyncHandler
    {
        private readonly IMacroService macroService;

        public MacroHandler(IEntityService entityService,
            IProfilingLogger logger, 
            IMacroService macroService,
            ISyncSerializer<IMacro> serializer, 
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, serializer, syncFileService, settings)
        {
            this.macroService = macroService;
        }

        /// <summary>
        ///  overrider the default export, because macros, don't exist as an object type???
        /// </summary>
        public override IEnumerable<uSyncAction> ExportAll(int parent, string folder)
        {
            var actions = new List<uSyncAction>();

            var items = macroService.GetAll();
            foreach(var item in items)
            {
                actions.Add(Export(item, folder));
            }

            return actions;
        }

        public override uSyncAction ReportItem(string file)
        {
            return uSyncAction.Fail("not implimented", typeof(IMacro), new Exception("Not implimented"));
        }

        protected override IMacro GetFromService(int id)
            => macroService.GetById(id);

        protected override string GetItemPath(IMacro item)
            => item.Alias.ToSafeFileName();

        public void InitializeEvents()
        {
            MacroService.Saved += MacroService_Saved;
            MacroService.Deleted += MacroService_Deleted;
        }

        private void MacroService_Deleted(IMacroService sender, Umbraco.Core.Events.DeleteEventArgs<IMacro> e)
        {
            // throw new NotImplementedException();
        }

        private void MacroService_Saved(IMacroService sender, Umbraco.Core.Events.SaveEventArgs<IMacro> e)
        {
            foreach(var item in e.SavedEntities)
            {
                Export(item, this.DefaultFolder);
            }
        }
    }
}
