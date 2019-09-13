using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Smelt
{
    public sealed class SmeltAtom : SmeltString
    {
        internal SmeltAtom(TextFragment fragment)
            : base(fragment) { }

        public override string Text
        {
            get { return Fragment.Text; }
        }

        public override int GetSourceIndex(int index)
        {
            if (index < 0 || index >= Fragment.Length) throw new IndexOutOfRangeException();
            return Fragment.Start + index;
        }
    }
}
