using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;
using NR.nrdo.Connection;
using NR.nrdo.Schema.Objects;
using System.Collections.Immutable;

namespace NR.nrdo.Schema.State
{
    public class StateStorage
    {
        private readonly SchemaConnection connection;
        private readonly IdentComparer identComparer;
        private readonly SubComparer subComparer;

        private Lazy<HashSet<StoredIdent>> roots;
        private HashSet<StoredIdent> Roots { get { return roots.Value; } }

        private Lazy<HashSet<StoredSub>> subs;
        private HashSet<StoredSub> Subs { get { return subs.Value; } }

        #region storage classes
        private class StoredIdent
        {
            private readonly string typeName;
            public string TypeName { get { return typeName; } }

            private readonly string name;
            public string Name { get { return name; } }

            internal StoredIdent(string typeName, string name)
            {
                this.typeName = typeName;
                this.name = name;
            }
        }
        private class IdentComparer : IEqualityComparer<StoredIdent>
        {
            private readonly IEqualityComparer<string> stringComparer;
            internal IdentComparer(IEqualityComparer<string> stringComparer)
            {
                this.stringComparer = stringComparer;
            }

            public bool Equals(StoredIdent x, StoredIdent y)
            {
                return stringComparer.Equals(x.TypeName, y.TypeName) &&
                    stringComparer.Equals(x.Name, y.Name);
            }

            public int GetHashCode(StoredIdent obj)
            {
                return stringComparer.GetHashCode(obj.TypeName + " " + obj.Name);
            }
        }

        private class StoredSub
        {
            private readonly StoredIdent parent;
            public StoredIdent Parent { get { return parent; } }

            private readonly StoredIdent child;
            public StoredIdent Child { get { return child; } }

            internal StoredSub(StoredIdent parent, StoredIdent child)
            {
                this.parent = parent;
                this.child = child;
            }
        }
        private class SubComparer : IEqualityComparer<StoredSub>
        {
            private readonly IdentComparer identComparer;
            internal SubComparer(IdentComparer identComparer)
            {
                this.identComparer = identComparer;
            }

            public bool Equals(StoredSub x, StoredSub y)
            {
                return identComparer.Equals(x.Parent, y.Parent) &&
                    identComparer.Equals(x.Child, y.Child);
            }

            public int GetHashCode(StoredSub obj)
            {
                return identComparer.GetHashCode(obj.Parent) + identComparer.GetHashCode(obj.Child);
            }
        }
        #endregion

        private static StoredIdent readIdent(NrdoResult result, string typeField, string nameField)
        {
            return new StoredIdent(result.GetString(typeField), result.GetString(nameField));
        }
        private HashSet<StoredIdent> loadRoots()
        {
            return new HashSet<StoredIdent>(connection.ExecutePortableSql("select :[type], :[name] from :[dbo.nrdo_object]", result => readIdent(result, "type", "name")), identComparer);
        }
        private HashSet<StoredSub> loadSubs()
        {
            return new HashSet<StoredSub>(
                connection.ExecutePortableSql("select :[parent_type], :[parent_name], :[type], :[name] from :[dbo.nrdo_object_sub]",
                    result => new StoredSub(readIdent(result, "parent_type", "parent_name"), readIdent(result, "type", "name"))),
                subComparer);
        }

        public StateStorage(SchemaConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            this.connection = connection;
            this.identComparer = new IdentComparer(connection.DbDriver.DbStringComparer);
            this.subComparer = new SubComparer(identComparer);
            Refresh();
        }

        public void Refresh()
        {
            this.roots = new Lazy<HashSet<StoredIdent>>(loadRoots);
            this.subs = new Lazy<HashSet<StoredSub>>(loadSubs);
        }

        public ImmutableHashSet<Identifier> GetKnownRoots(ObjectTypeHelper helper)
        {
            return (from root in Roots
                    let type = helper.GetObjectType(root.TypeName)
                    where type != null
                    select type.Identifier(root.Name)
                   ).ToImmutableHashSet();
        }

        public ImmutableHashSet<Tuple<Identifier, Identifier>> GetKnownSubs(ObjectTypeHelper helper)
        {
            return (from sub in Subs
                    let parentType = helper.GetObjectType(sub.Parent.TypeName)
                    let childType = helper.GetObjectType(sub.Child.TypeName)
                    where parentType != null && childType != null
                    select Tuple.Create(parentType.Identifier(sub.Parent.Name), childType.Identifier(sub.Child.Name))
                   ).ToImmutableHashSet();
        }

        public void PutAll(IEnumerable<ObjectState> objects)
        {
            foreach (var obj in objects)
            {
                PutState(obj);
            }
        }

        public void DeleteAll(IEnumerable<ObjectState> objects)
        {
            foreach (var obj in objects)
            {
                DeleteState(obj);
            }
        }

