using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Shared;

namespace NR.nrdo.Schema.State
{
    public class DatabaseState
    {
        private sealed class RootObject
        {
            internal readonly RootObjectState objectState;
            internal readonly ObjectSet<SubObjectState> children;

            internal RootObject(RootObjectState objectState, ObjectSet<SubObjectState> children)
            {
                this.objectState = objectState;
                this.children = children;
            }

            internal RootObject WithState(RootObjectState newState)
            {
                if (object.ReferenceEquals(newState, objectState)) return this;
                return new RootObject(newState, children);
            }

            internal RootObject WithChildren(ObjectSet<SubObjectState> newChildren)
            {
                if (object.ReferenceEquals(newChildren, children)) return this;
                return new RootObject(objectState, newChildren);
            }
        }

        private readonly ObjectSet<RootObject> rootObjects;
        private readonly Func<Identifier, bool> rootIdentifierFilter;
        private readonly Func<Identifier, Identifier, bool> subIdentifierFilter;

        private DatabaseState(ObjectSet<RootObject> rootObjects, Func<Identifier, bool> rootIdentifierFilter, Func<Identifier, Identifier, bool> subIdentifierFilter)
        {
            this.rootObjects = rootObjects;
            this.rootIdentifierFilter = rootIdentifierFilter;
            this.subIdentifierFilter = subIdentifierFilter;
        }

        private static readonly DatabaseState empty = new DatabaseState(ObjectSet<RootObject>.Empty, null, null);
        public static DatabaseState Empty { get { return empty; } }

        public static DatabaseState Create(IEnumerable<RootObjectState> roots, IEnumerable<SubObjectState> subs)
        {
            var dbState = new DatabaseState(
                ObjectSet.Create(roots,
                    root => root.Identifier,
                    root => new RootObject(root, ObjectSet.Create(
                        from sub in subs where sub.ParentIdentifier == root.Identifier select sub,
                        sub => sub.Identifier,
                        sub => sub))), null, null);

            if (subs.Any(sub => !dbState.Contains(sub)))
            {
                throw new ArgumentException("DatabaseState contains sub-objects (" + string.Join(", ", from sub in subs where !dbState.Contains(sub) select sub.ParentIdentifier + "." + sub.Identifier) + ") that do not have parents");
            }

            return dbState;
        }

        public static DatabaseState Create(IEnumerable<ObjectState> objects)
        {
            var objList = objects.ToList();
            return Create(objList.OfType<RootObjectState>(), objList.OfType<SubObjectState>());
        }

        private bool isRootVisible(Identifier root)
        {
            return rootIdentifierFilter == null || rootIdentifierFilter(root);
        }
        private bool isChildVisibleIfParentIs(Identifier parent, Identifier sub)
        {
            return subIdentifierFilter == null || subIdentifierFilter(parent, sub);
        }
        private bool isChildVisible(Identifier parent, Identifier sub)
        {
            return isRootVisible(parent) && isChildVisibleIfParentIs(parent, sub);
        }

        public int Count
        {
            get
            {
                if (rootIdentifierFilter == null && subIdentifierFilter == null)
                {
                    // Shortcut when no filter applies
                    return rootObjects.Values.Sum(root => root.children.Count + 1);
                }
                else
                {
                    return AllObjects.Count();
                }
            }
        }

        public IEnumerable<RootObjectState> RootObjects
        {
            get
            {
                return from obj in rootObjects
                       where isRootVisible(obj.Key)
                       select obj.Value.objectState;
            }
        }

        public IEnumerable<RootObjectState> GetRoots(ObjectType type)
        {
            return from root in rootObjects.ValuesOfType(type)
                   where isRootVisible(root.objectState.Identifier)
                   select root.objectState;
        }
        public IEnumerable<RootObjectState<TType, TState>> GetRoots<TType, TState>(RootObjectType<TType, TState> type)
            where TType : RootObjectType<TType, TState>, new()
        {
            return GetRoots((ObjectType)type).Cast<RootObjectState<TType, TState>>();
        }

