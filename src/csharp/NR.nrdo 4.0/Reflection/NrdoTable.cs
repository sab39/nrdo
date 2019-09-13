using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;
using System.Linq;

namespace NR.nrdo.Reflection
{
    public sealed class NrdoTable : NrdoObjectType
    {
        #region Internal static helper methods used in getting NrdoTable objects - including constructor

        internal NrdoTable(Type type)
            : base(type, type.GetAttribute<NrdoTableAttribute>())
        {
            var tableAttr = type.GetAttribute<NrdoTableAttribute>();
            this.isPkeySequenced = tableAttr.PkeySequenced;
            this.existingName = tableAttr.ExistingName;
        }
        #endregion

        #region Public static methods for getting NrdoTable objects
        /// <summary>
        /// Gets a NrdoTable object corresponding to the given table name. To determine which assembly to load from, the default
        /// lookup strategy of the calling assembly is used.
        /// </summary>
        /// <param name="name">The name of the table to get</param>
        /// <returns>A NrdoTable object corresponding to the named table</returns>
        public static NrdoTable GetTable(string name)
        {
            return NrdoReflection.GetCodeBase(Assembly.GetCallingAssembly()).GetTable(name);
        }

        /// <summary>
        /// Gets a NrdoTable object corresponding to the given table name. To determine which assembly to load from, the default
        /// lookup strategy of the given assembly is used.
        /// </summary>
        /// <param name="assembly">The assembly to use the default lookup strategy of. Note that the assembly's strategy might in theory NOT actually say to look in itself first, or at all.</param>
        /// <param name="name">The name of the table to get</param>
        /// <returns>A NrdoTable object corresponding to the named table</returns>
        public static NrdoTable GetTable(Assembly assembly, string name)
        {
            return NrdoReflection.GetCodeBase(assembly).GetTable(name);
        }

        /// <summary>
        /// Gets a NrdoTable object corresponding to the given table name. To determine which assembly to load from, the given
        /// lookup strategy is used.
        /// </summary>
        /// <param name="lookup">The lookup strategy to use to find the appropriate assembly</param>
        /// <param name="name">The name of the table to get</param>
        /// <returns>A NrdoTable object corresponding to the named table</returns>
        public static NrdoTable GetTable(ILookupAssemblies lookup, string name)
        {
            return NrdoReflection.GetCodeBase(lookup).GetTable(name);
        }

        /// <summary>
        /// Gets a NrdoTable object corresponding to the given type.
        /// </summary>
        /// <param name="type">The type corresponding to the table to get</param>
        /// <returns>A NrdoTable object corresponding to the given type</returns>
        public static NrdoTable GetTable(Type type)
        {
            Assembly assembly = type.Assembly;
            string name = NrdoCodeBase.getTableNameFromType(type);
            if (name == null || NrdoReflection.MangleName(assembly, name) != type.FullName) throw new ArgumentException(type.FullName + " is not a valid nrdo database table class: " + name + " mangles to " + NrdoReflection.MangleName(assembly, name));
            NrdoTable result = NrdoCodeBase.getTableInternal(assembly, name, type);
            if (result == null) throw new ArgumentException(type.FullName + " is not a valid nrdo database table class");
            return result;
        }

        /// <summary>
        /// Gets a NrdoTable object corresponding to the given type.
        /// </summary>
        /// <typeparam name="TTable">The type corresponding to the table to get</typeparam>
        /// <returns>A NrdoTable object corresponding to the given type</returns>
        public static NrdoTable GetTable<TTable>() where TTable : DBTableObject<TTable>
        {
            return GetTable(typeof(TTable));
        }

        /// <summary>
        /// Gets all known tables. To determine which assemblies to load from, the default lookup strategy of the calling
        /// assembly is used.
        /// </summary>
        /// <returns>NrdoTable objects corresponding to all known tables</returns>
        public static IEnumerable<NrdoTable> GetAllTables()
        {
            return NrdoReflection.GetCodeBase(Assembly.GetCallingAssembly()).AllTables;
        }

