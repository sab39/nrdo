using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public class NrdoIndex : IComparable<NrdoIndex>
    {
        internal NrdoIndex(NrdoIndexAttribute attr)
        {
            index = attr.Index;
            Name = attr.Name;
            IsPrimary = attr.IsPrimary;
            IsUnique = attr.IsUnique || attr.IsPrimary;
            Fields = attr.FieldsSemicolonSeparated.Split(';').ToList();
        }
        private int index { get; set; }
        public string Name { get; private set; }
        public bool IsPrimary { get; private set; }
        public bool IsUnique { get; private set; }
        public List<string> Fields { get; private set; }

        public int CompareTo(NrdoIndex other)
        {
            return index.CompareTo(other.index);
        }
    }
}
