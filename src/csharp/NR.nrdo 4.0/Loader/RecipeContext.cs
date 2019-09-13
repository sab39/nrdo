using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using NR.nrdo.Reflection;
using System.Xml;
using System.IO;
using System.Diagnostics;
using NR.nrdo.Util;

namespace NR.nrdo.Loader
{
    public class RecipeContext
    {
        public RecipeContext()
            : this(Assembly.GetCallingAssembly()) { }

        public RecipeContext(Assembly assembly)
            : this(NrdoReflection.GetLookupStrategy(assembly)) { }

        public RecipeContext(ILookupAssemblies lookup)
        {
            Lookup = lookup;
            RegisterTransformType("xslt", typeof(XsltTransformRecipe));
        }

        public ILookupAssemblies Lookup { get; set; }

        private List<ITransformRecipe> transforms = new List<ITransformRecipe>();
        public List<ITransformRecipe> Transforms { get { return transforms; } }

        private Dictionary<string, Type> transformTypes = new Dictionary<string, Type>();

        public void RegisterTransformType(string name, Type type)
        {
            if (!typeof(ITransformRecipe).IsAssignableFrom(type))
            {
                throw new ArgumentException("Transform type '" + type.FullName + "' does not implement ITransformRecipe");
            }
            transformTypes[name] = type;
        }
        public void RemoveTransformType(string name)
        {
            transformTypes.Remove(name);
        }

        private Predicate<string> preserveDroppedTableCallBack = name => true;
        public Predicate<string> PreserveDroppedTableCallBack
        {
            get { return preserveDroppedTableCallBack; }
            set { preserveDroppedTableCallBack = value; }
        }

        private IDictionary<string, string> tableRenameMapping;
        internal IDictionary<string, string> TableRenameMapping
        {
            get
            {
                if (tableRenameMapping == null) tableRenameMapping = NrdoTable.GetRenameMapping(Lookup);
                return tableRenameMapping;
            }
        }

        internal string GetRenameMappedTableName(string tableName)
        {
            if (!TableRenameMapping.ContainsKey(tableName))
            {
                var preserve = PreserveDroppedTableCallBack(tableName);
                TableRenameMapping[tableName] = preserve ? tableName : null;
            }

            return TableRenameMapping[tableName];
        }

        public int Count { get { return records.Count; } }

        // Maps from nrdoId to record.
        private Dictionary<string, RecipeRecord> records = new Dictionary<string, RecipeRecord>();

        // Maps from table name to dictionary from key to record.
        // Lazily populated: a table will only be added when needed. See GetRecordByKey
        private Dictionary<string, Dictionary<RecordKey, RecipeRecord>> recordsByKey = new Dictionary<string, Dictionary<RecordKey, RecipeRecord>>();

        public RecipeContext Clone()
        {
            RecipeContext context = new RecipeContext(Lookup);
            foreach (RecipeRecord record in records.Values)
            {
                record.CopyTo(context);
            }
            return context;
        }
        public XmlDocument ToXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("nrdo.context");
            doc.AppendChild(root);
            foreach (RecipeRecord record in records.Values)
            {
                root.AppendChild(record.ToXmlElement(doc));
            }
            return doc;
        }
        public static RecipeContext FromXml(XmlDocument doc)
        {
            return FromXml(Assembly.GetCallingAssembly(), doc);
        }
        public static RecipeContext FromXml(Assembly assembly, XmlDocument doc)
        {
            return FromXml(NrdoReflection.GetLookupStrategy(assembly), doc);
        }
        public static RecipeContext FromXml(ILookupAssemblies lookup, XmlDocument doc)
        {
            XmlElement root = doc.DocumentElement;
            if (root.LocalName != "nrdo.context") throw new ArgumentException("Cannot create a context from a <" + root.LocalName + "> element");
            RecipeContext result = new RecipeContext(lookup);
            foreach (XmlElement element in Recipe.elementChildren(root))
            {
                result.PutRecordRaw(RecipeRecord.FromXmlElement(result, element));
            }
            return result;
        }

