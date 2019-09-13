using System;
using System.Collections.Generic;
using System.Text;
using NR.nrdo.Reflection;
using System.Xml;
using System.ComponentModel;
using NR.nrdo.Util;

namespace NR.nrdo.Loader
{
    public class RecipeRecord
    {
        public RecipeRecord(RecipeContext context, string tableName, string originalTableName, string nrdoId)
        {
            this.context = context;
            this.originalTableName = originalTableName;
            this.tableName = tableName;
            this.nrdoId = nrdoId ?? ("." + Guid.NewGuid());
        }
        public RecipeRecord(RecipeContext context, NrdoTable table, string nrdoId)
            : this(context, table.Name, table.Name, nrdoId)
        {
            this.table = table;
        }

        public RecipeRecord CopyTo(RecipeContext context)
        {
            RecipeRecord record = CloneTo(context);
            context.PutRecordRaw(record);
            return record;
        }
        public RecipeRecord Clone()
        {
            return CloneTo(Context);
        }
        private RecipeRecord CloneTo(RecipeContext context)
        {
            RecipeRecord record = new RecipeRecord(context, TableName, OriginalTableName, NrdoId);
            foreach (RecipeField field in fields.Values)
            {
                field.CopyTo(record);
            }
            return record;
        }
        internal XmlElement ToXmlElement(XmlDocument doc)
        {
            XmlElement element = doc.CreateElement(TableName.Replace(':', '.'));
            if (NrdoId != null) element.SetAttribute("nrdo.id", NrdoId);
            foreach (RecipeField field in fields.Values)
            {
                RecipeValueField valueField = field as RecipeValueField;
                string value;
                string type = null;
                if (valueField != null)
                {
                    if (valueField.Value == null)
                    {
                        value = "null";
                    }
                    else
                    {
                        type = valueField.Value.GetType().FullName;
                        if (valueField.Value is DateTime)
                        {
                            value = ((DateTime) valueField.Value).ToBinary().ToString();
                        }
                        else
                        {
                            value = valueField.Value.ToString();
                        }
                    }
                }
                else
                {
                    value = "undefined";
                }
                element.SetAttribute(field.Name, type + ":" + value);
            }
            return element;
        }
        internal static RecipeRecord FromXmlElement(RecipeContext context, XmlElement element)
        {
            var origTableName = element.LocalName.Replace('.', ':');
            var tableName = context.GetRenameMappedTableName(origTableName);
            if (tableName == null) return null;

            RecipeRecord result = new RecipeRecord(context, tableName, origTableName, XmlUtil.GetAttr(element, "nrdo.id"));
            foreach (XmlAttribute attr in element.Attributes)
            {
                if (attr.LocalName == "nrdo.id") continue;

                RecipeField field;
                string[] parts = attr.Value.Split(new char[] { ':' }, 2);
                if (parts.Length != 2) throw new ArgumentException("attribute value " + attr.Name + "='" + attr.Value + "' does not contain a ':'");
                string type = parts[0];
                string value = parts[1];

                if (type == "")
                {
                    switch (value)
                    {
                        case "undefined":
                            field = new RecipeField(result, attr.LocalName);
                            break;
                        case "null":
                            field = new RecipeValueField(result, attr.LocalName, null);
                            break;
                        default:
                            throw new ArgumentException("attribute value " + attr.Name + "='" + attr.Value + "' starts with : but is neither :null nor :undefined");
                    }
                }
                else
                {
                    if (!type.StartsWith("System.")) throw new ArgumentException("Illegal context value type " + type);
                    object valueObject;
                    if (type == "System.DateTime")
                    {
                        valueObject = DateTime.FromBinary(long.Parse(value));
                    }
                    else
                    {
                        Type valueType = Type.GetType(type);
                        valueObject = TypeDescriptor.GetConverter(valueType).ConvertFromString(value);
                    }
                    field = new RecipeValueField(result, attr.LocalName, valueObject);
                }
                result.PutField(field);
            }
            return result;
        }
        


        private readonly string tableName;
        public string TableName { get { return tableName; } }

        private readonly string originalTableName;
        public string OriginalTableName { get { return originalTableName; } }

        private string nrdoId;
        public string NrdoId { get { return nrdoId.StartsWith(".") ? null : nrdoId; } }

        internal string InternalId { get { return nrdoId; } }

        private readonly RecipeContext context;
        public RecipeContext Context { get { return context; } }

        private NrdoTable table;
        public NrdoTable Table
        {
            get
            {
                if (table == null) table = NrdoTable.GetTable(context.Lookup, TableName);
                return table;
            }
        }

        private Dictionary<string, RecipeField> fields = new Dictionary<string, RecipeField>();

        public RecipeField GetField(string name)
        {
            return fields.ContainsKey(name) ? fields[name] : null;
        }
        public void PutField(RecipeField field)
        {
            if (field.Record != this) throw new ArgumentException("Cannot add a field from a different record to this record");
            fields[field.Name] = field;
        }

        private ITableObject data;
        internal ITableObject GetData()
        {
            if (data == null)
            {
                data = Recipe.invokeGet(Table.PkeyGet, delegate(NrdoField field) { return ((RecipeValueField)fields[field.Name]).Value; });
            }
            return data;
        }

        internal void SetNrdoId(string to)
        {
            if (nrdoId.StartsWith("."))
            {
                throw new ApplicationException("Cannot rename this record because it doesn't have an id");
            }
            nrdoId = to;
        }
    }
}
