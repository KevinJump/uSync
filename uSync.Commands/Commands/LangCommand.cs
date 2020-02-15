using System;
using System.IO;
using System.Threading.Tasks;

using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.BackOffice.Commands;

namespace uSync.BaseCommands.Commands
{
    [SyncCommand("Lang", "lang", "Add/Remove languages")]
    public class LangCommand : SyncCommandBase, ISyncCommand
    {
        private readonly ILocalizationService localizationService;

        public LangCommand(TextReader reader, TextWriter writer,
            ILocalizationService localizationService)
            : base(reader, writer)
        {
            this.localizationService = localizationService;
        }

        public async Task<SyncCommandResult> Run(string[] args)
        {
            if (args.Length < 1)
            {
                await writer.WriteLineAsync("usage Lang add iso-code [iso-code]");
                return SyncCommandResult.NoResult;
            }

            if (args.Length == 1 && args[0].Equals("list", StringComparison.InvariantCultureIgnoreCase))
            {
                return await List();
            }

            if (args.Length > 1) {

                switch (args[0].ToLower()) 
                {
                    case "add":
                        return await AddLanguage(args[1]);
                    case "remove":
                        return await RemoveLanguage(args[1]);
                    case "default":
                        return await SetDefault(args[1]);
                }
          
            }

            return SyncCommandResult.NoResult;
        }

        private async Task<SyncCommandResult> AddLanguage(string isoCode)
        { 
            var existing = localizationService.GetLanguageByIsoCode(isoCode);
            if (existing == null || existing.IsoCode != isoCode)
            {
                await writer.WriteLineAsync($"Adding Language {isoCode}");
                var lang = new Language(isoCode);
                localizationService.Save(lang);
                return SyncCommandResult.Success;
            }

            await writer.WriteLineAsync($"Language {isoCode} aready exists");
            return SyncCommandResult.NoResult;
        }

        private async Task<SyncCommandResult> RemoveLanguage(string isoCode)
        {
            var existing = localizationService.GetLanguageByIsoCode(isoCode);
            if (existing != null && existing.IsoCode.Equals(isoCode, StringComparison.InvariantCultureIgnoreCase))
            {
                await writer.WriteLineAsync($"Removing Language ${isoCode}");
                localizationService.Delete(existing);
                return SyncCommandResult.Success;
            }

            await writer.WriteLineAsync($"Language {isoCode} not installed");
            return SyncCommandResult.NoResult;
        }

        private async Task<SyncCommandResult> SetDefault(string isoCode)
        {
            var existing = localizationService.GetLanguageByIsoCode(isoCode);
            if (existing != null && existing.IsoCode.Equals(isoCode, StringComparison.InvariantCultureIgnoreCase))
            {
                var defaultLangId = localizationService.GetDefaultLanguageId();
                if (defaultLangId.HasValue)
                {
                    var defaultLang = localizationService.GetLanguageById(defaultLangId.Value);
                    if (defaultLang != null && !defaultLang.IsoCode.Equals(isoCode, StringComparison.InvariantCultureIgnoreCase))
                    {
                        await writer.WriteLineAsync($"Unsetting {isoCode} as default");
                        defaultLang.IsDefault = false;
                    }
                }

                await writer.WriteLineAsync($"Setting {isoCode} as default");
                existing.IsDefault = true;
                localizationService.Save(existing);

                return SyncCommandResult.Success;
            }


            await writer.WriteLineAsync($"Language {isoCode} not installed");
            return SyncCommandResult.NoResult;


        }

        private async Task<SyncCommandResult> List()
        {
            var languages = localizationService.GetAllLanguages();

            await writer.WriteLineAsync("Language\tDefault");
            await writer.WriteLineAsync("----------------------------");
            foreach(var lang in languages)
            {
                await writer.WriteLineAsync(
                    string.Format("- {0}\t\t{1}", lang.IsoCode,
                                    lang.IsDefault ? "  #" : ""));
            }

            return SyncCommandResult.Success;
        }
    }
}