        public RecipeRecord GetRecord(string nrdoId)
        {
            if (nrdoId == null) throw new ArgumentNullException("GetRecord called with null nrdoId");
            return GetRecord(nrdoId, null);
        }
        internal RecipeRecord GetRecord(RecordKey key)
        {
            if (key == null) throw new ArgumentNullException("GetRecord called with null key");
            if (!key.IsDefined) throw new ArgumentException("GetRecord called with key that is not fully defined");
            return GetRecord(null, key);
        }
        internal RecipeRecord GetRecord(string nrdoId, RecordKey key)
        {
            RecipeRecord resultById = null;
            if (nrdoId != null)
            {
                if (records.ContainsKey(nrdoId))
                {
                    resultById = verifyRecord(records[nrdoId]);
                }
            }
            RecipeRecord resultByKey = null;
            if (key != null && key.IsDefined)
            {
                Dictionary<RecordKey, RecipeRecord> byKey;
                if (!recordsByKey.ContainsKey(key.Table.Name))
                {
                    // This is where we load all the records from the context based on their primary key.
                    // Normally, this is pretty straightforward. However, since adding support for table renaming, it's possible that more than one record can end
                    // up existing with the same primary key - because at some point in the past we had used a new nrdo.id and a find.by to compensate for the
                    // previous lack of renaming support. So there's a record from the old table name with an old nrdo.id and a record from the current table
                    // name with the new nrdo.id and they both are now understood to represent the same record with the same id. Having two records for the same
                    // table with the same primary key but different nrdo.ids is of course forbidden. The way we disambiguate is to look and see if one of them
                    // has the current table name and the other does not - and if so we remove the one with the outdated table name.
                    // There is a theoretical possibility that there could be a double-rename of a table so two records could *both* have outdated table names.
                    // If that happens, the correct solution would be to determine which table name is more recent, but at the moment we do not attempt to do this.
                    // It'd also be possible to attempt to check whether the record in question actually still exists in the DB - and if not, of course,
                    // we can ignore it in the RecipeContext. Again, we don't currently attempt to do this.
                    List<RecipeRecord> recordsToRemove = new List<RecipeRecord>();
                    byKey = new Dictionary<RecordKey, RecipeRecord>();
                    foreach (RecipeRecord record in records.Values)
                    {
                        if (record.TableName == key.Table.Name)
                        {
                            var thisRecord = record;
                            var recordKey = new RecordKey(thisRecord);
                            if (byKey.ContainsKey(recordKey))
                            {
                                var otherRecord = byKey[recordKey];
                                var thisOneIsCurrent = record.TableName == thisRecord.OriginalTableName;
                                var otherIsCurrent = otherRecord.TableName == otherRecord.OriginalTableName;

                                if (thisOneIsCurrent == otherIsCurrent)
                                {
                                    throw new ArgumentException("The recipe context contains the same record key " + recordKey.ToString() + " for two separate records (nrdo.ids " + thisRecord.NrdoId + " and " + otherRecord.NrdoId + ").\r\n" +
                                        "This can happen due to the fact that recipes now support table renaming and records with old table names that were being ignored can now pop up and conflict with new records.\r\n" +
                                        "However, in this case, we cannot disambiguate based on table name because " + (thisOneIsCurrent ? "both" : "neither") + " of them have the current table name. See the comments in RecipeContext.cs for more details.");
                                }
                                else if (thisOneIsCurrent)
                                {
                                    recordsToRemove.Add(otherRecord);
                                }
                                else
                                {
                                    recordsToRemove.Add(thisRecord);
                                    thisRecord = otherRecord;
                                }
                            }
                            byKey[recordKey] = thisRecord;
                        }
                    }
                    recordsByKey[key.Table.Name] = byKey;
                    foreach (var remove in recordsToRemove)
                    {
                        records.Remove(remove.InternalId);
                    }
                }
                byKey = recordsByKey[key.Table.Name];
                byKey.TryGetValue(key, out resultByKey);
                resultByKey = verifyRecord(resultByKey);
            }
            // If they both got results and they weren't equal, that's an error
            if (resultById != null && resultByKey != null && resultById != resultByKey)
            {
                throw new ArgumentException("Record found by nrdo.id='" + nrdoId + "' does not match record found by primary key");
            }

            // Getting here means that either one or both is null, or they're equal, so there's
            // only one possible record to return as the result:
            RecipeRecord result = resultById ?? resultByKey;

            // If a result found by key has a nrdoId, but it doesn't match the specified
            // nrdoId, that's an error
            if (resultByKey != null && nrdoId != null &&
                resultByKey.NrdoId != null && resultByKey.NrdoId != nrdoId)
            {
                throw new ArgumentException("Record in context found by primary key has nrdo.id='" + resultByKey.NrdoId + "', does not match specified nrdo.id='" + nrdoId + "'");
            }
            // If key was given, and a record was found by nrdoId that doesn't
            // match any of the specified fields of key, that's an error
            if (resultById != null && key != null)
            {
                foreach (NrdoFieldRef field in key.Table.PkeyGet.Fields)
                {
                    object value = key.GetValue(field.Field);
                    if (!(value is Undefined))
                    {
                        object actualValue = field.Field.Get(resultById.GetData());
                        if (!(value is Undefined) && !object.Equals(value, actualValue))
                        {
                            throw new ArgumentException("Record found in context for nrdo.id='" + nrdoId + "' has value of field " + field.Field.Name + " (" + actualValue + ") that does not match what was specified (" + value + ")");
                        }
                    }
                }
            }

            // If it couldn't be found in the context it might still be findable in
            // the database itself...
            if (result == null && key != null && key.IsDefined)
            {
                result = new RecipeRecord(this, key.Table, nrdoId);
                foreach (NrdoFieldRef field in key.Table.PkeyGet.Fields)
                {
                    object value = key.GetValue(field.Field);
                    result.PutField(new RecipeValueField(result, field.Field.Name, value));
                }
                if (result.GetData() == null) result = null;
            }
            
            return result;
        }
        // returns the record if it actually exists in the database, or null (and
        // removes it from the context) if not
        private RecipeRecord verifyRecord(RecipeRecord record)
        {
            if (record != null && record.GetData() == null)
            {
                RemoveRecord(record);
                return null;
            }
            else
            {
                return record;
            }
        }
        public void PutRecord(RecipeRecord record)
        {
            if (record.Context != this) throw new ArgumentException("Cannot add a record from a different context to this context");
            RecordKey key = new RecordKey(record);
            RecipeRecord check = GetRecord(key);
            if (check.NrdoId != null && check.NrdoId != record.NrdoId)
            {
                throw new ArgumentException("Mismatched nrdo.id values, cannot overwrite nrdo.id='" + check.NrdoId + "' with nrdo.id='" + record.NrdoId + "'");
            }

            if (check.NrdoId == null) records.Remove(check.InternalId);
            recordsByKey[record.TableName][key] = record;
            records[record.InternalId] = record;
        }
        internal void PutRecordRaw(RecipeRecord record)
        {
            if (record.Context != this) throw new ArgumentException("Cannot add a record from a different context to this context");
            if (recordsByKey.ContainsKey(record.TableName)) throw new InvalidOperationException("Cannot call PutRecordRaw once the recordsByKey dictionary has been initialized");
            records[record.InternalId] = record;
        }
        internal bool RemoveRecord(RecipeRecord record)
        {
            if (record.Context != this) throw new ArgumentException("Cannot remove a record from a context it's not in");
            if (recordsByKey.ContainsKey(record.TableName))
            {
                recordsByKey[record.TableName].Remove(new RecordKey(record));
            }
            return records.Remove(record.InternalId);
        }

