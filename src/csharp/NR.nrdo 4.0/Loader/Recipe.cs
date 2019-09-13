using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using NR.nrdo.Reflection;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using NR.nrdo.Util;

namespace NR.nrdo.Loader
{
    class Recipe
    {
        private readonly XmlDocument doc;

        internal Recipe(Stream stream, RecipeContext context)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            this.doc = context.Canonicalize(doc);
        }
        internal void Run(RecipeContext context)
        {
            DateTime now = DateTime.Now;
            XmlElement recipeElement = doc.DocumentElement;
            if (recipeElement.LocalName != "nrdo.recipe") throw new InvalidDataException("Root element of a recipe must be <nrdo.recipe>");
            foreach (XmlElement recordElement in elementChildren(recipeElement))
            {
                debug(recordElement, "Starting...");

                if (recordElement.LocalName == "nrdo.id.changed")
                {
                    context.ChangeNrdoId(recordElement.GetAttribute("from"), recordElement.GetAttribute("to"));
                    continue;
                }

                // Verify that the element corresponds to a table we know about
                NrdoTable table = NrdoTable.GetTable(context.Lookup, recordElement.LocalName.Replace('.', ':'));
                if (table == null) throw new ArgumentException("No table called " + recordElement.LocalName + " could be found.");

                // Scan for eligible references on the table, and break any corresponding values down into individual attributes
                // of the :nrdoid.fieldname variety.
                // NOTE: It's inefficient to just translate them into :nrdoid.fieldname rather than looking up the value at this
                // point, because we have the object already and don't need to figure everything out again when we come to parse
                // the attribute, but since we're storing the values into an XML attribute, we need something we can stringify.
                // In future we could implement a cache on :nrdoid.fieldname strings, in which case we'd prepopulate that cache
                // at this point. Recipe loading is not considered perf-critical though. Fortunately!
                foreach (NrdoReference reference in table.References)
                {
                    if (isEligibleReference(reference) && recordElement.HasAttribute(reference.Name))
                    {
                        string targetId = XmlUtil.GetAttr(recordElement, reference.Name);
                        RecipeRecord target = context.GetRecord(targetId);
                        recordElement.RemoveAttribute(reference.Name);
                        string raction = XmlUtil.GetAttr(recordElement, "nrdo.action");
                        if (target == null && raction != "none" && raction != "delete")
                        {
                            throw new ArgumentException("Could not find record with nrdo.id " + targetId + " in context (processing " + recordElement.LocalName + " " + XmlUtil.GetAttr(recordElement, "nrdo.id") + " " + raction + ")");
                        }
                        if (target != null)
                        {
                            if (target.TableName != reference.TargetTable.Name) throw new ArgumentException("Target of reference " + reference.Name + " on " + table.Name + " is " + reference.TargetTable.Name + ", but nrdo.id " + targetId + " refers to a " + target.TableName);
                            foreach (NrdoJoin join in reference.Joins)
                            {
                                if (recordElement.HasAttribute(join.From.Field.Name)) throw new ArgumentException("Cannot specify field " + join.From.Field.Name + " at the same time as reference " + reference.Name + " which uses that field");
                                recordElement.SetAttribute(join.From.Field.Name, ":" + targetId + "." + join.To.Field.Name);
                            }
                        }
                    }
                }

                // Construct a key for looking up the value based on primary key
                RecordKey key = new RecordKey(table, delegate(NrdoField field)
                {
                    if (recordElement.HasAttribute(field.Name))
                    {
                        return context.evaluate(field.Type, XmlUtil.GetAttr(recordElement, field.Name));
                    }
                    else
                    {
                        return Undefined.Value;
                    }
                });

                // Validate the nrdo.id attribute
                // nrdo.id is required unless the pkey is not sequenced, AND all the
                // pkey fields are specified (which is equivalent to key.IsDefined)
                string nrdoId = null;
                if (recordElement.HasAttribute("nrdo.id"))
                {
                    nrdoId = XmlUtil.GetAttr(recordElement, "nrdo.id");
                    if (!Regex.IsMatch(nrdoId, "^[A-Za-z_][A-Za-z0-9_-]*$")) throw new ArgumentException("Illegal nrdo.id value " + nrdoId);
                }
                else
                {
                    if (table.IsPkeySequenced || !key.IsDefined)
                    {
                        throw new ArgumentException("Must specify nrdo.id attribute on " + table.Name);
                    }
                }

                // Verify that only attributes that are supposed to exist on the element actually do.
                foreach (XmlAttribute attr in recordElement.Attributes)
                {
                    switch (attr.Name)
                    {
                        case "nrdo.find.by":
                        case "nrdo.find.where":
                            if (!recordElement.HasAttribute("nrdo.id"))
                            {
                                throw new ArgumentException(attr.Name + " can only be specified if nrdo.id is present on " + table.Name);
                            }
                            if (key.IsDefined)
                            {
                                throw new ArgumentException(attr.Name + " cannot be specified if all primary keys are given on " + table.Name + " (" + recordElement.GetAttribute("nrdo.id") + ")");
                            }
                            break;
                        case "nrdo.id":
                        case "nrdo.exists":
                        case "nrdo.action":
                            // These are always legal. Nothing to see here. Move along.
                            break;
                        default:
                            if (table.GetField(attr.Name) == null) throw new ArgumentException("No such field as " + attr.Name + " defined on " + table.Name);
                            break;
                    }
                }

                // Go and look up the record. If found, check its type matches the element, then clone it.
                ITableObject data;
                RecipeRecord record = context.GetRecord(nrdoId, key);
                if (record != null)
                {
                    if (record.TableName != table.Name) throw new ArgumentException("nrdo.id " + nrdoId + " refers to a " + record.TableName + " record in the context, but is used on a " + table.Name + " record here.");
                    data = record.GetData();
                    record = record.Clone();
                }
                else
                {
                    record = new RecipeRecord(context, table, nrdoId);
                    data = null;

                    if (recordElement.HasAttribute("nrdo.find.by") && recordElement.HasAttribute("nrdo.find.where"))
                    {
                        throw new ArgumentException("Cannot specify both nrdo.find.by and nrdo.find.where");
                    }

                    if (recordElement.HasAttribute("nrdo.find.where"))
                    {
                        throw new ArgumentException("nrdo.find.where is not implemented");
                    }
                    else if (recordElement.HasAttribute("nrdo.find.by"))
                    {
                        NrdoSingleGet get = null;
                        foreach (NrdoGet aGet in table.Gets)
                        {
                            if (aGet is NrdoSingleGet && aGet.Name == XmlUtil.GetAttr(recordElement, "nrdo.find.by"))
                            {
                                get = (NrdoSingleGet) aGet;
                                break;
                            }
                        }
                        if (get == null) throw new ArgumentException("No single get by " + XmlUtil.GetAttr(recordElement, "nrdo.find.by") + " found on " + table.Name);
                        data = invokeGet(get, delegate(NrdoField field)
                        {
                            if (recordElement.HasAttribute(field.Name))
                            {
                                return context.evaluate(field.NullableType, XmlUtil.GetAttr(recordElement, field.Name));
                            }
                            else
                            {
                                return null;
                            }
                        });
                    }

                    if (data != null)
                    {
                        foreach (NrdoField field in table.Fields)
                        {
                            record.PutField(new RecipeField(record, field.Name));
                        }
                        foreach (NrdoFieldRef field in table.PkeyGet.Fields)
                        {
                            record.PutField(new RecipeValueField(record, field.Field.Name, field.Field.Get(data)));
                        }
                    }
                }

                // Validate against the nrdo.exists attribute, if present.
                if (recordElement.HasAttribute("nrdo.exists"))
                {
                    string val = XmlUtil.GetAttr(recordElement, "nrdo.exists");
                    switch (val)
                    {
                        case "required":
                            if (data == null) throw new ArgumentException("The record " + nrdoId + " is specified as 'required' in the recipe, but does not exist");
                            break;
                        case "permitted":
                            break;
                        case "error":
                            if (data != null) throw new ArgumentException("The record " + nrdoId + " exists, but the recipe specifies that it must not");
                            break;
                        default:
                            throw new ArgumentException("Illegal value " + val + " for nrdo.exists attribute, legal values are required, permitted and error");
                    }
                }

                // Determine what needs to be done based on the nrdo.action attribute
                string action = "update";
                bool setFields = true;
                bool overwriteFields = false;
                if (recordElement.HasAttribute("nrdo.action")) action = XmlUtil.GetAttr(recordElement, "nrdo.action");
                switch (action)
                {
                    case "none":
                        setFields = false;
                        break;
                    case "ensure":
                        if (data != null) setFields = false;
                        // Otherwise equivalent to "update", which is the default case, nothing to do.
                        break;
                    case "update":
                        // Nothing to do; this is the default case.
                        break;
                    case "replace":
                        overwriteFields = true;
                        break;
                    case "delete":
                        if (data != null)
                        {
                            data.Delete();
                            context.RemoveRecord(record);
                        }
                        continue;
                    default:
                        throw new ArgumentException("Illegal value " + action + " for the nrdo.action attribute, legal values are none, ensure, update, replace and delete");
                }

                // If the record doesn't correspond to an existing item in the database, create it.
                if (data == null)
                {
                    // If the record wasn't found AND isn't being created, then we don't need to add it to the database or
                    // to the context.
                    if (!setFields) continue;

                    List<object> ctorArgs = new List<object>();
                    foreach (NrdoField field in table.CtorParams)
                    {
                        object value = Undefined.Value;
                        if (recordElement.HasAttribute(field.Name))
                        {
                            value = context.evaluate(field.Type, XmlUtil.GetAttr(recordElement, field.Name));
                        }
                        if (value is Undefined) value = defaultValue(field.Type, field.IsNullable);
                        if (value is Now)
                        {
                            ctorArgs.Add(now);
                            record.PutField(new RecipeField(record, field.Name));
                        }
                        else
                        {
                            ctorArgs.Add(value);
                            record.PutField(new RecipeValueField(record, field.Name, value));
                        }
                    }
                    data = table.Create(ctorArgs.ToArray());
                }

                // Set the appropriate fields based on what's already known in the context and whether overwriteFields is true
                foreach (NrdoField field in table.Fields)
                {
                    // Figure out whether this particular field should get set
                    bool setIt = false;
                    if (field.IsWritable && recordElement.HasAttribute(field.Name))
                    {
                        if (data.IsNew || overwriteFields)
                        {
                            setIt = setFields;
                        }
                        else
                        {
                            RecipeField rfield = record.GetField(field.Name);
                            if (rfield == null)
                            {
                                // Wish I could figure out something smarter to do here
                                setIt = false;
                            }
                            else if (rfield is RecipeValueField)
                            {
                                object value = ((RecipeValueField) rfield).Value;
                                if (object.Equals(field.Get(data), value))
                                {
                                    setIt = setFields;
                                }
                            }
                        }
                    }
                    // Set the field
                    if (setIt)
                    {
                        object value = context.evaluate(field.Type, XmlUtil.GetAttr(recordElement, field.Name));
                        if (value is Now)
                        {
                            field.Set(data, now);
                            record.PutField(new RecipeField(record, field.Name));
                        }
                        else
                        {
                            field.Set(data, value);
                            record.PutField(new RecipeValueField(record, field.Name, value));
                        }
                    }
                    // Save the fact that we know the field, but not the value. But not if it's a readonly field,
                    // because that would have been set when the ctor was being processed.
                    else if (record.GetField(field.Name) == null)
                    {
                        // If the record was newly created, we know the value is the default for the type. Otherwise,
                        // we store that we just don't know the value.
                        if (data.IsNew)
                        {
                            if (field.IsWritable)
                            {
                                record.PutField(new RecipeValueField(record, field.Name, defaultValue(field.Type, field.IsNullable)));
                            }
                            else
                            {
                                // must either have been picked up by the constructor, or
                                // be the sequenced pkey and will be picked up below
                            }
                        }
                        else
                        {
                            record.PutField(new RecipeField(record, field.Name));
                        }
                    }
                }

                // Save the newly created object into the database and into the context
                if (setFields) data.Update();

                // This covers sequenced pkeys and also covers the case where
                // a DateTime field in the pkey was set to :now. If it's part of
                // the pkey we can't change it (so storing the value is harmless)
                // but we need to know it for future reference.
                foreach (NrdoFieldRef pkeyField in table.PkeyGet.Fields)
                {
                    record.PutField(new RecipeValueField(record, pkeyField.Field.Name, pkeyField.Field.Get(data)));
                }
                context.PutRecord(record);
                debug(recordElement, "Finished");
            }
        }

