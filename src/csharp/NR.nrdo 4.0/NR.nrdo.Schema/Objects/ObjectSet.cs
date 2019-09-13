using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using NR.nrdo.Schema.Shared;

namespace NR.nrdo.Schema.Objects
{
    public static class ObjectSet
    {
        public static ObjectSet<T> Create<T, V>(IEnumerable<V> values, Func<V, Identifier> getIdentifier, Func<V, T> getValue)
        {
            return ObjectSet<T>.Create(values, getIdentifier, getValue);
        }
    }
    public class ObjectSet<T> : IEnumerable<KeyValuePair<Identifier, T>>
    {
        private static readonly ObjectSet<T> empty = new ObjectSet<T>(ImmutableDictionary<ObjectType, ImmutableDictionary<string, T>>.Empty);
        public static ObjectSet<T> Empty { get { return empty; } }

        private readonly ImmutableDictionary<ObjectType, ImmutableDictionary<string, T>> objects;
        private ObjectSet(ImmutableDictionary<ObjectType, ImmutableDictionary<string, T>> objects)
        {
            this.objects = objects;
        }

        public static ObjectSet<T> Create<V>(IEnumerable<V> values, Func<V, Identifier> getIdentifier, Func<V, T> getValue)
        {
            try
            {
                var byType = from item in values
                             let kv = Helpers.KeyValuePair(getIdentifier(item), getValue(item))
                             group kv by kv.Key.ObjectType into type
                             select Helpers.KeyValuePair(type.Key, type.ToImmutableDictionary(i => i.Key.Name, i => i.Value, Nstring.DBEquivalentComparer));

                return new ObjectSet<T>(byType.ToImmutableDictionary());
            }
            catch
            {
                throw new ApplicationException("Duplicates found in " + string.Join(", ", values));
            }
        }

        private static KeyValuePair<Identifier, T> getKVPair(ObjectType type, string name, T value)
        {
            return Helpers.KeyValuePair(new Identifier(type, name), value);
        }
        private static KeyValuePair<Identifier, T> getKVPair(ObjectType type, KeyValuePair<string, T> kv)
        {
            return getKVPair(type, kv.Key, kv.Value);
        }

        public IEnumerator<KeyValuePair<Identifier, T>> GetEnumerator()
        {
            return (from typeKv in objects
                    from kv in typeKv.Value
                    select getKVPair(typeKv.Key, kv)
                    ).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<KeyValuePair<Identifier, T>> OfType(ObjectType type)
        {
            ImmutableDictionary<string, T> subDictionary;
            if (!objects.TryGetValue(type, out subDictionary)) return Enumerable.Empty<KeyValuePair<Identifier, T>>();

            return from kv in subDictionary select getKVPair(type, kv);
        }

        public IEnumerable<T> ValuesOfType(ObjectType type)
        {
            ImmutableDictionary<string, T> subDictionary;
            return objects.TryGetValue(type, out subDictionary) ? subDictionary.Values : Enumerable.Empty<T>();
        }

        public bool ContainsKey(Identifier ident)
        {
            ImmutableDictionary<string, T> subDictionary;
            return objects.TryGetValue(ident.ObjectType, out subDictionary) && subDictionary.ContainsKey(ident.Name);
        }

        public bool TryGetValue(Identifier ident, out T value)
        {
            value = default(T);

            ImmutableDictionary<string, T> subDictionary;
            return objects.TryGetValue(ident.ObjectType, out subDictionary) &&
                   subDictionary.TryGetValue(ident.Name, out value);
        }

        public T this[Identifier ident]
        {
            get
            {
                T result;
                if (!TryGetValue(ident, out result)) throw new IndexOutOfRangeException("No " + ident + " found in ObjectSet");
                return result;
            }
        }

        public ICollection<Identifier> Keys
        {
            get
            {
                return (from typeKv in objects
                        from kv in typeKv.Value
                        select new Identifier(typeKv.Key, kv.Key)).ToImmutableList();
            }
        }

        public ICollection<T> Values
        {
            get
            {
                return (from subDictionary in objects.Values
                        from value in subDictionary.Values
                        select value).ToImmutableList();
            }
        }

        public bool Contains(KeyValuePair<Identifier, T> item)
        {
            T value;
            return TryGetValue(item.Key, out value) && object.Equals(item.Value, value);
        }

        public int Count
        {
            get { return (from subDictionary in objects.Values select subDictionary.Count).Sum(); }
        }

        private ObjectSet<T> withObjects(ImmutableDictionary<ObjectType, ImmutableDictionary<string, T>> newObjects)
        {
            if (object.ReferenceEquals(newObjects, objects)) return this;
            return new ObjectSet<T>(newObjects);
        }

        private static ImmutableDictionary<ObjectType, ImmutableDictionary<string, T>>
            setObjects(ImmutableDictionary<ObjectType, ImmutableDictionary<string, T>> objects, ObjectType type, IEnumerable<KeyValuePair<string, T>> newItems)
        {
            ImmutableDictionary<string, T> subDictionary;
            if (objects.TryGetValue(type, out subDictionary))
            {
                var newSub = subDictionary.SetItems(newItems);
                return objects.SetItem(type, newSub);
            }
            else
            {
                return objects.SetItem(type, newItems.ToImmutableDictionary(Nstring.DBEquivalentComparer));
            }
        }

        private static ImmutableDictionary<ObjectType, ImmutableDictionary<string, T>>
            removeObjects(ImmutableDictionary<ObjectType, ImmutableDictionary<string, T>> objects, ObjectType type, IEnumerable<string> names)
        {
            ImmutableDictionary<string, T> subDictionary;
            if (!objects.TryGetValue(type, out subDictionary)) return objects;

            var newSub = subDictionary.RemoveRange(names);
            if (newSub.IsEmpty)
            {
                return objects.Remove(type);
            }
            else
            {
                return objects.SetItem(type, newSub);
            }
        }

        public ObjectSet<T> With(Identifier identifier, T value)
        {
            return withObjects(setObjects(objects, identifier.ObjectType,
                Helpers.EnumerableFrom(Helpers.KeyValuePair(identifier.Name, value))));
        }

        public ObjectSet<T> With(IEnumerable<KeyValuePair<Identifier, T>> items)
        {
            var objects = this.objects;
            var types = from item in items
                        group item by item.Key.ObjectType into g
                        select g;

            foreach (var type in types)
            {
                objects = setObjects(objects, type.Key, from item in type select Helpers.KeyValuePair(item.Key.Name, item.Value));
            }

            return withObjects(objects);
        }

        public ObjectSet<T> Without(Identifier identifier)
        {
            return withObjects(removeObjects(objects, identifier.ObjectType, Helpers.EnumerableFrom(identifier.Name)));
        }

        public ObjectSet<T> Without(IEnumerable<Identifier> identifiers)
        {
            var objects = this.objects;
            var types = from ident in identifiers
                        group ident by ident.ObjectType into g
                        select g;

            foreach (var type in types)
            {
                objects = removeObjects(objects, type.Key, from ident in type select ident.Name);
            }

            return withObjects(objects);
        }
    }
}