        internal object evaluate(Type type, string expr)
        {
            if (expr.StartsWith(":"))
            {
                if (expr.StartsWith("::"))
                {
                    expr = expr.Substring(1);
                }
                else if (expr == ":null")
                {
                    return null;
                }
                else if (expr == ":now")
                {
                    if (type.Equals(typeof(DateTime)))
                    {
                        return Now.Value;
                    }
                    else
                    {
                        throw new ArgumentException(":now can only be used on fields of type DateTime, not " + type.FullName);
                    }
                }
                else
                {
                    string[] parts = expr.Substring(1).Split('.');
                    if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                    {
                        throw new ArgumentException("Values that start with : must either be :null, start with :: (for a value that really starts with ':') or be of the form :nrdoid.fieldname. '" + expr + "' is none of these");
                    }
                    string nrdoId = parts[0];
                    string fieldName = parts[1];
                    RecipeRecord record = GetRecord(nrdoId);
                    if (record == null) return Undefined.Value;
                    NrdoField field = record.Table.GetField(fieldName);
                    if (field == null) throw new ArgumentException("nrdo.id " + nrdoId + " is a " + record.Table.Name + " which does not contain a field called " + fieldName);
                    object value = field.Get(record.GetData());
                    if (value == null || value.GetType() == type)
                    {
                        return value;
                    }
                    else
                    {
                        try
                        {
                            return TypeDescriptor.GetConverter(type).ConvertFrom(value);
                        }
                        catch (NotSupportedException)
                        {
                            expr = value.ToString();
                        }
                    }
                }
            }
            return TypeDescriptor.GetConverter(type).ConvertFromString(expr);
        }