        internal static object defaultValue(Type type, bool nullable)
        {
            // Nullable<T> is a value type, but in that case Activator.CreateInstance
            // will return null. For all other value types, Activator will create a
            // default instance.
            if (type.IsValueType && !nullable)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        internal static bool isEligibleReference(NrdoReference reference)
        {
            // From the spec:
            // A reference is eligible if it's a single reference, contains no extra tables, fields or parameters,
            // and all joins are directly from the source table to the destination table ("joins {* to *}", or "by")

            if (reference.IsMulti) return false;

            if (reference.Tables.Count > 0) return false;
            if (reference.Fields.Count > 0) return false;
            if (reference.Params.Count > 0) return false;
            if (reference.ParamTables.Count > 0) return false;

            foreach (NrdoJoin join in reference.Joins)
            {
                if (!join.isSelfToTarget) return false;
            }
            return true;
        }

        internal static ITableObject invokeGet(NrdoSingleGet get, FieldValueGetter getFieldValue)
        {
            if (get.Params.Count > 0 || get.ParamTables.Count > 0) throw new ArgumentException("Get by " + get.Name + " is not eligible for use in nrdo.find.by since it has parameters that are not fields");
            object[] args = new object[get.Fields.Count];
            int i = 0;
            foreach (NrdoFieldRef field in get.Fields)
            {
                if (!field.Table.IsSelf) throw new ArgumentException("Get by " + get.Name + " is not eligible for use in nrdo.find.by since it uses fields not on the current table");
                object value = getFieldValue(field.Field);
                args[i++] = value;
                if (value is Undefined) return null; // We skip attempts to call gets on Undefined values because the value is inherently unknown, the get couldn't succeed.
            }
            return get.Call(args);
        }

        public static event DebugPrinter Debug;

        internal static void debug(string msg)
        {
            DebugPrinter handler = Debug;
            if (handler != null) handler(msg);
        }
        internal static void debug(RecipeRecord rec, string msg)
        {
            debug(rec.TableName + " " + rec.NrdoId + " (" + new RecordKey(rec) + "): " + msg);
        }
        internal static void debug(XmlElement elem, string msg)
        {
            debug("<" + elem.LocalName + "> " + elem.GetAttribute("nrdo.id") + " " + elem.GetAttribute("nrdo.action") + ": " + msg);
        }

        internal static IEnumerable<XmlElement> elementChildren(XmlElement element)
        {
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement child = node as XmlElement;
                if (child != null)
                {
                    yield return child;
                }
                else if (node is XmlWhitespace || node is XmlComment)
                {
                    // Do nothing
                }
                else
                {
                    throw new ArgumentException("<" + element.LocalName + "> can only contain elements, not " + node);
                }
            }
        }
    }
}
