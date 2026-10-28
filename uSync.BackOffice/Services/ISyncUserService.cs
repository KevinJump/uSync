using System.Threading.Tasks;

namespace uSync.BackOffice.Services;

/// <summary>
///  works out which Umbraco user an import runs as.
/// </summary>
public interface ISyncUserService
{
    /// <summary>
    ///  get the id of the user an import should run as.
    /// </summary>
    /// <remarks>
    ///  a <paramref name="requestedUserId"/> that isn't <c>Constants.Security.UnknownUserId</c> is used as is.
    ///  Otherwise it's the current backoffice user, then the configured background user
    ///  (<c>uSync:Settings:BackgroundUser</c>) and finally the superuser.
    /// </remarks>
    Task<int> GetImportUserIdAsync(int requestedUserId);
}
