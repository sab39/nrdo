using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;

namespace NR.nrdo.Smelt
{
    public sealed class LineNumberedText
    {
        private readonly string rawText;
        public string RawText { get { return rawText; } }

        private readonly Lazy<ImmutableList<int>> lineStarts;

        public LineNumberedText(string rawText)
        {
            if (rawText == null) throw new ArgumentNullException("rawText");
            this.rawText = rawText;
            this.lineStarts = new Lazy<ImmutableList<int>>(() => scanLines(rawText).ToImmutableList());
        }

        private static IEnumerable<int> scanLines(string text)
        {
            var start = 0;
            do
            {
                yield return start;
                start = text.IndexOf('\n', start + 1);
            }
            while (start >= 0);
        }

        public int Length { get { return rawText.Length; } }

        public int LineCount { get { return lineStarts.Value.Count; } }

        public sealed class FragmentHelper
        {
            private readonly LineNumberedText text;
            private readonly int start;
            internal FragmentHelper(LineNumberedText text, int start)
            {
                this.text = text;
                this.start = start;
            }
            public TextFragment Length(int length)
            {
                return new TextFragment(text, start, length);
            }
            public TextFragment To(int end)
            {
                return new TextFragment(text, start, end - start + 1);
            }
        }
        public FragmentHelper From(int start)
        {
            return new FragmentHelper(this, start);
        }

        public TextLocation GetLocationOfIndex(int index)
        {
            // Would be more efficient with a binary search but this will do
            var lines = lineStarts.Value;
            var line = 0;
            while (line < lines.Count && lines[line] <= index) line++;

            return new TextLocation(line, index - lines[line - 1] + 1);
        }

        public override bool Equals(object obj)
        {
            return obj is LineNumberedText && ((LineNumberedText)obj).rawText == rawText;
        }
        public override int GetHashCode()
        {
            return rawText.GetHashCode();
        }
        public override string ToString()
        {
            return rawText;
        }
    }
}
