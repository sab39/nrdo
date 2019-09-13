using System;
using System.Collections.Generic;
using System.Text;
using NR.nrdo.Reflection;

namespace NR.nrdo.Loader
{
    public class RecipeField
    {
        internal RecipeField(RecipeRecord record, string name)
        {
            this.record = record;
            this.name = name;
        }

        private readonly RecipeRecord record;
        public RecipeRecord Record { get { return record; } }

        public RecipeContext Context { get { return Record.Context; } }

        private readonly string name;
        public string Name { get { return name; } }

        private NrdoField field;
        public NrdoField Field
        {
            get
            {
                if (field == null) field = Record.Table.GetField(Name);
                return field;
            }
        }

        public RecipeField CopyTo(RecipeRecord record)
        {
            RecipeField field = CloneTo(record);
            record.PutField(field);
            return field;
        }
        public RecipeField Clone()
        {
            return CloneTo(Record);
        }
        protected virtual RecipeField CloneTo(RecipeRecord record)
        {
            return new RecipeField(record, Name);
        }
    }
    class RecipeValueField : RecipeField
    {
        internal RecipeValueField(RecipeRecord record, string name, object value)
            : base(record, name)
        {
            this.value = value;
        }

        private object value;
        public object Value { get { return value; } set { this.value = value; } }

        protected override RecipeField CloneTo(RecipeRecord record)
        {
            return new RecipeValueField(record, Name, Value);
        }
    }
}
