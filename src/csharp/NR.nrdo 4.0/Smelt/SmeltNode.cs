using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Smelt
{
    public abstract class SmeltNode
    {
        private readonly TextFragment fragment;
        public TextFragment Fragment { get { return fragment; } }

        protected SmeltNode(TextFragment fragment)
        {
            this.fragment = fragment;
        }

        public string SourceText { get { return Fragment.ToString(); } }
    }
}
