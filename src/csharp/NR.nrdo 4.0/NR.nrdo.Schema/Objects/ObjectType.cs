using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Objects
{
    public abstract class ObjectType
    {
        internal ObjectType() { }

        public abstract string Name { get; }

        internal abstract bool IsRoot { get; }

        public abstract void CheckType(Identifier identifier);

        public Identifier Identifier(string name)
        {
            return new Identifier(this, name);
        }

        public abstract IEnumerable<StepBase> Steps { get; }

        public abstract IEnumerable<ObjectState> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper);

        // Equals, GetHashCode and ToString are all based on the Name (case-insensitive using the database's comparer)
        // If two different ObjectTypes end up having the same Name this is a problem, so we add a double-check that if the
        // names are the same then the types are the same too.
        // FIXME this doesn't work well when DBEquivalentComparer moves into DbDriver; consider storing the db driver itself at this level, or the comparer
        public override bool Equals(object obj)
        {
            var other = obj as ObjectType;
            var equal = (object)other != null && Nstring.DBEquivalentComparer.Equals(Name, other.Name);
            if (equal && obj.GetType() != this.GetType()) throw new NotSupportedException("Two different object types " + obj.GetType().FullName + " and " + this.GetType().FullName + " have the same name " + Name);
            return equal;
        }

        public override int GetHashCode()
        {
            return Nstring.DBEquivalentComparer.GetHashCode(Name);
        }

        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==(ObjectType a, ObjectType b)
        {
            return object.Equals(a, b);
        }

        public static bool operator !=(ObjectType a, ObjectType b)
        {
            return !(a == b);
        }
    }

    public abstract class ObjectType<TType, TState> : ObjectType
        where TType : ObjectType<TType, TState>, new()
    {
        internal ObjectType() { }

        private static TType instance = new TType();
        public static TType Instance { get { return instance; } }

        public override void CheckType(Identifier identifier)
        {
            if (!(identifier.ObjectType is TType)) throw new ArgumentException(identifier + " is not a " + Name);
        }

        public sealed override IEnumerable<ObjectState> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return getExistingObjectsInternal(connection, helper);
        }

        protected abstract IEnumerable<ObjectState> getExistingObjectsInternal(SchemaConnection connection, ObjectTypeHelper helper);

        public new static Identifier Identifier(string name)
        {
            return new Identifier(Instance, name);
        }
    }
}
