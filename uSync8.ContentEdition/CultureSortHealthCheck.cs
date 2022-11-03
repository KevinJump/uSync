using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Xml.Linq;

using Microsoft.AspNet.SignalR.Infrastructure;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web.HealthCheck;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;

namespace uSync8.ContentEdition
{
    [HealthCheck("3FAAB2A9-8D2F-4327-9722-6424FFA94474",
        "uSync - Culture Sort Check",
        Description = "Check the sort order of cultures on content properties",
        Group = "uSync")]
    internal class CultureSortHealthCheck : HealthCheck
    {
        private readonly SyncFileService _fileService;
        private uSyncSettings _settings;


        public CultureSortHealthCheck(SyncFileService fileService)
        {
            _fileService = fileService;
            _settings = Current.Configs.uSync();
        }

        public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        {
            if (action.Alias == "sort-culture") return FixCultures();

            throw new NotImplementedException($"Unknown action {action.Alias}");
        }

        public override IEnumerable<HealthCheckStatus> GetStatus()
        {
            return CheckContentItems().AsEnumerableOfOne();
        }

        private HealthCheckStatus CheckContentItems()
        {
            var root = _fileService.GetAbsPath(_settings.RootFolder);
            var contentPath = Path.Combine(root, "content");

            int fileCount = 0;

            foreach (var file in _fileService.GetFiles(contentPath, "*.config", true))
            {
                try
                {
                    var xml = XElement.Load(file);

                    fileCount++;

                    if (!AreLanguagesOK(xml))
                    {
                        return new HealthCheckStatus("Cultures are not sorted")
                        {
                            ResultType = StatusResultType.Warning,
                            Actions = new HealthCheckAction("sort-culture", this.Id)
                            {
                                Name = "Sort Cultures in uSync content files"
                            }.AsEnumerableOfOne(),
                            Description =
@"The cultures in your content nodes are not sorted.
uSync files from v8.12.x or above sort all the culture values to speed things up.
You can quickly fix this locally by running this health check."
                        };
                    }
                }
                catch(Exception ex)
                {
                    return new HealthCheckStatus("Error reading content files")
                    {
                        ResultType = StatusResultType.Error,
                        Description = $@"The health check encounted an error reading a content config file:
[{Path.GetFileName(file)}] {ex.Message}"
                    };
                }

            }

            return new HealthCheckStatus($"Content cultures look OK ({fileCount} files)");
        }                    

        private bool AreLanguagesOK(XElement xml)
        {
            var nodeName = xml.Element("Info")?.Element("NodeName");
            if (nodeName == null || !nodeName.HasElements) return true; // no culture stuff 

            var cultures = nodeName.Elements()
                .Where(x => x.Attribute("Culture") != null)
                .Select(x => x.Attribute("Culture").Value);

            var sorted = string.Join(":", cultures.OrderBy(x => x));

            return sorted == String.Join(":", cultures);
        }


        private HealthCheckStatus FixCultures()
        {
            var root = _fileService.GetAbsPath(_settings.RootFolder);
            var contentPath = Path.Combine(root, "content");
            int changeCount = 0;

            foreach (var file in _fileService.GetFiles(contentPath, "*.config", true))
            {
                try
                {
                    var xml = XElement.Load(file);
                    var nodeChanges = SortNodeName(xml);
                    var properyChanges = SortProperties(xml);

                    if (nodeChanges || properyChanges)
                    {
                        changeCount++;
                        xml.Save(file);
                    }
                }
                catch (Exception ex) 
                {
                    return new HealthCheckStatus("Error reading content files")
                    {
                        ResultType = StatusResultType.Error,
                        Description = $@"The health check encounted an error reading a content config file:
[{Path.GetFileName(file)}] {ex.Message}"
                    };
                }
            }

            return new HealthCheckStatus($"Updated Culture sorting in {changeCount} files");
        }

        private bool SortNodeName(XElement xml)
        {
            var info = xml.Element("Info");
            if (info == null) return false; // not a proper file anyway


            var changes = false;
            var nodeName = info.Element("NodeName");
            if (nodeName != null && nodeName.HasElements)
            {
                var sortedNodeName = new XElement("NodeName",
                    new XAttribute("Default", nodeName.Attribute("Default").Value));

                foreach (var element in nodeName.Elements()
                    .OrderBy(x => x.Attribute("Culture").Value))
                {
                    var newElement = new XElement(element.Name.LocalName,
                        new XAttribute("Culture", element.Attribute("Culture").Value),
                        element.Value);

                    sortedNodeName.Add(newElement);
                }
                changes = true;

                nodeName.ReplaceWith(sortedNodeName);
            }

            return changes;
        }

        private bool SortProperties(XElement xml)
        {
            var properties = xml.Element("Properties");
            if (properties == null || !properties.HasElements) return false;

            var changes = false;

            foreach (var property in properties.Elements().ToList())
            {
                if (!property.HasElements) continue;

                var sorted = false;
                var sortedValue = new XElement(property.Name.LocalName);
                foreach (var element in property.Elements()
                    .Where(x => x.Attribute("Culture") != null)
                    .OrderBy(x => x.Attribute("Culture").Value))
                {
                    var newElement = new XElement(element.Name.LocalName,
                        new XAttribute("Culture", element.Attribute("Culture").Value),
                        new XCData(element.Value));

                    sortedValue.Add(newElement);
                    sorted = true;
                }

                if (sorted)
                {
                    changes = true;
                    property.ReplaceWith(sortedValue);
                }
            }

            return changes;

            
        }

    }
}
