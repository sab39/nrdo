using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

namespace NR.nrdo.Install
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string connStr = null;
            string binBase = null;
            string cacheBase = null;
            var silent = false;
            string error = null;

            // Commandline arguments available:
            // "/s" - no parameter - silent mode
            // "/log logfile" - logs to logfile
            // binbase - path to bin folder - presumed to be the first parameter that is is neither part of /s nor /log
            // cachebase - path to nrdo-cache folder to create - presumed to be the second parameter that is neither part of /s nor /log
            // binbase defaults to "bin" and cachebase defaults to "..\nrdo-cache" but you can't specify one without the other
            var args = Environment.GetCommandLineArgs();
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i].ToLower() == "/s")
                {
                    silent = true;
                }
                else if (args[i].ToLower() == "/log")
                {
                    i++;
                    if (i < args.Length)
                    {
                        Progress.SetLogging(args[i]);
                    }
                    else
                    {
                        error = "Must specify log filename";
                    }
                }
                else
                {
                    if (connStr == null)
                    {
                        connStr = args[i];
                    }
                    else if (binBase == null)
                    {
                        binBase = args[i];
                    }
                    else if (cacheBase == null)
                    {
                        cacheBase = args[i];
                    }
                    else
                    {
                        error = "Unknown parameter: " + args[i];
                    }
                }
            }
            if (cacheBase == null)
            {
                error = "Must specify connection string, bin path and cache path on the commandline";
            }

            if (error != null)
            {
                error += "\r\nUsage: NrdoInstall [/s] [/log logfile] connectionstring binpath cachepath";
            }

            if (silent)
            {
                Progress.Failed += (message, ex) => Environment.Exit(1);
                Progress.Completed += message => Environment.Exit(0);
                RunInstall.Run(connStr, binBase, cacheBase, error);
            }
            else
            {
                Application.Run(new MainWindow(connStr, binBase, cacheBase, error));
            }
        }
    }
}