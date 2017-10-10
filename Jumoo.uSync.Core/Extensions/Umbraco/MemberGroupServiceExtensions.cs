using System;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Extensions.Umbraco
{
    public static class MemberGroupServiceExtensions
    {
        public static IMemberGroup GetByKey(this IMemberGroupService service, Guid key)
        {
            return service.GetAll().FirstOrDefault(u => u.Key == key);
        }
    }
}