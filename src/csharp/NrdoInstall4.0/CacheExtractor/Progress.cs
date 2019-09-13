using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NR.nrdo.Install
{
    public static class Progress
    {
        public static void SetLogging(string logFile)
        {
            Reported += message => File.AppendAllText(logFile, DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss") + ": " + message + "\r\n");
            Completed += message => File.AppendAllText(logFile, DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss") + ": " + message + "\r\n");
            Failed += (message, err) =>
            {
                File.AppendAllText(logFile, DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss") + ": Error: " + message + "\r\n");
                if (err != null)
                {
                    File.AppendAllText(logFile, err.GetType().FullName + ": " + err.Message + "\r\n" + err.StackTrace + "\r\n");
                }
                File.AppendAllText(logFile, "Failed!\r\n");
            };
        }

        private static int total;
        public static int Total { get { return total; } set { total = value; if (current > total) current = total; changed(); } }

        private static int current;
        public static int Current { get { return current; } set { current = value; if (current > total) total = current; changed(); } }

        public static void Report(string message)
        {
            ProgressReportHandler handler = Reported;
            if (handler != null) handler(message);
        }
        private static void changed()
        {
            EventHandler handler = Changed;
            if (handler != null) handler(null, EventArgs.Empty);
        }
        public static void Done(string message)
        {
            ProgressReportHandler handler = Completed;
            if (handler != null) handler(message);
        }
        public static void Fail(string message)
        {
            Fail(message, null);
        }
        public static void Fail(string message, Exception err)
        {
            FailedHandler handler = Failed;
            if (handler != null) handler(message, err);
        }

        public static event EventHandler Changed;
        public static event ProgressReportHandler Reported;
        public static event ProgressReportHandler Completed;
        public static event FailedHandler Failed;
    }
    public delegate void ProgressReportHandler(string message);
    public delegate void FailedHandler(string message, Exception err);
}
