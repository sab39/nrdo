using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using net.netreach.nrdo.tools;
using net.netreach.util;
using System.Windows.Forms;
using System.Linq;
using System.IO;

namespace NR.nrdo.Build
{
    public class GenerateCode : Task
    {
        [Required]
        public ITaskItem NrdoFile { get; set; }

        public ITaskItem[] DfnFiles { get; set; }

        public ITaskItem[] QuFiles { get; set; }

        public bool IgnoreLackOfDfnFiles { get; set; }

        [Required]
        public bool DoGenerateCode { get; set; }

        [Required]
        public bool DoCreateTables { get; set; }

        public bool DropTables { get; set; }

        [Output]
        public ITaskItem[] CSharpFiles { get; set; }

        public override bool Execute()
        {
            if (NrdoFile == null) return true;

            if (DfnFiles == null && QuFiles == null && !IgnoreLackOfDfnFiles) return true;

            NRDOTool.reset();
            var output = new TaskOutputProvider(this);
            Output.addOutputProvider(output, true);
            Output.setPromptProvider(output);
            if (NRDOTool.doMain(NrdoFile.GetMetadata("FullPath"), DoGenerateCode, DoCreateTables, DropTables, null)) return true;

            if (!output.errorReported) Log.LogError("nrdo failed - see output window for details");
            return false;
        }
    }

    public class CollectGeneratedFiles : Task
    {
        [Required]
        public ITaskItem NrdoFile { get; set; }

        public ITaskItem[] DfnFiles { get; set; }

        public ITaskItem[] QuFiles { get; set; }

        [Output]
        public ITaskItem[] CSharpFiles { get; set; }

        public override bool Execute()
        {
            if (NrdoFile == null) return true;

            var csFiles = new List<ITaskItem>() {
                new TaskItem("NrdoGlobal.cs")
            };
            if (DfnFiles != null) csFiles.AddRange(from item in DfnFiles select new TaskItem(item.ItemSpec + ".gen.cs") as ITaskItem);
            if (QuFiles != null) csFiles.AddRange(from item in QuFiles select new TaskItem(item.ItemSpec + ".gen.cs") as ITaskItem);

            CSharpFiles = csFiles.FindAll(item => File.Exists(item.GetMetadata("FullPath"))).ToArray();

            return true;
        }
    }

    internal class TaskOutputProvider : OutputProvider, PromptProvider
    {
        private readonly Task task;
        internal bool errorReported = false;

        internal TaskOutputProvider(Task task)
        {
            this.task = task;
        }

        public void reportError(FileLocation loc, string str)
        {
            if (str == null) return;
            errorReported = true;
            if (loc != null)
            {
                task.Log.LogError("nrdo", null, null, loc.getFile().getAbsolutePath(),
                    loc.getLine(), loc.getCol(), loc.getEndLine(), loc.getEndCol(), str);
            }
            else
            {
                task.Log.LogError(str);
            }
        }

        public void println(string str)
        {
            task.Log.LogMessage(MessageImportance.High, DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss: ") + str);
        }

        public bool prompt(string prompt, string question)
        {
            return MessageBox.Show(prompt + "\r\n\r\n" + question, "nrdo prompt", MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        public void setCurrentProgress(int i)
        {
        }

        public void setTotalProgress(int i)
        {
        }
    }

}