        public void PutState(ObjectState obj)
        {
            var sub = obj as SubObjectState;
            if (sub != null)
            {
                PutSub(sub.ParentIdentifier, sub.Identifier);
            }
            else
            {
                PutRoot(((RootObjectState)obj).Identifier);
            }
        }
        public void DeleteState(ObjectState obj)
        {
            var sub = obj as SubObjectState;
            if (sub != null)
            {
                DeleteSub(sub.ParentIdentifier, sub.Identifier);
            }
            else
            {
                DeleteRoot(((RootObjectState)obj).Identifier);
            }
        }

        private StoredIdent getStored(Identifier ident)
        {
            return new StoredIdent(ident.ObjectType.Name, ident.Name);
        }
        private StoredSub getStoredSub(Identifier parent, Identifier child)
        {
            return new StoredSub(getStored(parent), getStored(child));
        }

        private void setRootParams(NrdoCommand cmd, Identifier identifier)
        {
            cmd.SetString("type", "varchar", identifier.ObjectType.Name);
            cmd.SetString("name", "varchar", identifier.Name);
        }
        private void setSubParams(NrdoCommand cmd, Identifier parent, Identifier child)
        {
            cmd.SetString("ptype", "varchar", parent.ObjectType.Name);
            cmd.SetString("pname", "varchar", parent.Name);
            cmd.SetString("type", "varchar", child.ObjectType.Name);
            cmd.SetString("name", "varchar", child.Name);
        }
        public void PutRoot(Identifier identifier)
        {
            var stored = getStored(identifier);
            if (!Roots.Contains(stored))
            {
                connection.ExecutePortableSql("insert into :[dbo.nrdo_object] (:[type], :[name]) values (:type, :name)",
                                      cmd => setRootParams(cmd, identifier));
                Roots.Add(stored);
            }
        }
        public void DeleteRoot(Identifier identifier)
        {
            var stored = getStored(identifier);

            if (Roots.Contains(stored))
            {
                connection.ExecutePortableSql("delete from :[dbo.nrdo_object] where :[type] = :type and :[name] = :name",
                                      cmd => setRootParams(cmd, identifier));
                Subs.RemoveWhere(sub => identComparer.Equals(sub.Parent, stored));
                Roots.Remove(stored);
            }
        }
        public void PutSub(Identifier parent, Identifier child)
        {
            var stored = getStoredSub(parent, child);
            if (!Subs.Contains(stored))
            {
                connection.ExecutePortableSql("insert into :[dbo.nrdo_object_sub] (:[parent_type], :[parent_name], :[type], :[name]) values (:ptype, :pname, :type, :name)",
                                      cmd => setSubParams(cmd, parent, child));
                Subs.Add(stored);
            }
        }
        public void DeleteSub(Identifier parent, Identifier child)
        {
            var stored = getStoredSub(parent, child);
            if (Subs.Contains(stored))
            {
                connection.ExecutePortableSql("delete from :[dbo.nrdo_object_sub] where :[parent_type] = :ptype and :[parent_name] = :pname and :[type] = :type and :[name] = :name",
                                      cmd => setSubParams(cmd, parent, child));
                Subs.Remove(stored);
            }
        }

        public void Rename(Identifier from, Identifier to)
        {
            if (!object.Equals(from.ObjectType, to.ObjectType)) throw new ArgumentException("Cannot rename between different object types " + from.ObjectType + " and " + to.ObjectType);

            var storedFrom = getStored(from);
            var storedTo = getStored(to);

            if (!Roots.Contains(storedFrom))
            {
                throw new ArgumentException("Cannot store rename from " + from + " to " + to + " because " + from + " does not exist in state table");
            }

            if (Roots.Contains(storedTo))
            {
                throw new ArgumentException("Cannot store rename from " + from + " to " + to + " because " + to + " already exists in state table");
            }

            connection.ExecutePortableSql("insert into :[dbo.nrdo_object] (:[type], :[name]) values (:type, :name)",
                                  cmd => setRootParams(cmd, to));
            Roots.Add(storedTo);

            connection.ExecutePortableSql("update :[dbo.nrdo_object_sub] set :[parent_name] = :toname where :[parent_type] = :type and parent_name = :fromname",
                                  cmd =>
                                  {
                                      cmd.SetString("toname", "varchar", to.Name);
                                      cmd.SetString("type", "varchar", from.ObjectType.Name);
                                      cmd.SetString("fromname", "varchar", from.Name);
                                  });
            var newSubs = from sub in Subs where identComparer.Equals(sub.Parent, storedFrom) select new StoredSub(storedTo, sub.Child);
            Subs.UnionWith(newSubs.ToImmutableList());
            Subs.RemoveWhere(sub => identComparer.Equals(sub.Parent, storedFrom));

            connection.ExecutePortableSql("delete from :[dbo.nrdo_object] where :[type] = :type and :[name] = :name",
                                  cmd => setRootParams(cmd, from));
            Roots.Remove(storedFrom);
        }

        public bool ContainsRoot(Identifier root)
        {
            return Roots.Contains(getStored(root));
        }
        public bool ContainsSub(Identifier parent, Identifier child)
        {
            return Subs.Contains(getStoredSub(parent, child));
        }
    }
}
