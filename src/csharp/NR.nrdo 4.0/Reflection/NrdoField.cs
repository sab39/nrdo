using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public sealed class NrdoField : IComparable<NrdoField>, IDfnElement
    {
        internal NrdoField(NrdoObjectType container, NrdoFieldAttribute attr, PropertyInfo prop)
        {
            this.index = attr.Index;
            this.container = container;
            this.name = attr.Name;
            this.property = prop;
            this.type = prop.PropertyType;
            if (type.IsValueType)
            {
                if (attr.Nullable)
                {
                    if (!typeof(Nullable<>).Equals(type.GetGenericTypeDefinition())) throw new ArgumentException("Field " + name + " is nullable but type is not Nullable<>, it's " + type);
                    nullableType = type;
                    nonNullableType = type.GetGenericArguments()[0];
                }
                else
                {
                    nonNullableType = type;
                    nullableType = typeof(Nullable<>).MakeGenericType(type);
                }
            }
            else
            {
                if (type.IsGenericType) throw new ArgumentException("Field " + name + " is not a value type but is generic type " + type);
                nonNullableType = type;
                nullableType = type;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }
            this.dbType = attr.DbType;
            this.length = attr.LengthIfAny;
            this.isNullable = attr.Nullable;
            this.isWritable = prop.CanWrite;
        }

        private int index;

        private NrdoObjectType container;
        public NrdoObjectType Container { get { return container; } }

        private string name;
        public string Name { get { return name; } }

        private PropertyInfo property;
        public PropertyInfo Property { get { return property; } }

        private Type type;
        public Type Type { get { return type; } }

        private Type nullableType;
        public Type NullableType { get { return nullableType; } }

        private Type nonNullableType;
        public Type NonNullableType { get { return nonNullableType; } }

        private string dbType;
        public string DbType { get { return dbType; } }

        private int? length;
        /// <summary>
        /// For string fields defined as particular lengths in the database (eg
        /// varchar(50)), the defined length.
        /// </summary>
        public int? Length { get { return length; } }

        private bool isNullable;
        public bool IsNullable { get { return isNullable; } }

        private bool isWritable;
        public bool IsWritable { get { return isWritable; } }

        // obj must be of type Table.Type
        public object Get(IDBObject obj)
        {
            if (obj.GetType() != container.Type)
            {
                throw new ArgumentException("Attempt to get field " + this + " on object of type " + obj.GetType().FullName);
            }
            return property.GetValue(obj, null);
        }

        // obj must be of type Table.Type, value of type Type
        public void Set(IDBObject obj, object value)
        {
            if (obj.GetType() != container.Type)
            {
                throw new ArgumentException("Attempt to set field " + this + " on object of type " + obj.GetType().FullName);
            }
            property.SetValue(obj, value, null);
        }

        public void Set(IDBObject obj, string value, bool truncate)
        {
            if (!typeof(string).Equals(Type))
            {
                throw new ArgumentException("Attempt to set non-string field " + this + " with truncation");
            }
            if (truncate && value != null && Length != null && value.Length > Length)
            {
                value = value.Substring(0, (int) Length);
            }
            Set(obj, value);
        }

        int IComparable<NrdoField>.CompareTo(NrdoField other)
        {
            return index.CompareTo(other.index);
        }

        string IDfnElement.ToDfnSyntax()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("    " + NrdoReflection.GetTypeString(NonNullableType) + " " + Name + " ");
            if (DbType != null) sb.Append(DbType + " ");
            sb.Append(IsNullable ? "nullable " : "notnull ");
            if (!(container is NrdoQuery)) sb.Append(IsWritable ? "readwrite " : "readonly ");
            sb.Append("[]");
            return sb.ToString();
        }
    }
}
