using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;

namespace NR.nrdo.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false)]
    public class NrdoNsBaseAttribute : Attribute
    {
        public NrdoNsBaseAttribute(string nsBase)
        {
            this.nsBase = nsBase;
        }
        private readonly string nsBase;
        public string NsBase { get { return nsBase; } }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class NrdoTablesAttribute : Attribute
    {
        public NrdoTablesAttribute(Type type)
        {
            this.type = type;
        }
        private readonly Type type;
        public Type Type { get { return type; } }

        public string[] OldNames { get; set; }

        public string Name { get; set; }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class NrdoQueriesAttribute : Attribute
    {
        public NrdoQueriesAttribute(Type type)
        {
            this.type = type;
        }
        private readonly Type type;
        public Type Type { get { return type; } }
    }

    public abstract class NrdoObjectTypeAttribute : Attribute
    {
        internal NrdoObjectTypeAttribute(string name)
        {
            this.name = name;
        }
        private readonly string name;
        public string Name { get { return name; } }

        public string CacheFileName { get; set; }
        public string CacheFileContents { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NrdoTableAttribute : NrdoObjectTypeAttribute
    {
        public NrdoTableAttribute(string name) : base(name) { }

        public bool PkeySequenced { get; set; }
        public string ExistingName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NrdoQueryAttribute : NrdoObjectTypeAttribute
    {
        public NrdoQueryAttribute(string name, string sql)
            : base(name)
        {
            this.sql = sql;
        }
        private readonly string sql;
        public string Sql { get { return sql; } }

        public bool StoredProc { get; set; }
        public bool StoredFunction { get; set; }
        public bool PreUpgradeHook { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NrdoFieldAttribute : Attribute
    {
        public NrdoFieldAttribute(int index, string name)
        {
            this.index = index;
            this.name = name;
        }

        private readonly int index;
        public int Index { get { return index; } }

        private readonly string name;
        public string Name { get { return name; } }

        public bool Nullable { get; set; }

        // This attribute is also used on result properties of queries as well as fields of tables.
        // In the case of queries, there is no dbType.
        public string DbType { get; set; }

        // Can't set a nullable int as a property on an attribute, unfortunately, so we have to pretend
        // this value is an int.
        internal int? LengthIfAny { get; set; }
        public int Length { get { return (int)LengthIfAny; } set { LengthIfAny = value; } }
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public class NrdoCtorAttribute : Attribute
    {
        public NrdoCtorAttribute(params string[] parameters)
        {
            this.parameters = parameters;
        }
        private readonly string[] parameters;
        public string[] Parameters { get { return parameters; } }
    }

    public class NrdoGetBaseAttribute : Attribute
    {
        public NrdoGetBaseAttribute(int index, string name)
        {
            this.index = index;
            this.name = name;
        }

        private readonly int index;
        public int Index { get { return index; } }

        private readonly string name;
        public string Name { get { return name; } }

        public bool Multi { get; set; }
        public bool HasCode { get; set; }
        public string Where { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NrdoGetAttribute : NrdoGetBaseAttribute
    {
        public NrdoGetAttribute(int index, string name) : base(index, name) { }

        public bool Pkey { get; set; }
        public bool HasIndex { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NrdoRefAttribute : NrdoGetBaseAttribute
    {
        // The reference back would be:
        // [NrdoRef(1, "songs", typeof(Song), Multi=true, Fkey=false, Cascade=false, Get="GetByGenreId", GetParams=new Type[] {typeof(int?)})]
        // [NrdoByTable(1, typeof(SongGenre), "song_genre")]
        // [NrdoJoin(1, ToTable="song_genre", FromField="id", ToField="genre_id")]
        // [NrdoJoin(2, FromTable="song_genre", FromField="song_id", ToField="id")]
        public NrdoRefAttribute(int index, string name, Type targetType)
            : base(index, name)
        {
            this.targetType = targetType;
        }

        private readonly Type targetType;
        public Type TargetType { get { return targetType; } }

        public bool Fkey { get; set; }
        public string FkeyName { get; set; }
        public bool Cascade { get; set; }
        public string Get { get; set; }
        public Type[] GetParams { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NrdoByFieldAttribute : Attribute
    {
        public NrdoByFieldAttribute(int index, string fieldName)
        {
            this.index = index;
            this.fieldName = fieldName;
        }

        private readonly int index;
        public int Index { get { return index; } }

        private readonly string fieldName;
        public string FieldName { get { return fieldName; } }

        public string Table { get; set; }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NrdoParamAttribute : Attribute
    {
        public NrdoParamAttribute(int index, string name, Type type)
        {
            this.index = index;
            this.name = name;
            this.type = type;
        }

        private readonly int index;
        public int Index { get { return index; } }

        private readonly string name;
        public string Name { get { return name; } }

        private readonly Type type;
        public Type Type { get { return type; } }

        // This attribute is also used on parameters of queries as well as of table gets.
        // In the case of table gets, there is no dbType.
        public string DbType { get; set; }

        public bool Nullable { get; set; }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NrdoByTableAttribute : Attribute
    {
        public NrdoByTableAttribute(int index, Type table, string alias)
        {
            this.index = index;
            this.table = table;
            this.alias = alias;
        }

        private readonly int index;
        public int Index { get { return index; } }

        private readonly Type table;
        public Type Table { get { return table; } }

        private readonly string alias;
        public string Alias { get { return alias; } }

        public bool Param { get; set; }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NrdoJoinAttribute : Attribute
    {
        public NrdoJoinAttribute(int index)
        {
            this.index = index;
        }

        private readonly int index;
        public int Index { get { return index; } }

        public string FromTable { get; set; }
        public string ToTable { get; set; }
        public string FromField { get; set; }
        public string ToField { get; set; }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NrdoOrderByAttribute : Attribute
    {
        public NrdoOrderByAttribute(int index)
        {
            this.index = index;
        }

        private readonly int index;
        public int Index { get { return index; } }

        public bool Descending { get; set; }
        public string Table { get; set; }
        public string Field { get; set; }
        public string Sql { get; set; }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class NrdoBeforeStatementAttribute : Attribute
    {
        public NrdoBeforeStatementAttribute(int index, string step, string name, string statement)
        {
            this.index = index;
            this.step = step;
            this.name = name;
            this.statement = statement;
        }
        private readonly int index;
        public int Index { get { return index; } }

        private readonly string step;
        public string Step { get { return step; } }

        private readonly string name;
        public string Name { get { return name; } }

        private readonly string statement;
        public string Statement { get { return statement; } }

        public bool Initial { get; set; }
        public bool Upgrade { get; set; }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class NrdoRenamedFromAttribute : Attribute
    {
        public NrdoRenamedFromAttribute(int index, string name)
        {
            this.index = index;
            this.name = name;
        }
        private readonly int index;
        public int Index { get { return index; } }

        private readonly string name;
        public string Name { get { return name; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class NrdoIndexAttribute : Attribute
    {
        public NrdoIndexAttribute(int index, string name, string fieldsSemicolonSeparated)
        {
            this.index = index;
            this.name = name;
            this.fieldsSemicolonSeparated = fieldsSemicolonSeparated;
        }
        private readonly int index;
        public int Index { get { return index; } }

        private readonly string name;
        public string Name { get { return name; } }

        private readonly string fieldsSemicolonSeparated;
        public string FieldsSemicolonSeparated { get { return fieldsSemicolonSeparated; } }

        public bool IsPrimary { get; set; }
        public bool IsUnique { get; set; }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NrdoFulltextIndexAttribute : Attribute
    {
        public NrdoFulltextIndexAttribute(params string[] fields)
        {
            this.fields = fields.ToList().AsReadOnly();
        }

        private readonly ReadOnlyCollection<string> fields;
        public ReadOnlyCollection<string> Fields { get { return fields; } }

        public string CatalogName { get; set; }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class NrdoAssemblyLookupAttribute : Attribute
    {
        public NrdoAssemblyLookupAttribute(Type type)
        {
            this.type = type;
        }
        private readonly Type type;
        public Type Type { get { return type; } }
    }
}
