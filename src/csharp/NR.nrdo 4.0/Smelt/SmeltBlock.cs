using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;

namespace NR.nrdo.Smelt
{
    public sealed class SmeltBlock : SmeltWord
    {
        private readonly ImmutableList<SmeltLine> lines;
        public ImmutableList<SmeltLine> Lines { get { return lines; } }

        internal SmeltBlock(TextFragment fragment, ImmutableList<SmeltLine> lines)
            : base(fragment)
        {
            this.lines = lines;
        }
    }
}
