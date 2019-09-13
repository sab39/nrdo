using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Smelt
{
    public abstract class SmeltString : SmeltWord
    {
        protected SmeltString(TextFragment fragment)
            : base(fragment) { }

        public abstract string Text { get; }
        public abstract int GetSourceIndex(int index);

        public override string ToString()
        {
            return Text;
        }
    }
}
