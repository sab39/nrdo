using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Util.OutputUtil
{
    public interface IOutput
    {
        void Report(OutputMode mode, string message);
        void ReportVerbose(string msg);

        bool CanPrompt { get; }
        bool Prompt(OutputMode mode, string information, string yesNoQuestion);

        void SetStatus(OutputMode status);
        void Progress(Portion progress);
    }
}