        public IEnumerable<SubObjectState> GetChildren(Identifier parentIdentifier)
        {
            if (!ContainsRoot(parentIdentifier)) return Enumerable.Empty<SubObjectState>();

            return from sub in rootObjects[parentIdentifier].children
                   where isChildVisibleIfParentIs(parentIdentifier, sub.Key)
                   select sub.Value;
        }
        public IEnumerable<SubObjectState> GetChildren(Identifier parentIdentifier, ObjectType type)
        {
            if (!ContainsRoot(parentIdentifier)) return Enumerable.Empty<SubObjectState>();

            return from sub in rootObjects[parentIdentifier].children.ValuesOfType(type)
                   where isChildVisibleIfParentIs(parentIdentifier, sub.Identifier)
                   select sub;
        }
        public IEnumerable<SubObjectState<TType, TState>> GetChildren<TType, TState>(Identifier parentIdentifier, SubObjectType<TType, TState> type)
            where TType : SubObjectType<TType, TState>, new()
        {
            return GetChildren(parentIdentifier, (ObjectType)type).Cast<SubObjectState<TType, TState>>();
        }

        public IEnumerable<SubObjectState> AllChildren
        {
            get
            {
                return from root in rootObjects.Values
                       where isRootVisible(root.objectState.Identifier)
                       from sub in root.children.Values
                       where isChildVisibleIfParentIs(sub.ParentIdentifier, sub.Identifier)
                       select sub;
            }
        }
        public IEnumerable<SubObjectState> GetAllChildren(ObjectType type)
        {
            return from root in rootObjects.Values
                   where isRootVisible(root.objectState.Identifier)
                   from sub in root.children.ValuesOfType(type)
                   where isChildVisibleIfParentIs(sub.ParentIdentifier, sub.Identifier)
                   select sub;
        }
        public IEnumerable<SubObjectState<TType, TState>> GetAllChildren<TType, TState>(SubObjectType<TType, TState> type)
            where TType : SubObjectType<TType, TState>, new()
        {
            return GetAllChildren((ObjectType)type).Cast<SubObjectState<TType, TState>>();
        }

        public IEnumerable<ObjectState> AllObjects
        {
            get
            {
                return Enumerable.Concat<ObjectState>(RootObjects,
                                                      from root in rootObjects
                                                      where isRootVisible(root.Key)
                                                      from sub in root.Value.children
                                                      where isChildVisibleIfParentIs(root.Key, sub.Key)
                                                      select sub.Value);
            }
        }

        public bool ContainsRoot(Identifier identifier)
        {
            return isRootVisible(identifier) && rootObjects.ContainsKey(identifier);
        }
        public bool Contains(RootObjectState state)
        {
            return ContainsRoot(state.Identifier);
        }

        public bool ContainsChild(Identifier parentIdentifier, Identifier childIdentifier)
        {
            return ContainsRoot(parentIdentifier) &&
                isChildVisibleIfParentIs(parentIdentifier, childIdentifier) &&
                rootObjects[parentIdentifier].children.ContainsKey(childIdentifier);
        }
        public bool Contains(SubObjectState child)
        {
            return ContainsChild(child.ParentIdentifier, child.Identifier);
        }

        public bool Contains(ObjectState state)
        {
            var child = state as SubObjectState;
            return child != null ? Contains(child) : Contains((RootObjectState)state);
        }

        public RootObjectState GetRoot(Identifier identifier)
        {
            if (!ContainsRoot(identifier)) return null;
            return rootObjects[identifier].objectState;
        }
        public RootObjectState<TType, TState> Get<TType, TState>(RootObjectState<TType, TState> root)
            where TType : RootObjectType<TType, TState>, new()
        {
            return (RootObjectState<TType, TState>)GetRoot(root.Identifier);
        }

        public SubObjectState GetChild(Identifier parentIdentifier, Identifier childIdentifier)
        {
            if (!ContainsRoot(parentIdentifier)) return null;
            var root = rootObjects[parentIdentifier];

            if (!isChildVisibleIfParentIs(parentIdentifier, childIdentifier) || !root.children.ContainsKey(childIdentifier)) return null;
            return root.children[childIdentifier];
        }
        public SubObjectState<TType, TState> Get<TType, TState>(SubObjectState<TType, TState> child)
            where TType : SubObjectType<TType, TState>, new()
        {
            return (SubObjectState<TType, TState>)GetChild(child.ParentIdentifier, child.Identifier);
        }

