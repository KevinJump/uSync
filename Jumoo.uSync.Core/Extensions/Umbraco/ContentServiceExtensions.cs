using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Extensions.Umbraco
{
    public static class ContentServiceExtensions
    {
        public static IEnumerable<IContent> GetContentDescendants(this IContentService service)
        {
            var children = service.GetChildren(-1).ToList();
            var result = new List<IContent>(children);
            foreach (var rootNode in children)
            {
                result.AddRange(rootNode.Descendants());
            }

            return result;
        }

        public static IContent GetFirstParentOfType(this IContentService service, int contentId, string contentTypeAlias)
        {
            var parent = service.GetParent(contentId);
            while (parent != null)
            {
                if (parent.ContentType.Alias == contentTypeAlias)
                {
                    return parent;
                }
                parent = parent.Parent();
            }
            return null;
        }
    }
}