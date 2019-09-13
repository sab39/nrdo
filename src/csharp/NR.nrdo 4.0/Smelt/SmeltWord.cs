using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Smelt
{
    public abstract class SmeltWord : SmeltNode
    {
        protected SmeltWord(TextFragment fragment)
            : base(fragment) { }
    }
}
