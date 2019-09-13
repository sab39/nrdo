using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;

namespace NR.nrdo.Util.OutputUtil
{
    public static class Progress
    {
        public static Block ProgressBlock(this IOutput output, int totalSteps = 0)
        {
            return Block.Create(output, Portion.Zero, Portion.Complete, totalSteps).Start();
        }

        public class Block : IOutput, IDisposable
        {
            private readonly IOutput output;
            private readonly Func<Portion, Portion> parentTransform;

            private readonly Portion offset;
            private readonly Portion scale;

            internal static Block Create(IOutput output, Portion offset, Portion scale, int totalSteps)
            {
                var sub = output as Block;
                if (sub != null)
                {
                    return new Block(sub.output, sub.transform, offset, scale, totalSteps);
                }
                else
                {
                    return new Block(output, portion => portion, offset, scale, totalSteps);
                }
            }
            private Block(IOutput output, Func<Portion, Portion> parentTransform, Portion offset, Portion scale, int totalSteps)
            {
                if (output == null) throw new ArgumentNullException("output");

                this.output = output;
                this.offset = offset;
                this.scale = scale;
                this.totalSteps = totalSteps;
                this.parentTransform = parentTransform ?? (portion => portion);
            }

            private Portion transform(Portion progress)
            {
                return parentTransform(offset + progress * scale);
            }

            public void Progress(Portion progress)
            {
                output.Progress(transform(progress));
            }

            public Portion CurrentProgress { get { return Portion.Ratio(Step, TotalSteps); } }

            private int totalSteps = 0;
            public int TotalSteps
            {
                get { return totalSteps; }
                set
                {
                    if (value < 0) throw new ArgumentOutOfRangeException("TotalSteps cannot be negative");
                    totalSteps = value;
                    Progress(CurrentProgress);
                }
            }

            private int step = 0;
            public int Step
            {
                get { return step < 0 ? 0 : step > totalSteps ? totalSteps : step; }
                set
                {
                    if (TotalSteps == 0 && value != 0) throw new ArgumentOutOfRangeException("Cannot set Steps > 0 when TotalSteps is zero");
                    step = value;
                    Progress(CurrentProgress);
                }
            }

            public Block BlockForStep(int blockSteps = 0)
            {
                return BlockForSteps(1, blockSteps);
            }
            public Block BlockForSteps(int steps, int blockSteps = 0)
            {
                var start = CurrentProgress;
                step += steps; // Done on the raw variable so it doesn't trigger a progress update
                var end = CurrentProgress;
                return new Block(this, parentTransform, start, end - start, blockSteps);
            }

            public IEnumerable<Block> GetChunks(params int[] proportions)
            {
                var total = proportions.Sum();

                var nextBlockStart = 0;
                foreach (var size in proportions)
                {
                    var offset = Portion.Ratio(nextBlockStart, total);
                    var scale = Portion.Ratio(size, total);
                    var block = new Block(this, parentTransform, offset, scale, size);
                    yield return block;

                    nextBlockStart += size;
                }
            }

            // Returns the block for use in using() statements
            public Block Start()
            {
                step = 0;
                Progress(Portion.Zero);
                return this;
            }
            public void ProgressComplete()
            {
                step = totalSteps;
                Progress(Portion.Complete);
            }
            void IDisposable.Dispose()
            {
                ProgressComplete();
            }

            public void Report(OutputMode mode, string message)
            {
                output.Report(mode, message);
            }

            public void ReportVerbose(string msg)
            {
                output.ReportVerbose(msg);
            }

            public bool CanPrompt
            {
                get { return output.CanPrompt; }
            }

            public bool Prompt(OutputMode mode, string information, string yesNoQuestion)
            {
                return output.Prompt(mode, information, yesNoQuestion);
            }

            public void SetStatus(OutputMode status)
            {
                output.SetStatus(status);
            }        
        }
    }
}
