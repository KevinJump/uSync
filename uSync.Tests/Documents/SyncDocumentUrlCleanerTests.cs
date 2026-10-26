using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;

using uSync.Core.Documents;

namespace uSync.Tests.Documents;

[TestFixture]
public class SyncDocumentUrlCleanerTests
{
    [Test]
    public void KeyChange_Cleans_Urls_And_UrlAliases_For_OldKey()
    {
        var key = Guid.NewGuid();

        var urlRepository = new Mock<IDocumentUrlRepository>();
        var aliasRepository = new Mock<IDocumentUrlAliasRepository>();
        var scopeProvider = new Mock<ICoreScopeProvider>();
        scopeProvider
            .Setup(x => x.CreateCoreScope(
                It.IsAny<System.Data.IsolationLevel>(), It.IsAny<RepositoryCacheMode>(),
                It.IsAny<Umbraco.Cms.Core.Events.IEventDispatcher>(), It.IsAny<Umbraco.Cms.Core.Events.IScopedNotificationPublisher>(),
                It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(Mock.Of<ICoreScope>());

        var cleaner = new SyncDocumentUrlCleaner(
            urlRepository.Object,
            aliasRepository.Object,
            NullLogger<SyncDocumentUrlCleaner>.Instance,
            scopeProvider.Object);

        cleaner.CleanUrlsForDocument(key);

        urlRepository.Verify(x => x.DeleteByDocumentKey(It.Is<IEnumerable<Guid>>(k => k.Single() == key)), Times.Once);
        aliasRepository.Verify(x => x.DeleteByDocumentKey(It.Is<IEnumerable<Guid>>(k => k.Single() == key)), Times.Once);
    }
}
