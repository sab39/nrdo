using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Smelt
{
    public struct TextLocation
    {
        private readonly int line;
        public int Line { get { return line; } }

        private readonly int column;
        public int Column { get { return column; } }

        public TextLocation(int line, int column)
        {
            this.line = line;
            this.column = column;
        }

        public override string ToString()
        {
            return "line " + Line + " col " + Column;
        }
    }
}
