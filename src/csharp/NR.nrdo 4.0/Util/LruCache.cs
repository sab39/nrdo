using System;
using System.Collections.Generic;
using System.Text;

namespace NR.nrdo.Util
{
    public interface ICache
    {
        int Capacity { get; set; }
        int Count { get;}
        int FlushCount { get;}
        int PeakCount { get; }
        void Clear();
        int Cost { get; }
        int PeakCost { get; }
        int CyclePeakCost();
    }
    public interface IListCache : ICache
    {
        int ItemCapacity { get; set; }
        int ItemCount { get;}
        int PeakItemCount { get; }
    }
    public class LruCache<TKey, TValue> : IDictionary<TKey, TValue>, ICache
    {
        private class Entry
        {
            public TKey key;
            public TValue value;
            public Entry next;
            public Entry previous;

            public Entry(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        private Entry anchor = new Entry(default(TKey), default(TValue));
        private Dictionary<TKey, Entry> entries = new Dictionary<TKey, Entry>();

        public LruCache(int capacity)
        {
            Capacity = capacity;
            Clear();
        }
        public LruCache(IDictionary<TKey, TValue> dict, int capacity)
            : this(capacity)
        {
            lock (this)
            {
                if (Capacity > dict.Count) Capacity = dict.Count;
                foreach (KeyValuePair<TKey, TValue> item in dict)
                {
                    Add(item);
                }
            }
        }

        public virtual void Add(TKey key, TValue value)
        {
            lock (this)
            {
                if (ContainsKey(key)) throw new ArgumentException("Key already in dictionary");
                Entry entry = new Entry(key, value);
                entries.Add(key, entry);
                entry.previous = anchor;
                entry.next = anchor.next;
                anchor.next = entry;
                entry.next.previous = entry;
                shrinkToCapacity();
            }
        }

        private int capacity;
        public int Capacity
        {
            get { return capacity; }
            set
            {
                lock (this)
                {
                    if (value > capacity) IsOverflowing = false;
                    capacity = value;
                    if (capacity < 0) capacity = 0;
                    shrinkToCapacity();
                }
            }
        }

        protected virtual bool EvictionNeeded
        {
            get { lock (this) { return Count > Capacity; } }
        }

        public virtual int Cost { get { return Count; } }
        public virtual int PeakCost { get { return PeakCount; } }

        public int Count { get { lock (this) { return entries.Count; } } }
        public ICollection<TKey> Keys { get { lock (this) { return entries.Keys; } } }
        public bool ContainsKey(TKey key) { lock (this) { return entries.ContainsKey(key); } }

        private int flushCount;
        public int FlushCount { get { lock (this) { return flushCount; } } }

        private int peakCount;
        public int PeakCount
        {
            get
            {
                lock (this)
                {
                    return Count > peakCount ? Count : peakCount;
                }
            }
        }

        private int cyclePeakCost;
        public int CyclePeakCost()
        {
            var result = cyclePeakCost;
            cyclePeakCost = Cost;
            return result;
        }

        private void moveToTop(Entry entry)
        {
            lock (this)
            {
                entry.previous.next = entry.next;
                entry.next.previous = entry.previous;
                entry.previous = anchor;
                entry.next = anchor.next;
                anchor.next.previous = entry;
                anchor.next = entry;
            }
        }

        public virtual TValue this[TKey key]
        {
            get
            {
                lock (this)
                {
                    Entry entry = entries[key];
                    moveToTop(entry);
                    return entry.value;
                }
            }
            set
            {
                lock (this)
                {
                    if (!entries.ContainsKey(key))
                    {
                        Add(key, value);
                    }
                    else
                    {
                        Entry entry = (Entry) entries[key];
                        moveToTop(entry);
                        entry.value = value;
                    }
                }
            }
        }

        public bool Remove(TKey key)
        {
            lock (this)
            {
                if (!entries.ContainsKey(key)) return false;
                remove(entries[key]);
                return true;
            }
        }

        private void remove(Entry entry)
        {
            lock (this)
            {
                entry.previous.next = entry.next;
                entry.next.previous = entry.previous;
                entries.Remove(entry.key);
            }
        }

        public bool IsOverflowing { get; protected set; }

        protected void shrinkToCapacity()
        {
            lock (this)
            {
                while (EvictionNeeded)
                {
                    IsOverflowing = true;
                    remove(anchor.previous);
                }
                if (Count > peakCount) peakCount = Count;
                if (Cost > cyclePeakCost) cyclePeakCost = Cost;
            }
        }

        public void Clear()
        {
            lock (this)
            {
                IsOverflowing = false;
                if (Count > 0) flushCount++;
                anchor.next = anchor;
                anchor.previous = anchor;
                entries.Clear();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this)
            {
                if (ContainsKey(key))
                {
                    value = this[key];
                    return true;
                }
                else
                {
                    value = default(TValue);
                    return false;
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (this)
                {
                    List<TValue> values = new List<TValue>();
                    foreach (KeyValuePair<TKey, TValue> item in this)
                    {
                        values.Add(item.Value);
                    }
                    return values;
                }
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (this)
            {
                return ContainsKey(item.Key) && object.Equals(this[item.Key], item.Value);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (this)
            {
                foreach (KeyValuePair<TKey, TValue> item in this)
                {
                    array[arrayIndex++] = item;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (this)
            {
                if (Contains(item))
                {
                    if (Count == 1) IsOverflowing = false;
                    return Remove(item.Key);
                }
                return false;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, Entry> item in entries)
            {
                yield return new KeyValuePair<TKey, TValue>(item.Key, item.Value.value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ListLruCache<TKey, TValue> : LruCache<TKey, List<TValue>>, IListCache
    {
        public ListLruCache(int capacity, int itemCapacity) : base(capacity)
        {
            this.itemCapacity = itemCapacity;
        }
        public ListLruCache(IDictionary<TKey, List<TValue>> dict, int capacity, int itemCapacity)
            : this(capacity, itemCapacity)
        {
            lock (this)
            {
                foreach (KeyValuePair<TKey, List<TValue>> item in dict)
                {
                    if (this.itemCapacity < ItemCount + item.Value.Count)
                    {
                        this.itemCapacity = ItemCount + item.Value.Count;
                    }
                    Add(item);
                }
            }
        }
        protected override bool EvictionNeeded
        {
            get
            {
                lock (this)
                {
                    return base.EvictionNeeded && ItemCount > ItemCapacity;
                }
            }
        }
        private int itemCapacity = int.MaxValue;
        public int ItemCapacity
        {
            get { return itemCapacity; }
            set
            {
                lock (this)
                {
                    if (value > itemCapacity) IsOverflowing = false;
                    itemCapacity = value;
                    if (itemCapacity < 0) itemCapacity = 0;
                    shrinkToCapacity();
                }
            }
        }
        public int ItemCount
        {
            get
            {
                lock (this)
                {
                    int count = 0;
                    foreach (KeyValuePair<TKey, List<TValue>> item in this)
                    {
                        count += item.Value.Count + 1;
                    }
                    return count;
                }
            }
        }
        private int peakItemCount;
        public int PeakItemCount
        {
            get
            {
                lock (this)
                {
                    return ItemCount > peakItemCount ? ItemCount : peakItemCount;
                }
            }
        }
        public override int Cost { get { return ItemCount; } }
        public override int PeakCost { get { return PeakItemCount; } }

        public override void Add(TKey key, List<TValue> value)
        {
            lock (this)
            {
                base.Add(key, value);
                peakItemCount = PeakItemCount;
            }
        }
        // Assigning a new value to an existing item could make it larger and hence require shrinking, based on
        // ItemCount, or increase the value of PeakItemCount
        public override List<TValue> this[TKey key]
        {
            set
            {
                lock (this)
                {
                    base[key] = value;
                    shrinkToCapacity();
                    peakItemCount = PeakItemCount;
                }
            }
        }
    }
}
