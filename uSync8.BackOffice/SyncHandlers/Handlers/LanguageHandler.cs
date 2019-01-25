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
    [SyncHandler("languageHandler", "Language Handler", "Languages", uSyncBackOfficeConstants.Priorites.Languages)]
    public class LanguageHandler : SyncHandlerBase<ILanguage>, ISyncHandler
    {
        private readonly ILocalizationService localizationService;

        public LanguageHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            ILocalizationService localizationService,
            ISyncSerializer<ILanguage> serializer, 
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, serializer, syncFileService, settings)
        {
            this.localizationService = localizationService;

            this.itemObjectType = UmbracoObjectTypes.Language;
            this.itemContainerType = UmbracoObjectTypes.Unknown;
        }


        public override uSyncAction ReportItem(string file)
        {
            return uSyncAction.Fail("not implimented", typeof(ILanguage), new Exception("Not implimented"));
        }

        protected override ILanguage GetFromService(int id)
            => localizationService.GetLanguageById(id);

        protected override string GetItemPath(ILanguage item)
            => item.IsoCode.ToSafeFileName();

        public void InitializeEvents()
        {
            LocalizationService.SavedLanguage += LocalizationService_SavedLanguage;
            LocalizationService.DeletedLanguage += LocalizationService_DeletedLanguage;
        }

        private void LocalizationService_DeletedLanguage(ILocalizationService sender, Umbraco.Core.Events.DeleteEventArgs<ILanguage> e)
        {
            // throw new NotImplementedException();
        }

        private void LocalizationService_SavedLanguage(ILocalizationService sender, Umbraco.Core.Events.SaveEventArgs<ILanguage> e)
        {
            foreach(var item in e.SavedEntities)
            {
                Export(item, this.DefaultFolder);
            }
        }
    }
}