        /// <summary>
        /// Gets all known tables. To determine which assemblies to load from, the default lookup strategy of the given
        /// assembly is used.
        /// </summary>
        /// <param name="assembly">The assembly to use the default lookup strategy of. Note that the assembly's strategy might in theory NOT actually say to look in itself first, or at all.</param>
        /// <returns>NrdoTable objects corresponding to all known tables</returns>
        public static IEnumerable<NrdoTable> GetAllTables(Assembly assembly)
        {
            return NrdoReflection.GetCodeBase(assembly).AllTables;
        }
        /// <summary>
        /// Gets all known tables. To determine which assemblies to load from, the given lookup strategy is used.
        /// </summary>
        /// <param name="lookup">The lookup strategy to use to find the appropriate assembly</param>
        /// <returns>NrdoTable objects corresponding to all known tables</returns>
        public static IEnumerable<NrdoTable> GetAllTables(ILookupAssemblies lookup)
        {
            return NrdoReflection.GetCodeBase(lookup).AllTables;
        }

        public static IDictionary<string, string> GetRenameMapping(ILookupAssemblies lookup)
        {
            return NrdoReflection.GetCodeBase(lookup).GetTableRenameMapping();
        }
        #endregion

        #region General Information

        private string existingName;
        public string ExistingName { get { return existingName; } }

        public override string DatabaseName { get { return ExistingName ?? base.DatabaseName; } }

        private List<string> renamedFrom;
        public IList<string> RenamedFrom
        {
            get
            {
                if (renamedFrom == null)
                {
                    renamedFrom = new List<string>();
                    var attrs = Type.GetAttributes<NrdoRenamedFromAttribute>().ToList();
                    attrs.Sort((a, b) => a.Index.CompareTo(b.Index));
                    foreach (var attr in attrs)
                    {
                        renamedFrom.Add(attr.Name);
                    }
                }
                return renamedFrom;
            }
        }

        private void resolveFulltext()
        {
            if (fulltextFields == null)
            {
                var attr = Type.GetAttribute<NrdoFulltextIndexAttribute>();
                if (attr != null)
                {
                    fulltextCatalog = attr.CatalogName;
                    fulltextFields = attr.Fields;
                }
                else
                {
                    fulltextFields = new List<string>().AsReadOnly();
                }
            }
        }

        private string fulltextCatalog;
        public string FulltextCatalog { get { resolveFulltext(); return fulltextCatalog; } }

        private ReadOnlyCollection<string> fulltextFields;
        public ReadOnlyCollection<string> FulltextFields { get { resolveFulltext(); return fulltextFields; } }

        private List<NrdoIndex> indexes;
        public IList<NrdoIndex> Indexes
        {
            get
            {
                if (indexes == null)
                {
                    indexes = (from attr in Type.GetAttributes<NrdoIndexAttribute>() orderby attr.Index select new NrdoIndex(attr)).ToList();
                }
                return indexes;
            }
        }

        #endregion

        #region Fields
        private List<NrdoField> fields;
        public IList<NrdoField> Fields
        {
            get
            {
                resolveFields();
                return new ReadOnlyCollection<NrdoField>(fields);
            }
        }

        public NrdoField GetField(string name)
        {
            // FIXME: This can be made more efficient by storing a dictionary
            foreach (NrdoField field in Fields)
            {
                if (field.Name == name) return field;
            }
            return null;
        }

        private void resolveFields()
        {
            if (fields == null)
            {
                fields = new List<NrdoField>();
                foreach (var prop in Type.GetProperties())
                {
                    var attr = prop.GetAttribute<NrdoFieldAttribute>();
                    if (attr != null)
                    {
                        fields.Add(new NrdoField(this, attr, prop));
                    }
                }
                fields.Sort();
            }
        }
        #endregion

        #region Constructor Information including Create method
        private List<NrdoField> ctorParams;
        public IList<NrdoField> CtorParams
        {
            get
            {
                resolveCtor();
                return new ReadOnlyCollection<NrdoField>(ctorParams);
            }
        }

