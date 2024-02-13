using Examine.Lucene;

using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Examine;

namespace uSync.IntegrationTests.TestSite;

public class SetupProjectComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        var settings = new Umbraco.Cms.Infrastructure.PublishedCache.PublishedSnapshotServiceOptions
        {
            IgnoreLocalDb = true
        };

        builder.Services.AddSingleton(settings);
    }
}

/// <summary>
///     Configures the index options to construct the Examine indexes
/// </summary>
public sealed class ConfigureExamineIndexes : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
{
    public void Configure(string? name, LuceneDirectoryIndexOptions options)
    {
        options.DirectoryFactory = new LuceneRAMDirectoryFactory();
    }

    public void Configure(LuceneDirectoryIndexOptions options)
        => throw new NotImplementedException("This is never called and is just part of the interface");
}
