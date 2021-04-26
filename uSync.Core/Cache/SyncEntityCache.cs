
using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace uSync.Core.Cache
{
    /// <summary>
    ///  A Simple cache for entity lookup, the cache is emptied at the end of a sync run.
    /// </summary>
    /// <remarks>
    ///  This exists primarly to stop us having double lookups as part of the sync, its 
    ///  not actually that much faster for most operations, it really helps when we 
    ///  are working out parent values, so on deep trees. 
    /// </remarks>
    public class SyncEntityCache
    {
        private readonly DictionaryAppCache cache = new DictionaryAppCache();
        private readonly DictionaryAppCache keyCache = new DictionaryAppCache();

        private readonly IEntityService entityService;

        private bool _cacheEnabled;

        public SyncEntityCache(IEntityService entityService)
        {
            this.entityService = entityService;
            this._cacheEnabled = true;
        }

        public IEntitySlim GetEntity(int id)
        {
            if (!_cacheEnabled) return entityService.Get(id);

            return cache.GetCacheItem(id.ToString(), () =>
            {
                return entityService.Get(id);
            });
        }

        public IEntitySlim GetEntity(int id, UmbracoObjectTypes objectType)
        {
            if (!_cacheEnabled) return entityService.Get(id, objectType);

            return cache.GetCacheItem(id.ToString(), () =>
            {
                return entityService.Get(id, objectType);
            });
        }

        public IEntitySlim GetEntity(Guid id)
        {
            if (!_cacheEnabled) return entityService.Get(id);

            // double cache lookup, we only store id's in the key cache,
            // that way we are not double storing all the entityIds in memory.

            IEntitySlim entity = null;
            var intId = keyCache.GetCacheItem(id.ToString(), () =>
            {
                entity = entityService.Get(id);
                if (entity != null) return entity.Id;

                return 0;
            });

            if (intId != 0)
            {
                return cache.GetCacheItem(intId.ToString(), () =>
                {
                    if (entity != null)
                    {
                        return entity;
                    }
                    return entityService.Get(intId);
                });
            }
            else
            {
                keyCache.ClearByKey(id.ToString());
                return null;
            }
        }
        public IEntitySlim GetEntity(Guid id, UmbracoObjectTypes objectType)
        {
            if (!_cacheEnabled) return entityService.Get(id, objectType);

            IEntitySlim entity = null;
            var intId = keyCache.GetCacheItem(id.ToString(), () =>
            {
                entity = entityService.Get(id, objectType);
                if (entity != null) return entity.Id;

                return 0;
            });

            if (intId != 0)
            {
                return cache.GetCacheItem(intId.ToString(), () =>
                {
                    if (entity != null)
                    {
                        return entity;
                    }

                    return entityService.Get(intId, objectType);
                });
            }
            else
            {
                keyCache.ClearByKey(id.ToString());
                return null;
            }
        }


        public IEnumerable<IEntitySlim> GetAll(UmbracoObjectTypes objectType, int[] ids)
        {
            if (!_cacheEnabled) return entityService.GetAll(objectType, ids);

            var items = new List<IEntitySlim>();
            var uncachedIds = new List<int>();
            foreach (var id in ids)
            {
                var cachedItem = cache.GetCacheItem<IEntitySlim>(id.ToString());
                if (cachedItem != null)
                {
                    items.Add(cachedItem);
                }
                else
                {
                    uncachedIds.Add(id);
                }
            }

            // if you call this with blank you get everything! 
            if (uncachedIds.Count > 0)
            {
                var remaining = entityService.GetAll(objectType, uncachedIds.ToArray()).ToList();
                foreach (var item in remaining)
                {
                    items.Add(cache.GetCacheItem(item.Id.ToString(), () => { return item; }));
                }
            }

            return items;
        }

        public void Clear()
        {
            cache.Clear();
            keyCache.Clear();
        }
    }
}
