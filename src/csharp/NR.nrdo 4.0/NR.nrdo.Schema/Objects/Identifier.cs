using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Objects
{
    public sealed class Identifier
    {
        private readonly ObjectType objectType;
        public ObjectType ObjectType { get { return objectType; } }

        private readonly string name;
        public string Name { get { return name; } }

        public Identifier(ObjectType objectType, string name)
        {
            if (objectType == null) throw new ArgumentNullException("objectType");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            this.objectType = objectType;
            this.name = name;
        }

        public Identifier WithName(string newName)
        {
            if (Nstring.DBEquivalentComparer.Equals(name, newName)) return this;
            return new Identifier(objectType, newName);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Identifier;
            return (object)other != null &&
                object.Equals(other.objectType, objectType) &&
                Nstring.DBEquivalentComparer.Equals(other.name, name);
        }

        public override int GetHashCode()
        {
            return Nstring.DBEquivalentComparer.GetHashCode(ToString());
        }

        public override string ToString()
        {
            return objectType + " " + name;
        }

        public static bool operator ==(Identifier a, Identifier b) 
        {
            return object.Equals(a, b);
        }

        public static bool operator !=(Identifier a, Identifier b)
        {
            return !(a == b);
        }
    }
}
