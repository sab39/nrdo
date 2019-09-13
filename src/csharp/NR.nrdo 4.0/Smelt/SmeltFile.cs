using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;

namespace NR.nrdo.Smelt
{
    public sealed class SmeltFile : SmeltNode
    {
        private readonly ImmutableList<SmeltLine> lines;
        public ImmutableList<SmeltLine> Lines { get { return lines; } }

        internal SmeltFile(LineNumberedText text, ImmutableList<SmeltLine> lines)
            : base(text.From(0).Length(text.Length))
        {
            this.lines = lines;
        }


        public static SmeltFile Parse(string sourceText)
        {
            return new SmeltParser(sourceText).Parse();
        }
    }
}
