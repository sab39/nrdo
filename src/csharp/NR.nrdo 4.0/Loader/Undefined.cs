using System;
using System.Collections.Generic;
using System.Text;

namespace NR.nrdo.Loader
{
    internal class Undefined
    {
        private Undefined() { }
        internal static readonly Undefined Value = new Undefined();
    }
}
