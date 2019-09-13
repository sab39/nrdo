using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Smelt
{
    public sealed class TextFragment
    {
        private readonly LineNumberedText fromText;
        public LineNumberedText FromText { get { return fromText; } }

        private readonly int start;
        public int Start { get { return start; } }

        private readonly int length;
        public int Length { get { return length; } }

        public TextFragment(LineNumberedText fromText, int start, int length)
        {
            this.fromText = fromText;
            this.start = start;
            this.length = length;
        }

        public int End { get { return Start + Length - 1; } }

        public string Text { get { return FromText.RawText.Substring(Start, Length); } }

        public override string ToString()
        {
            return Text;
        }

        public TextLocation StartLocation { get { return FromText.GetLocationOfIndex(Start); } }
        public TextLocation EndLocation { get { return FromText.GetLocationOfIndex(End); } }

        public string Location
        {
            get
            {
                var startLoc = StartLocation;
                var endLoc = EndLocation;
                return startLoc.Line == endLoc.Line ? (startLoc + "-" + endLoc.Column) : (startLoc + " - " + endLoc);
            }
        }
    }
}
