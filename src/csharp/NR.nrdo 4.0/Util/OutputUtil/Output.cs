using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace NR.nrdo.Util.OutputUtil
{
    public static class Output
    {
        public static IOutput Console(Func<string> getPrefix = null)
        {
            return new ConsoleOutput(getPrefix);
        }

        public static IOutput ConsoleVerbose(Func<string> getPrefix = null)
        {
            return AllOf(new ConsoleOutput(getPrefix, ConsoleColor.White), new VerboseConsoleOutput(getPrefix));
        }

        public static IOutput None { get { return AllOf(Enumerable.Empty<IOutput>()); } }

        public static IOutput AllOf(IOutput first, IOutput second, params IOutput[] rest)
        {
            return AllOf(new[] { first, second }.Concat(rest));
        }

        public static void Message(this IOutput output, string message)
        {
            output.Report(OutputMode.Normal, message);
        }

        public static void Warning(this IOutput output, string warning)
        {
            output.Report(OutputMode.Warning, warning);
        }

        public static void Error(this IOutput output, string error)
        {
            output.Report(OutputMode.Error, error);
        }

        public static void Verbose(this IOutput output, string message)
        {
            output.ReportVerbose(message);
        }

        public static IOutput AllOf(IEnumerable<IOutput> outputs)
        {
            var outputList = outputs.ToList().AsReadOnly();
            if (outputList.Count == 1) return outputList[1];

            return new MultiOutput(outputList);
        }

        public abstract class OutputBase : IOutput
        {
            public virtual void Report(OutputMode mode, string message)
            {
            }

            public virtual void ReportVerbose(string message)
            {
            }

            public virtual bool CanPrompt { get { return false; } }

            public virtual bool Prompt(OutputMode mode, string information, string yesNoQuestion)
            {
                throw new NotImplementedException();
            }

            protected OutputMode Status { get; private set; }

            public virtual void SetStatus(OutputMode status)
            {
                Status = status;
                RefreshStatus();
            }
            protected virtual void RefreshStatus()
            {
            }

            public virtual void Progress(Portion progress)
            {
            }
        }

        private abstract class ConsoleOutputBase : OutputBase
        {
            private readonly Func<string> getPrefix;
            protected ConsoleOutputBase(Func<string> getPrefix)
            {
                this.getPrefix = getPrefix;
            }

            protected static ConsoleColor? statusColor(OutputMode mode)
            {
                switch (mode) {
                    case OutputMode.Normal: return ConsoleColor.Green;
                    case OutputMode.Warning: return ConsoleColor.DarkYellow;
                    case OutputMode.Error: return ConsoleColor.Red;
                    default: return null;
                }
            }

            private static bool atStartOfLine = true;
            private static string tempStatus = null;

            protected void WithColor(ConsoleColor? color, Action action)
            {
                if (color == null)
                {
                    action();
                }
                else
                {
                    var originalColor = System.Console.ForegroundColor;
                    System.Console.ForegroundColor = (ConsoleColor)color;
                    action();
                    System.Console.ForegroundColor = originalColor;
                }
            }

            protected void WriteLine(string msg, ConsoleColor? color)
            {
                WithColor(color, () => WriteLine(msg));
            }

            protected void WriteLine(string msg)
            {
                writePrefix();
                System.Console.WriteLine(msg);
                atStartOfLine = true;
                RefreshStatus();
            }

            protected void WriteLine()
            {
                wipeTempStatus();
                System.Console.WriteLine();
                atStartOfLine = true;
                RefreshStatus();
            }

            protected void Write(string msg, ConsoleColor? color)
            {
                WithColor(color, () => Write(msg));
            }

            protected void Write(string msg)
            {
                writePrefix();
                System.Console.Write(msg);
                atStartOfLine = false;
            }

            protected override void RefreshStatus()
            {
                if (atStartOfLine && tempStatus != null)
                {
                    WithColor(statusColor(Status), () => System.Console.Write(tempStatus + "\r"));
                }
            }
            private void setTempStatus(string msg)
            {
                var expanded = msg.TrimEnd();
                if (tempStatus != null)
                {
                    for (var i = msg.Length; i < tempStatus.Length; i++) expanded += ' ';
                }
                tempStatus = expanded;
                RefreshStatus();
                tempStatus = msg;
            }
            private void wipeTempStatus()
            {
                if (atStartOfLine && tempStatus != null)
                {
                    System.Console.Write(Regex.Replace(tempStatus, ".", " ") + "\r");                    
                }
            }

            public override void Progress(Portion progress)
            {
                setTempStatus(progress.ToString());
            }

            private void writePrefix()
            {
                wipeTempStatus();
                if (atStartOfLine && getPrefix != null)
                {
                    System.Console.Write(getPrefix());
                    atStartOfLine = false;
                }
            }
        }

        private sealed class ConsoleOutput : ConsoleOutputBase
        {
            private readonly ConsoleColor? msgColor;

            public ConsoleOutput(Func<string> getPrefix = null, ConsoleColor? msgColor = null)
                : base(getPrefix)
            {
                this.msgColor = msgColor;
            }

            private static string statusPrefix(OutputMode mode)
            {
                switch (mode)
                {
                    case OutputMode.Normal: return "";
                    case OutputMode.Warning: return "WARNING: ";
                    case OutputMode.Error: return "ERROR: ";
                    default: return null;
                }
            }

            public override void Report(OutputMode mode, string message)
            {
                WriteLine(statusPrefix(mode) + message, mode == OutputMode.Normal ? msgColor : statusColor(mode));
            }

            public override bool CanPrompt { get { return true; } }
            public override bool Prompt(OutputMode mode, string information, string yesNoQuestion)
            {
                Report(mode, information);
                WithColor(statusColor(mode), () =>
                {
                    WriteLine(yesNoQuestion);
                    System.Console.Write("Press [y]/n: ");
                });
                while (true)
                {
                    var response = System.Console.ReadKey();
                    if (response.KeyChar == 'Y' || response.KeyChar == 'y' || response.Key == ConsoleKey.Enter) return true;
                    if (response.KeyChar == 'N' || response.KeyChar == 'n' || response.Key == ConsoleKey.Escape) return false;
                }
            }
        }

        private sealed class VerboseConsoleOutput : ConsoleOutputBase
        {
            public VerboseConsoleOutput(Func<string> getPrefix = null)
                : base(getPrefix) { }

            public override void ReportVerbose(string msg)
            {
                WriteLine(msg, ConsoleColor.Gray);
            }
        }

        private class MultiOutput : OutputBase
        {
            private readonly ReadOnlyCollection<IOutput> outputs;

            internal MultiOutput(ReadOnlyCollection<IOutput> outputs)
            {
                this.outputs = outputs;
            }

            public override void Report(OutputMode mode, string message)
            {
                foreach (var output in outputs) output.Report(mode, message);
            }

            public override void ReportVerbose(string message)
            {
                foreach (var output in outputs) output.ReportVerbose(message);
            }

            public override void Progress(Portion progress)
            {
                foreach (var output in outputs) output.Progress(progress);
            }

            public override bool CanPrompt { get { return outputs.Any(output => output.CanPrompt); } }

            public override bool Prompt(OutputMode mode, string information, string yesNoQuestion)
            {
                return outputs.First(output => output.CanPrompt).Prompt(mode, information, yesNoQuestion);
            }

            public override void SetStatus(OutputMode status)
            {
                foreach (var output in outputs) output.SetStatus(status);
            }
        }
    }
}
