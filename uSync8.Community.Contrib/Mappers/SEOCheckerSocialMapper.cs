using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core;
using Umbraco.Core.Services;
using uSync8.ContentEdition.Mapping;
using uSync8.Core.Dependency;

namespace uSync8.Community.Contrib.Mappers
{
    public class SEOCheckerSocialMapper : SyncValueMapperBase, ISyncMapper
    {
        public SEOCheckerSocialMapper(IEntityService entityService)
            : base(entityService)
        { }

        public override string Name => "SEO Checker Social Mapper";

        public override string[] Editors => new[] { "SEOChecker.SEOCheckerSocialPropertyEditor" };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            if (value is string str && string.IsNullOrWhiteSpace(str) == false)
            {
                try
                {
                    var doc = XDocument.Parse(str);
                    if (doc != null)
                    {
                        var image = doc.XPathSelectElement("/SEOCheckerSocial/socialImage");
                        if (image != null)
                        {
                            var dependency = CreateDependency(image.Value, flags);
                            if (dependency != null)
                            {
                                return dependency.AsEnumerableOfOne();
                            }
                        }
                    }
                }
                catch { /* ¯\_(ツ)_/¯ */ }
            }

            return base.GetDependencies(value, editorAlias, flags);
        }
    }
}
