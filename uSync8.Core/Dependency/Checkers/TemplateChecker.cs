using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace uSync8.Core.Dependency
{
    public class TemplateChecker : ISyncDependencyChecker<ITemplate>
    {
        private IFileService fileService;

        public TemplateChecker(IFileService fileService)
        {
            this.fileService = fileService;
        }

        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Template;

        public IEnumerable<uSyncDependency> GetDependencies(ITemplate item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            if (flags.HasFlag(DependencyFlags.NoTemplates)) return dependencies;

            dependencies.Add(new uSyncDependency()
            {
                Order = DependencyOrders.Templates,
                Udi = item.GetUdi(),
                Flags = flags
            });

            if (flags.HasFlag(DependencyFlags.IncludeAncestors))
            {
                dependencies.AddRange(GetParents(item, DependencyOrders.Templates - 1, flags));
            }

            if (flags.HasFlag(DependencyFlags.IncludeChildren))
            {
                // children check.
                dependencies.AddRange(GetChildren(item, DependencyOrders.Templates + 1, flags));
            }

            return dependencies;
        }

        private IEnumerable<uSyncDependency> GetParents(ITemplate item, int order, DependencyFlags flags)
        {
            var templates = new List<uSyncDependency>();

            if (!string.IsNullOrWhiteSpace(item.MasterTemplateAlias))
            {
                var master = fileService.GetTemplate(item.MasterTemplateAlias);
                if (master != null)
                {
                    templates.Add(new uSyncDependency()
                    {
                        Order = order,
                        Udi = master.GetUdi(),
                        Flags = flags
                    });

                    templates.AddRange(GetParents(master, order - 1, flags));
                }
            }

            return templates;
        }

        private IEnumerable<uSyncDependency> GetChildren(ITemplate item, int order, DependencyFlags flags)
        {
            var templates = new List<uSyncDependency>();

            var children = fileService.GetTemplateChildren(item.Id);
            if (children != null && children.Any())
            {
                foreach(var child in children)
                {
                    templates.Add(new uSyncDependency()
                    {
                        Order = order,
                        Udi = child.GetUdi(),
                        Flags = flags
                    });

                    GetChildren(child, order + 1, flags);
                }
            }

            return templates;
        }
    }
}
