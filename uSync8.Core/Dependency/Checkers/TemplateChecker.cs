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

            // only get teh dependencies for templates if the flag is set. 
            if (!flags.HasFlag(DependencyFlags.IncludeViews)) return dependencies;

            dependencies.Add(new uSyncDependency()
            {
                Name = item.Name,
                Order = DependencyOrders.Templates,
                Udi = item.GetUdi(),
                Flags = flags,
                Level = CalculateLevel(item)
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
                        Name = item.Name,
                        Order = order,
                        Udi = master.GetUdi(),
                        Level = CalculateLevel(master),
                        Flags = flags & ~DependencyFlags.IncludeAncestors
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
                        Name = item.Name,
                        Order = order,
                        Udi = child.GetUdi(),
                        Flags = flags & ~DependencyFlags.IncludeAncestors,
                        Level = CalculateLevel(child)
                    });

                    GetChildren(child, order + 1, flags);
                }
            }

            return templates;
        }

        private int CalculateLevel(ITemplate item)
        {
            return item.Path.ToDelimitedList().Count();

            /*
            if (item.MasterTemplateAlias.IsNullOrWhiteSpace()) return 1;

            int level = 1;
            var current = item;
            while (!string.IsNullOrWhiteSpace(current.MasterTemplateAlias) && level < 20)
            {
                level++;
                var parent = fileService.GetTemplate(current.MasterTemplateAlias);
                if (parent == null) return level;

                current = parent;
            }

            return level;
            */
        }
    }
}
