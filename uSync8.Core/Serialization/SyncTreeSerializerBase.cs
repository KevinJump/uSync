using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization
{
    public abstract class SyncTreeSerializerBase<TObject> : SyncSerializerBase<TObject>
        where TObject : ITreeEntity
    {
        protected SyncTreeSerializerBase(IEntityService entityService)
            : base(entityService)
        {
        }

        protected abstract TObject CreateItem(string alias, TObject parent, ITreeEntity treeItem, string itemType);


        #region Getters
        // Getters - get information we already know (either in the object or the XElement)
        protected virtual string GetItemBaseType(XElement node)
            => string.Empty;
        
        #endregion

        #region Finders 
        // Finders - used on importing, getting things that are already there (or maybe not)

        protected abstract TObject FindOrCreate(XElement node);

        protected TObject FindItem(Guid key, string alias)
        {
            var item = FindItem(key);
            if (item != null) return item;

            return FindItem(alias);
        }

        #endregion


    }
}