        private XmlElement CanonicalizeElement(XmlElement element, NrdoTable type, XmlElement beforeElement)
        {
            element.ParentNode.RemoveChild(element);
            XmlElement newElement = element.OwnerDocument.CreateElement(type.Name.Replace(':', '.'));
            foreach (XmlAttribute attr in element.Attributes)
            {
                newElement.SetAttribute(attr.Name, attr.Value);
            }
            beforeElement.ParentNode.InsertBefore(newElement, beforeElement);
            CanonicalizeChildren(element, newElement, type, newElement, beforeElement);
            return newElement;
        }
        private void CanonicalizeChildren(XmlElement origElement, XmlElement newElement, NrdoTable type, XmlElement beforeElement, XmlElement afterElement)
        {
            XmlElement preFind = null;
            bool parentIsDelete = XmlUtil.GetAttr(newElement, "nrdo.action") == "delete";

            foreach (XmlElement childElement in new List<XmlElement>(Recipe.elementChildren(origElement)))
            {
                string name = childElement.LocalName;
                string action = XmlUtil.GetAttr(childElement, "nrdo.action");

                // If it names a field whose type is string, grab either the text or the InnerXml of it (depending on the
                // "escaped" attribute) and put it in an attribute on newElement, erroring if such attribute already exists.
                NrdoField field = type.GetField(name);
                if (field != null && field.Type.Equals(typeof(string)))
                {
                    if (newElement.HasAttribute(field.Name)) throw new ArgumentException("Cannot specify " + field.Name + " as both attribute and nested element on " + type.Name);
                    bool escaped;
                    if (childElement.HasAttribute("escaped"))
                    {
                        switch (XmlUtil.GetAttr(childElement, "escaped"))
                        {
                            case "true":
                                escaped = true;
                                break;
                            case "false":
                                escaped = false;
                                break;
                            default:
                                throw new ArgumentException("escaped attribute must have value 'true' or 'false', not " + XmlUtil.GetAttr(childElement, "escaped"));
                        }
                    }
                    else
                    {
                        escaped = false;
                    }
                    // FIXME: should check for any elements here if (escaped), they're illegal and should be thrown out
                    string value = escaped ? childElement.InnerText : childElement.InnerXml;
                    newElement.SetAttribute(field.Name, value);
                    if (preFind != null) preFind.SetAttribute(field.Name, value);
                    continue;
                }

                // If it names an eligible reference, then
                // - CanonicalizeElement(childElement, targetType, beforeElement) and save the result.
                // - If the result doesn't have a nrdo.id, add one from origElement (or error if it doesn't have one)
                // - Add a reference-named attribute to newElement with the nrdo.id of the new element.
                NrdoSingleReference reference = null;
                foreach (NrdoReference testRef in type.References)
                {
                    if (testRef.Name == name && Recipe.isEligibleReference(testRef) &&
                        testRef.AssociatedGet == testRef.TargetTable.PkeyGet && testRef.TargetTable.IsPkeySequenced)
                    {
                        reference = (NrdoSingleReference) testRef;
                        break;
                    }
                }
                if (reference != null)
                {
                    // If the parent element is being deleted, the child element MUST be deleted too.
                    if (parentIsDelete)
                    {
                        if (action != "none" && action != "delete")
                        {
                            throw new ApplicationException(childElement.LocalName + " element inside deleted " + newElement.LocalName + " element must also have nrdo.action='delete'");
                        }
                    }
                    else if (action == "delete")
                    {
                        throw new ApplicationException(childElement.LocalName + " element cannot have nrdo.action='delete' because containing " + newElement.LocalName + " element doesn't");
                    }
                    if (preFind == null)
                    {
                        preFind = newElement.OwnerDocument.CreateElement(newElement.LocalName);
                        foreach (XmlAttribute attr in newElement.Attributes)
                        {
                            preFind.SetAttribute(attr.Name, attr.Value);
                        }
                        if (!parentIsDelete)
                        {
                            preFind.SetAttribute("nrdo.action", "none");
                        }
                        bool insertPreFind = true;
                        if (!preFind.HasAttribute("nrdo.id"))
                        {
                            foreach (NrdoFieldRef pkeyField in type.PkeyGet.Fields)
                            {
                                if (!preFind.HasAttribute(pkeyField.Field.Name)) insertPreFind = false;
                            }
                        }
                        if (insertPreFind)
                        {
                            beforeElement.ParentNode.InsertBefore(preFind, beforeElement);
                        }
                        else if (parentIsDelete)
                        {
                            throw new ApplicationException("Cannot construct correct order for deleting " + childElement.LocalName + " without a nrdo.id, either add the right nrdo.id or reorder the elements manually");
                        }
                    }

                    if (!childElement.HasAttribute("nrdo.id"))
                    {
                        if (!newElement.HasAttribute("nrdo.id")) throw new ArgumentException(childElement.Name + " element must have a nrdo.id attribute.");
                        childElement.SetAttribute("nrdo.id", XmlUtil.GetAttr(newElement, "nrdo.id") + "_" + reference.Name);
                    }
                    // This needs to be done before canonicalizing, because the canonicalization can actually
                    // result in two records (the child may itself end up turning into a 'preFind') and only
                    // one of these gets returned.
                    if (newElement.HasAttribute("nrdo.id"))
                    {
                        childElement.SetAttribute(reference.Joins[0].To.Field.Name, ":" + XmlUtil.GetAttr(newElement, "nrdo.id") + "." + reference.Joins[0].From.Field.Name);
                    }
                    XmlElement result = CanonicalizeElement(childElement, reference.TargetTable, beforeElement);
                    if (newElement.HasAttribute(reference.Name)) throw new ArgumentException("Cannot specify " + reference.Name + " as both attribute and nested element on " + type.Name);
                    newElement.SetAttribute(reference.Name, XmlUtil.GetAttr(result, "nrdo.id"));
                    continue;
                }

                // Otherwise it should name a table. If it contains a ".", get the table outright. Otherwise start from
                // the module that "type" is in, trying NrdoTable.GetTable on each until something is found. If nothing is, it's
                // an error.
                NrdoTable table = null;
                if (name.IndexOf('.') >= 0)
                {
                    table = NrdoTable.GetTable(Lookup, name.Replace('.', ':'));
                }
                else
                {
                    string module = type.Module;
                    while (module != null && table == null)
                    {
                        table = NrdoTable.GetTable(Lookup, module + ":" + name);
                        int pos = module.LastIndexOf(':');
                        module = pos < 0 ? null : module.Substring(0, pos);
                    }
                    if (table == null)
                    {
                        table = NrdoTable.GetTable(Lookup, name);
                    }
                }
                if (table == null) throw new ArgumentException("No table called " + name + " found from " + type.Module);

                // Find all eligible references from that table to "type". If there's not exactly one, error. Otherwise take that
                // reference.
                List<NrdoSingleReference> refs = new List<NrdoSingleReference>();
                foreach (NrdoReference testRef in table.References)
                {
                    if (Recipe.isEligibleReference(testRef) && testRef.TargetTable.Name == type.Name)
                    {
                        refs.Add((NrdoSingleReference) testRef);
                    }
                }
                if (refs.Count != 1) throw new ArgumentException("There isn't a single eligible reference from " + table.Name + " to " + type.Name);

                if (parentIsDelete && action != "none" && action != "delete")
                {
                    throw new ApplicationException(childElement.LocalName + " element inside deleted " + newElement.LocalName + " element must also have nrdo.action='delete'");
                }
                else if (parentIsDelete && action == "delete")
                {
                    // FIXME: this needs to somehow turn into <outer nrdo.action="none"/><inner nrdo.action="delete"/><outer nrdo.action="delete"/>
                    throw new ArgumentException("Not yet implemented: cannot use 'back-references as nested element' construct between two elements that are both being deleted (" + newElement.LocalName + " and " + childElement.LocalName + ")");
                }
                else
                {
                    // CanonicalizeElement(childElement, tableType, afterElement) and save the result.
                    // Add a reference-named attribute to the result with the nrdo.id of the current element
                    if (!newElement.HasAttribute("nrdo.id")) throw new ArgumentException("Cannot use 'back-references as nested element' construct on a " + type.Name + " element without a nrdo.id");
                    XmlElement refResult = CanonicalizeElement(childElement, table, afterElement);
                    if (refResult.HasAttribute(refs[0].Name)) throw new ArgumentException("Cannot specify " + refs[0].Name + " attribute directly on " + name + " when nesting inside a " + type.Name);
                    refResult.SetAttribute(refs[0].Name, XmlUtil.GetAttr(newElement, "nrdo.id"));
                }
            }
        }
        internal static int debugCounter = 0;
        internal XmlDocument Canonicalize(XmlDocument doc)
        {
            List<ITransformRecipe> transformQueue = new List<ITransformRecipe>(Transforms);

            debugCounter++;
            bool again = true;
            int pass = 0;
            while (again)
            {
                pass++;
                debugDump(doc, "recipe-" + debugCounter + "-pass-" + pass + "-in");
                List<ITransformRecipe> newTransforms = new List<ITransformRecipe>();
                XmlElement recipeElement = doc.DocumentElement;
                if (recipeElement.Name != "nrdo.recipe") throw new InvalidDataException("Root element of a recipe must be <nrdo.recipe>");
                foreach (XmlElement childElement in new List<XmlElement>(Recipe.elementChildren(recipeElement)))
                {
                    if (childElement.LocalName.StartsWith("nrdo."))
                    {
                        if (childElement.LocalName == "nrdo.transform")
                        {
                            recipeElement.RemoveChild(childElement);
                            string type = XmlUtil.GetAttr(childElement, "type") ?? "xslt";
                            if (!transformTypes.ContainsKey(type)) throw new ArgumentException("Unknown transform type '" + type + "'");
                            ITransformRecipe transform = (ITransformRecipe)Activator.CreateInstance(transformTypes[type]);
                            if (childElement.HasAttribute("src")) transform.SourceFile = XmlUtil.GetAttr(childElement, "src");
                            newTransforms.Add(transform);
                        }
                    }
                    else
                    {
                        NrdoTable table = NrdoTable.GetTable(Lookup, childElement.LocalName.Replace('.', ':'));
                        if (table == null) throw new ArgumentException("Table " + childElement.LocalName + " could not be found.");

                        // This is something of a hack - insert a dummy element after the real element that's
                        // going to be removed as part of the canonicalization, for
                        // content to be inserted before. But it works and saves having to
                        // come up with a probably more complicated and hacky robust way to
                        // specify a location in the document that may NOT correspond to any
                        // actual element.
                        XmlElement dummy = doc.CreateElement("dummy");
                        recipeElement.InsertAfter(dummy, childElement);
                        CanonicalizeElement(childElement, table, dummy);
                        recipeElement.RemoveChild(dummy);
                    }
                }
                transformQueue.InsertRange(0, newTransforms);
                debugDump(doc, "recipe-" + debugCounter + "-pass-" + pass + "-out");

                if (transformQueue.Count == 0)
                {
                    again = false;
                }
                else
                {
                    doc = transformQueue[0].Transform(doc);
                    transformQueue.RemoveAt(0);
                }
            }
            return doc;
        }
        [Conditional("DEBUGCANONICALIZE")]
        private void debugDump(XmlDocument doc, string name)
        {
            using (XmlWriter writer = XmlWriter.Create("p:\\" + name + ".xml"))
            {
                doc.WriteTo(writer);
            }
        }

        internal void ChangeNrdoId(string from, string to)
        {
            if (from == null || to == null)
            {
                throw new ApplicationException("nrdo.id.changed element must have 'from' and 'to' attributes specified");
            }

            if (!records.ContainsKey(from)) return;

            if (records.ContainsKey(to))
            {
                throw new ApplicationException("Cannot change nrdo.id from '" + from + "' to '" + to + "' because there already exists a record by that name");
            }

            records[to] = records[from];
            records.Remove(from);

            records[to].SetNrdoId(to);
        }
    }
}
