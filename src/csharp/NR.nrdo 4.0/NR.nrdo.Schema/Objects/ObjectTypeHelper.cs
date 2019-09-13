using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Connection;
using System.Collections.Immutable;

namespace NR.nrdo.Schema.Objects
{
    public sealed class ObjectTypeHelper
    {
        private readonly ImmutableDictionary<string, ObjectType> objectTypeDict;

        public ObjectType GetObjectType(string typeName)
        {
            ObjectType type;
            return objectTypeDict.TryGetValue(typeName, out type) ? type : null;
        }

        private readonly bool isStateStorageInitialized;
        public bool IsStateStorageInitialized { get { return isStateStorageInitialized; } }

        internal ObjectTypeHelper(IEnumerable<ObjectType> objectTypes, bool isStateStorageInitialized, DbDriver dbDriver)
        {
            this.objectTypeDict = objectTypes.ToImmutableDictionary(t => t.Name, dbDriver.DbStringComparer);
            this.isStateStorageInitialized = isStateStorageInitialized;
        }
    }
}
