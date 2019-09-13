using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Objects
{
    public abstract class SubObjectType<TType, TState> : ObjectType<TType, TState>
        where TType : SubObjectType<TType, TState>, new()
    {
        internal sealed override bool IsRoot { get { return false; } }

        internal virtual void parentTypeCheck(ObjectType parentType)
        {
            if (!(parentType.IsRoot)) throw new ArgumentException(parentType + " is not a valid parent type for " + Name);
        }

        public static void CheckParentType(Identifier parent)
        {
            Instance.parentTypeCheck(parent.ObjectType);
        }

        public new static void CheckType(Identifier identifier)
        {
            ((ObjectType)Instance).CheckType(identifier);
        }

        protected sealed override IEnumerable<ObjectState> getExistingObjectsInternal(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return GetExistingObjects(connection, helper);
        }

        public new abstract IEnumerable<SubObjectState<TType, TState>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper);

        protected IEnumerable<SubObjectState<TType, TState>> GetBasedOnKnownIdentifiers(SchemaConnection connection, ObjectTypeHelper helper,
            Func<Identifier, string, SubObjectState<TType, TState>> getEmptyState)
        {
            if (!helper.IsStateStorageInitialized) return Enumerable.Empty<SubObjectState<TType, TState>>();

            return connection.ExecuteSql("select parent_type, parent_name, name from nrdo_object_sub where type = " + connection.SchemaDriver.QuoteParam("type"),
                result => getEmptyState(helper.GetObjectType(result.GetString("parent_type")).Identifier(result.GetString("parent_name")), result.GetString("name")),
                cmd => cmd.SetString("type", "nvarchar", Name));
        }

        public static IEnumerable<SubObjectState<TType, TState>> AllFrom(DatabaseState database)
        {
            return database.GetAllChildren(Instance);
        }

        public static IEnumerable<SubObjectState<TType, TState>> ChildrenFrom(DatabaseState database, Identifier parentIdentifier)
        {
            CheckParentType(parentIdentifier);
            return database.GetChildren(parentIdentifier, Instance);
        }

        public static SubObjectState<TType, TState> GetFrom(DatabaseState database, Identifier parentIdentifier, Identifier identifier)
        {
            CheckParentType(parentIdentifier);
            CheckType(identifier);
            return (SubObjectState<TType, TState>)database.GetChild(parentIdentifier, identifier);
        }

        protected static SubObjectState<TType, TState> CreateState(Identifier parent, string name, TState state)
        {
            CheckParentType(parent);
            return new SubObjectState<TType, TState>(parent, Identifier(name), state);
        }
    }

    public abstract class SubObjectType<TType, TState, TParent, TParentState> : SubObjectType<TType, TState>
        where TType : SubObjectType<TType, TState, TParent, TParentState>, new()
        where TParent : RootObjectType<TParent, TParentState>, new()
    {
        public static TParent ParentType { get { return ObjectType<TParent, TParentState>.Instance; } }

        internal sealed override void parentTypeCheck(ObjectType parentType)
        {
            if (!(parentType is TParent)) throw new ArgumentException(parentType + " is not a valid parent type for " + Name + " (should be " + ParentType + ")");
        }
    }
}
