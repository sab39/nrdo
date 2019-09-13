using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Objects
{
    public abstract class RootObjectType<TType, TState> : ObjectType<TType, TState>
        where TType : RootObjectType<TType, TState>, new()
    {
        internal sealed override bool IsRoot { get { return true; } }

        public new static void CheckType(Identifier identifier)
        {
            ((ObjectType)Instance).CheckType(identifier);
        }

        protected sealed override IEnumerable<ObjectState> getExistingObjectsInternal(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return GetExistingObjects(connection, helper);
        }

        public new abstract IEnumerable<RootObjectState<TType, TState>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper);

        protected IEnumerable<RootObjectState<TType, TState>> GetBasedOnKnownIdentifiers(SchemaConnection connection, ObjectTypeHelper helper,
            Func<string, RootObjectState<TType, TState>> getEmptyState)
        {
            if (!helper.IsStateStorageInitialized) return Enumerable.Empty<RootObjectState<TType, TState>>();

            return connection.ExecuteSql("select name from nrdo_object where type = " + connection.SchemaDriver.QuoteParam("type"),
                result => getEmptyState(result.GetString("name")),
                cmd => cmd.SetString("type", "nvarchar", Name));
        }

        public static IEnumerable<RootObjectState<TType, TState>> AllFrom(DatabaseState database)
        {
            return database.GetRoots(Instance);
        }

        public static RootObjectState<TType, TState> GetFrom(DatabaseState database, Identifier identifier)
        {
            CheckType(identifier);
            return (RootObjectState<TType, TState>)database.GetRoot(identifier);
        }

        protected static RootObjectState<TType, TState> CreateState(string name, TState state)
        {
            return new RootObjectState<TType, TState>(Identifier(name), state);
        }
    }
}
