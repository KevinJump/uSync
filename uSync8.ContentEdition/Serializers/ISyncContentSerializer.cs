using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models.Entities;

namespace uSync8.ContentEdition.Serializers
{
    public interface ISyncContentSerializer<TObject>
    {
        int GetLevel(TObject item);
    }
}
