using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TemplateWizard;
using EnvDTE;
using System.IO;
using System.Text.RegularExpressions;

namespace CMSVisualStudio
{
    public class MangleNames : IWizard
    {
        private List<ProjectItem> itemsAdded;

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            string filePath = projectItem.get_FileNames(1);
            if (filePath.EndsWith(".dfn") || filePath.EndsWith(".qu"))
            {
                replaceInFile(filePath, "$nrdotablemodule$:", getModuleName(projectItem.ContainingProject.FileName, filePath));
            }
            itemsAdded.Add(projectItem);
        }

        private void replace<TValue>(Dictionary<string, TValue> values, string fromName, string toName, Func<TValue, TValue> func)
        {
            if (values.ContainsKey("$" + fromName + "$"))
            {
                values["$" + toName + "$"] = func(values["$" + fromName + "$"]);
            }
        }

        private void replaceInFile(string path, string oldValue, string newValue)
        {
            var text = File.ReadAllText(path);
            File.WriteAllText(path, text.Replace(oldValue, newValue));
        }

        private string getModuleName(string projectFile, string dfnFile)
        {
            var nrdoFile = Regex.Replace(projectFile, "\\.csproj$", ".nrdo", RegexOptions.IgnoreCase);
            var projFileParts = projectFile.Split('\\').ToList();
            var dfnFileParts = dfnFile.Split('\\').Skip(projFileParts.Count - 1).ToList();
            dfnFileParts.RemoveAt(dfnFileParts.Count - 1);
            if (File.Exists(nrdoFile))
            {
                foreach (var line in File.ReadAllLines(nrdoFile))
                {
                    Match match = Regex.Match(line, "^\\s*module\\s+([^;]+)\\s*;");
                    if (match.Success)
                    {
                        dfnFileParts.InsertRange(0, match.Groups[1].Value.Split(':'));
                    }
                }
            }

            if (dfnFileParts.Count == 0) return "";

            return string.Join(":", (from part in dfnFileParts select nrdoMangleModule(part)).ToArray()) + ":";
        }

        public void RunFinished()
        {
            ProjectItem parent = null;
            ProjectItem child = null;
            foreach (var item in itemsAdded)
            {
                if (item.get_FileNames(1).EndsWith(".dfn") || item.get_FileNames(1).EndsWith(".qu")) parent = item;
                if (item.get_FileNames(1).EndsWith(".dfn.cs") || item.get_FileNames(1).EndsWith(".qu.cs")) child = item;
            }
            if (parent != null && child != null)
            {
                parent.ProjectItems.AddFromFileCopy(child.get_FileNames(1));
            }
            itemsAdded = null;
        }

        private string nrdoMangleModule(string value)
        {
            if (value.Substring(1) == value.Substring(1).ToLower())
            {
                return value.ToLower();
            }
            else
            {
                return value;
            }
        }

        private string toTitleCase(string value)
        {
            return char.ToUpper(value[0]) + value.Substring(1);
        }

        private string nrdoMangleFileToClass(string filename)
        {
            return string.Join("", (from part in Path.GetFileNameWithoutExtension(filename).Split('_') select toTitleCase(part)).ToArray());
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            replace(replacementsDictionary, "safeprojectname", "lcprojectname", value => value.ToLower());
            replace(replacementsDictionary, "safeprojectname", "nrdomodulename", nrdoMangleModule);
            replace(replacementsDictionary, "rootname", "nrdotableclass", nrdoMangleFileToClass);
            itemsAdded = new List<ProjectItem>();
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
    }
}
