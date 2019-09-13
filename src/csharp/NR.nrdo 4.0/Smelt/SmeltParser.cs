using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;

namespace NR.nrdo.Smelt
{
    internal class SmeltParser
    {
        private readonly LineNumberedText text;
        private readonly string sourceText;

        internal SmeltParser(string sourceText)
        {
            this.sourceText = sourceText;
            this.text = new LineNumberedText(sourceText);
        }

        private int index = 0;
        private State state = State.InLine;

        private BuildingLine currentLine;
        private BuildingBlock currentBlock;
        private BuildingLiteral currentLiteral;
        private int atomStart;

        private enum State
        {
            InLine, InAtom, InComment, InLiteral, InLiteralEscape, InLiteralComment
        }

        private void endLineSoft()
        {
            if (currentLine.words.Count > 0)
            {
                var line = new SmeltLine(text.From(currentLine.words[0].Fragment.Start)
                                             .To(currentLine.words.Last().Fragment.End),
                    currentLine.words.ToImmutableList());
                currentBlock.lines.Add(line);
                currentLine = new BuildingLine(currentBlock);
            }
        }

        internal SmeltFile Parse()
        {
            currentBlock = new BuildingBlock(0, null);
            currentLine = new BuildingLine(currentBlock);

            while (index < sourceText.Length)
            {
                var ch = sourceText[index];
                switch (state) {

                    case State.InLine:
                        if (ch == '{')
                        {
                            currentBlock = new BuildingBlock(index, currentLine);
                            currentLine = new BuildingLine(currentBlock);
                        }
                        else if (ch == '[')
                        {
                            currentLiteral = new BuildingLiteral(index);
                            state = State.InLiteral;
                        }
                        else if (ch == '#')
                        {
                            state = State.InComment;
                        }
                        else if (ch == ';')
                        {
                            // This is a 'hard' end of line, as opposed to endLineSoft()
                            // A semicolon always causes a line to actually exist, even if it's empty
                            // { } is a block with no lines in it, but { ; } is a block with one (empty) line in it.
                            // Similarly, { hello ; ; world } is a block with three lines in it (a line containing "hello", an empty line, and
                            // a line containing "world").
                            // The boundaries of the fragments look like this:
                            // { hello ; ; world ; } { is it ; me ; ; you're looking for }
                            //   ------- - -------     ------- ---- - ------------------
                            int start = currentLine.words.Count == 0 ? index : currentLine.words[0].Fragment.Start;
                            currentBlock.lines.Add(new SmeltLine(text.From(start).To(index), currentLine.words.ToImmutableList()));
                            currentLine = new BuildingLine(currentBlock);
                        }
                        else if (ch == '}')
                        {
                            endLineSoft();

                            if (currentBlock.inLine == null) throw new ApplicationException("Close block that was not open"); // FIXME should be a parse exception using the line position

                            currentLine = currentBlock.inLine;
                            currentLine.words.Add(new SmeltBlock(text.From(currentBlock.start).To(index), currentBlock.lines.ToImmutableList()));
                            currentBlock = currentLine.inBlock;
                        }
                        else if (ch == ']')
                        {
                            throw new ApplicationException("Illegal character"); // FIXME Should error more usefully
                        }
                        else if (char.IsWhiteSpace(ch))
                        {
                            // do nothing
                        }
                        else
                        {
                            atomStart = index;
                            state = State.InAtom;
                        }
                        break;

                    case State.InComment:
                        if (ch == '\n' || ch == '\r')
                        {
                            state = State.InLine;
                        }
                        else
                        {
                            // do nothing, continue parsing the comment
                        }
                        break;

                    case State.InLiteral:
                        if (ch == '[')
                        {
                            currentLiteral.fragmentTo(text, index);
                            state = State.InLiteralEscape;
                        }
                        else if (ch == ']')
                        {
                            currentLiteral.fragmentTo(text, index);
                            currentLine.words.Add(new SmeltLiteral(text.From(currentLiteral.start).To(index), currentLiteral.fragments.ToImmutableList()));
                            state = State.InLine;
                        }
                        else
                        {
                            // do nothing
                        }
                        break;

                    case State.InLiteralEscape:
                        if (ch == '#')
                        {
                            state = State.InLiteralComment;
                        }
                        else if (ch == '[' || ch == ']')
                        {
                            state = State.InLiteral;
                        }
                        else
                        {
                            throw new ApplicationException("Unexpected escaped character"); // FIXME
                        }
                        break;

                    case State.InLiteralComment:
                        if (ch == '\r' || ch == '\n')
                        {
                            currentLiteral.fragmentStart = index;
                            state = State.InLiteral;
                        }
                        else
                        {
                            // do nothing
                        }
                        break;

                    case State.InAtom:
                        if (ch == '[' || ch == ']')
                        {
                            throw new ApplicationException("Illegal character"); // FIXME error more usefully
                        }
                        else if (ch == '{' || ch == '}' || ch == ';' || char.IsWhiteSpace(ch))
                        {
                            index--;
                            currentLine.words.Add(new SmeltAtom(text.From(atomStart).To(index)));
                            state = State.InLine;
                        }
                        else
                        {
                            // do nothing, continue parsing the atom
                        }
                        break;
                }
                index++;
            }

            switch (state) {
                case State.InAtom:
                    currentLine.words.Add(new SmeltAtom(text.From(atomStart).To(index - 1)));
                    break;
                case State.InLiteral:
                case State.InLiteralEscape:
                    throw new ApplicationException("Unterminated string literal"); // FIXME
            }

            endLineSoft();

            if (currentBlock.inLine != null)
            {
                throw new ApplicationException("Unterminated block"); // FIXME
            }

            return new SmeltFile(text, currentBlock.lines.ToImmutableList());
        }

        private class BuildingLine
        {
            internal readonly BuildingBlock inBlock;
            internal readonly List<SmeltWord> words = new List<SmeltWord>();

            internal BuildingLine(BuildingBlock inBlock)
            {
                this.inBlock = inBlock;
            }
        }

        private class BuildingBlock
        {
            internal readonly int start;
            internal readonly BuildingLine inLine;
            internal readonly List<SmeltLine> lines = new List<SmeltLine>();

            internal BuildingBlock(int start, BuildingLine inLine)
            {
                this.start = start;
                this.inLine = inLine;
            }
        }

        private class BuildingLiteral
        {
            internal readonly int start;
            internal readonly List<TextFragment> fragments = new List<TextFragment>();

            internal int fragmentStart;
            internal void fragmentTo(LineNumberedText text, int index)
            {
                fragments.Add(text.From(fragmentStart).To(index - 1));
                fragmentStart = index + 1;
            }

            internal BuildingLiteral(int start)
            {
                this.start = start;
                fragmentStart = start + 1;
            }
        }
    }
}
