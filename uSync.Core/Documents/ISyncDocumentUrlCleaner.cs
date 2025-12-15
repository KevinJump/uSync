
namespace uSync.Core.Documents;

public interface ISyncDocumentUrlCleaner
{
    void CleanUrlsForDocument(Guid key);
}