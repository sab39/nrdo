using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Util;

namespace NR.nrdo.Caching
{
    public abstract class TableMultiObjectCache<T, TWhere, TCache> : DBMultiObjectCache<T, TWhere, TCache>, ITableMultiObjectCache<T>
        where T : DBTableObject<T>
        where TWhere : CachingWhereBase<T, TWhere, TCache>
        where TCache : TableMultiObjectCache<T, TWhere, TCache>
    {
        #region DataModification helpers
        // Documentation for which helper should be used in which scenarios is in the csharp4-table.cgl

        // This should be overridden if any of the 'ByGettingWhere' methods are to be used for a cache
        // It's implementable if the get has no fields on other tables, and no params.
        protected virtual TWhere GetWhereByObject(T t)
        {
            throw new NotImplementedException();
        }

        protected void DeleteByGettingWhere(T t)
        {
            var twhere = GetWhereByObject(t);
            if (LruCache.ContainsKey(twhere))
            {
                LruCache[twhere] = LruCache[twhere].Where(item => !t.PkeyEquals(item)).ToList();
                HitInfo.tweakedDirectly++;
            }
        }

        protected void ClearByGettingWhere(T t)
        {
            if (LruCache.Remove(GetWhereByObject(t)))
            {
                HitInfo.tweakedDirectly++;
            }
        }

        protected void CascadeByWhere(TWhere where)
        {
            if (LruCache.Remove(where))
            {
                HitInfo.tweakedDirectly++;
            }
        }

        protected void DeleteByIteration(T t)
        {
            if (LruCache.ItemCount > IterationLimit)
            {
                Clear();
            }
            else
            {
                var tweaked = false;
                foreach (var kv in LruCache.ToList())
                {
                    if (kv.Value.Any(item => t.PkeyEquals(item)))
                    {
                        tweaked = true;
                        LruCache[kv.Key] = (from item in kv.Value where !t.PkeyEquals(item) select item).ToList();
                    }
                }
                HitInfo.iterated++;
                if (tweaked) HitInfo.tweakedByIteration++;
            }
        }

        protected void ClearByIteration(T t)
        {
            if (LruCache.ItemCount > IterationLimit)
            {
                Clear();
            }
            else
            {
                var tweaked = false;
                foreach (var kv in LruCache.ToList())
                {
                    if (kv.Value.Any(item => t.PkeyEquals(item)))
                    {
                        tweaked = true;
                        LruCache.Remove(kv.Key);
                    }
                }
                HitInfo.iterated++;
                if (tweaked) HitInfo.tweakedByIteration++;
            }
        }
        #endregion
        
        protected TableMultiObjectCache(int capacity, int itemCapacity)
            : base(capacity, itemCapacity)
        {
            DBTableObject<T>.DataModification.Insert += ReactToInsert;
            DBTableObject<T>.DataModification.Update += ReactToUpdate;
            DBTableObject<T>.DataModification.Delete += ReactToDelete;
            DBTableObject<T>.DataModification.FullFlush += Clear;

            // This does not react to cascades individually; it is the responsibility of each implementation to hook up the cascades that
            // can happen on it, or to hook up to CascadeDelete if it can't handle them individually.
            // Double-cascades turn into FullFlush.
        }

        public virtual void ReactToInsert(T t)
        {
            // Only cache entries that match T's values need clearing
            // That means you can generate a Where for the cache based on T's values, for each cache
            // If the get is unique (ie get single without noindex) then you can *insert* a clone of t into that cache
            // Otherwise remove the entry for that key
            Clear();
        }

        public virtual void ReactToUpdate(T t)
        {
            // Cache entries need clearing as if for a delete and an insert:
            Clear();
        }

        public virtual void ReactToDelete(T t)
        {
            // Depends on whether the cache is on readonly fields or not.
            // If the fields are readonly, then use the field values to generate a Where for the cache based on T's values and eliminate that entry
            // If the fields are readwrite, what to do depends on the size of the cache. If it's small, iterate over it looking for the primary key
            // of the item being deleted, and wipe out any items that match (including perhaps piecewise removing them from the lists in a multi get rather than wiping
            // out the entire entry). If the cache is large and the fields are writable, just wipe it.
            Clear();
        }

        // On cascade...

        // What to do depends on TOther and the nature of the get, AND of the join between TOther and this
        // It MAY be possible to generate a where key from the TOther and then use that as a key into the cache, in which case that should be done
        // and used to delete the key if present.
        // If it isn't, what to do depends on the size of the cache; if it's large, just clear it, but if it's smallish, iterate over it
        // looking for any keys that would join to the item being cascade deleted, and wipe them out.
        // NOTE: more thought needed here in the (rare?) case where the field joined from the other table are not readonly. But probably BECAUSE
        // it's rare and requires more thought, the right thing to do is just NOT think about it and blindly clear in that case.
    }
}