        public ObjectState Get(ObjectState state)
        {
            var child = state as SubObjectState;
            if (child != null) return GetChild(child.ParentIdentifier, child.Identifier);

            return GetRoot(((RootObjectState)state).Identifier);
        }

        private DatabaseState withObjects(ObjectSet<RootObject> newObjects)
        {
            if (object.ReferenceEquals(rootObjects, newObjects)) return this;
            return new DatabaseState(newObjects, rootIdentifierFilter, subIdentifierFilter);
        }

        public DatabaseState WithFilters(Func<Identifier, bool> rootIdentifierFilter, Func<Identifier, Identifier, bool> subIdentifierFilter)
        {
            if (object.ReferenceEquals(rootIdentifierFilter, this.rootIdentifierFilter) &&
                object.ReferenceEquals(subIdentifierFilter, this.subIdentifierFilter)) return this;
            return new DatabaseState(rootObjects, rootIdentifierFilter, subIdentifierFilter);
        }

        public DatabaseState WithoutFilters()
        {
            return WithFilters(null, null);
        }

        public DatabaseState With(IEnumerable<RootObjectState> newRoots, IEnumerable<SubObjectState> newSubs)
        {
            var newObjects = this.rootObjects;

            if (newRoots != null)
            {
                // We don't attempt to extract the sub-objects here because it's more or less the same cost to
                // add them in the second pass, and simplifies the code.
                newObjects = newObjects.With(from root in newRoots select Helpers.KeyValuePair(root.Identifier, new RootObject(root, ObjectSet<SubObjectState>.Empty)));
            }

            if (newSubs != null)
            {
                newObjects = newObjects.With(from sub in newSubs
                                             group sub by sub.ParentIdentifier into root
                                             let existing = newObjects[root.Key]
                                             select Helpers.KeyValuePair(root.Key,
                                                 existing.WithChildren(existing.children.With(from sub in root select Helpers.KeyValuePair(sub.Identifier, sub)))));
            }

            return withObjects(newObjects);
        }

        public DatabaseState With(RootObjectState newObject)
        {
            return With(Helpers.EnumerableFrom(newObject), null);
        }

        public DatabaseState With(SubObjectState newObject)
        {
            return With(null, Helpers.EnumerableFrom(newObject));
        }

        public DatabaseState With(IEnumerable<ObjectState> newObjects)
        {
            var objList = newObjects.ToList();
            return With(objList.OfType<RootObjectState>(), objList.OfType<SubObjectState>());
        }

        public DatabaseState Without(IEnumerable<RootObjectState> removeRoots, IEnumerable<SubObjectState> removeSubs)
        {
            var newObjects = this.rootObjects;

            if (removeRoots != null)
            {
                newObjects = newObjects.Without(from root in removeRoots select root.Identifier);
            }

            if (removeSubs != null)
            {
                newObjects = newObjects.With(from sub in removeSubs
                                             group sub by sub.ParentIdentifier into root
                                             where newObjects.ContainsKey(root.Key)
                                             let existing = newObjects[root.Key]
                                             select Helpers.KeyValuePair(root.Key,
                                                 existing.WithChildren(existing.children.Without(from sub in root select sub.Identifier))));
            }

            return withObjects(newObjects);
        }

        public DatabaseState Without(RootObjectState removeObject)
        {
            return Without(Helpers.EnumerableFrom(removeObject), null);
        }

        public DatabaseState Without(SubObjectState removeObject)
        {
            return Without(null, Helpers.EnumerableFrom(removeObject));
        }

        public DatabaseState Without(IEnumerable<ObjectState> removeObjects)
        {
            var objList = removeObjects.ToList();
            return Without(objList.OfType<RootObjectState>(), objList.OfType<SubObjectState>());
        }

        public DatabaseState WithRename(Identifier fromIdent, Identifier toIdent)
        {
            var oldRoot = GetRoot(fromIdent);
            var newRoot = oldRoot.WithIdentifier(toIdent);

            return Without(oldRoot).With(Helpers.EnumerableFrom(newRoot), from sub in GetChildren(fromIdent) select sub.WithParent(toIdent));
        }
    }
}