        private ConstructorInfo ctor;
        public ConstructorInfo Ctor
        {
            get
            {
                resolveCtor();
                return ctor;
            }
        }
        private void resolveCtor()
        {
            NrdoCtorAttribute attr = null;
            foreach (var constructor in Type.GetConstructors())
            {
                attr = constructor.GetAttribute<NrdoCtorAttribute>();
                if (attr != null)
                {
                    ctor = constructor;
                    break;
                }
            }
            if (attr == null) throw new ArgumentException("Cannot find nrdo constructor in " + Type.FullName);
            ctorParams = new List<NrdoField>();
            foreach (string fieldName in attr.Parameters)
            {
                ctorParams.Add(GetField(fieldName));
            }
        }

        // arg types must match types of CtorParams
        public ITableObject Create(params object[] args)
        {
            return (ITableObject)Ctor.Invoke(args);
        }
        #endregion

        #region Gets including primary key, GetAll and SelfTableRef
        public IList<ITableObject> GetAll()
        {
            MethodInfo meth = Type.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var objects = (System.Collections.IList)meth.Invoke(null, null);
            return objects.Cast<ITableObject>().ToList().AsReadOnly();
        }

        private NrdoTableRef selfTableRef;
        public NrdoTableRef SelfTableRef
        {
            get
            {
                lock (this)
                {
                    if (selfTableRef == null) selfTableRef = new NrdoTableRef(this);
                    return selfTableRef;
                }
            }
        }

        private List<NrdoGet> gets;
        public IList<NrdoGet> Gets
        {
            get
            {
                resolveGets();
                return gets;
            }
        }

        private NrdoSingleGet pkeyGet;
        public NrdoSingleGet PkeyGet { get { resolveGets(); return pkeyGet; } }

        private bool isPkeySequenced;
        public bool IsPkeySequenced { get { return isPkeySequenced; } }

        private void resolveGets()
        {
            if (gets == null)
            {
                gets = new List<NrdoGet>();
                foreach (MethodInfo method in Type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var gattr = method.GetAttribute<NrdoGetAttribute>();
                    if (gattr != null)
                    {
                        NrdoGet get;
                        if (gattr.Multi)
                        {
                            get = new NrdoMultiGet(this, method, gattr);
                        }
                        else
                        {
                            NrdoSingleGet sget = new NrdoSingleGet(this, method, gattr);
                            get = sget;
                            if (gattr.Pkey) pkeyGet = sget;
                        }
                        gets.Add(get);
                    }
                }
                gets.Sort();
            }
        }
        #endregion

        #region References
        private List<NrdoReference> references;
        public IList<NrdoReference> References
        {
            get
            {
                resolveRefs();
                return references;
            }
        }

        private void resolveRefs()
        {
            if (references == null)
            {
                references = new List<NrdoReference>();
                foreach (MethodInfo method in Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var rattr = method.GetAttribute<NrdoRefAttribute>();
                    if (rattr != null)
                    {
                        NrdoReference reference;
                        if (rattr.Multi)
                        {
                            reference = new NrdoMultiReference(this, method, rattr);
                        }
                        else
                        {
                            reference = new NrdoSingleReference(this, method, rattr);
                        }
                        references.Add(reference);
                    }
                }
                references.Sort();
            }
        }
        #endregion

        #region ToDfnSyntax method

        public string ToDfnSyntax()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("table " + Name + " {\r\n");
            if (ExistingName != null) sb.Append("  existing as " + ExistingName + ";\r\n");
            foreach (string renamed in RenamedFrom)
            {
                sb.Append("  renamed from " + renamed + ";\r\n");
            }
            sb.Append(Fields.DfnSyntaxBlock("  fields", true));
            sb.Append(Gets.DfnSyntaxList(true));
            sb.Append(References.DfnSyntaxList(true));
            sb.Append(BeforeStatements.DfnSyntaxList(true));
            sb.Append("};");
            return sb.ToString();
        }
        #endregion

    }
}
