using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;

namespace NR.nrdo.Smelt
{
    public sealed class SmeltLine : SmeltNode
    {
        private readonly ImmutableList<SmeltWord> words;
        public ImmutableList<SmeltWord> Words { get { return words; } }

        internal SmeltLine(TextFragment fragment, ImmutableList<SmeltWord> words)
            : base(fragment)
        {
            this.words = words;
        }

        public string GetString(int index)
        {
            return ((SmeltString)words[index]).Text;
        }

        public SmeltBlock GetBlock(int index)
        {
            return (SmeltBlock)words[index];
        }
    }
}
