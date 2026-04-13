namespace uSync.Core.Versions;

/// <summary>
///  service to help us update image urls when we move between sites.
/// </summary>
/// <remarks>
///  with the advent of the HMAC secret key, the image urls have a hash value in them to prevent tampering. When we move between sites,
///  the hash value will be different, so we need to update the image urls to have the correct hash value for the new site.
/// </remarks>
public interface ISyncImageUpdateHelper
{
    /// <summary>
    ///  update any image urls in the provided html to have the correct HMAC hash value for the current site. 
    ///  If the HMAC secret key is not set, this will return the original html unmodified.
    /// </summary>
    string UpdateImageUrlValues(string html);
}