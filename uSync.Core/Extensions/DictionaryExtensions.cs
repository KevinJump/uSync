using System;
using System.Collections.Generic;

using Umbraco.Cms.Core.Models.Membership;

namespace uSync.Core.Extensions;
internal static class DictionaryExtensions
{
    /// <summary>
    ///  get the username
    /// </summary>
    /// <param name="usernames"></param>
    /// <param name="id"></param>
    /// <param name="findMethod"></param>
    /// <returns></returns>

    public static string GetUsername(this Dictionary<int, string> usernames, int? id, Func<int, IUser> findMethod)
    {
        if (usernames == null || id == null) return "unknown";

        usernames[id.Value] = usernames.ContainsKey(id.Value)
            ? usernames[id.Value]
            : findMethod(id.Value)?.Email ?? "unknown";

        return usernames[id.Value];
    }

    public static int GetEmails(this Dictionary<string, int> emails, string email, Func<string, IUser> findMethod)
    {
        if (emails == null || string.IsNullOrEmpty(email)) return -1;

        emails[email] = emails.ContainsKey(email)
            ? emails[email]
            : findMethod(email)?.Id ?? -1;

        return emails[email];
    }
}
