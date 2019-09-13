using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;

namespace NR.nrdo.Smelt
{
    public sealed class SmeltLiteral : SmeltString
    {
        private readonly ImmutableList<TextFragment> fragments;
        public ImmutableList<TextFragment> Fragments { get { return fragments; } }

        internal SmeltLiteral(TextFragment fragment, ImmutableList<TextFragment> fragments)
            : base(fragment)
        {
            this.fragments = fragments;
        }

        public override string Text
        {
            get { return string.Concat(fragments); }
        }

        public override int GetSourceIndex(int index)
        {
            foreach (var fragment in fragments)
            {
                if (index < fragment.Length) return fragment.Start + index;
                index -= fragment.Length;
            }
            throw new IndexOutOfRangeException();
        }
    }
}
