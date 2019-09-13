using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using NR.Win7;

namespace NR.nrdo.Install
{
    public partial class MainWindow : Form
    {
        public MainWindow(string connStr, string binBase, string cacheBase, string initialError)
        {
            InitializeComponent();
            Progress.Changed += delegate(object sender, EventArgs e)
            {
                Invoke((ThreadStart)delegate()
                {
                    progressBar1.Maximum = Progress.Total;
                    progressBar1.Value = Progress.Current;
                    this.SetTaskBarProgress(Progress.Current, Progress.Total, ProgressBarState.Normal);
                });
            };
            Progress.Completed += delegate(string message) { Environment.Exit(0); };
            Progress.Reported += delegate(string message)
            {
                Invoke((ThreadStart)delegate()
                {
                    progressReport.AppendText(message + "\r\n");
                });
            };
            Progress.Failed += delegate(string message, Exception ex)
            {
                Invoke((ThreadStart)delegate()
                {
                    this.SetTaskBarProgress(Progress.Current, Progress.Total, ProgressBarState.Error);
                    progressBar1.SetErrorState(ProgressBarState.Error);
                    progressReport.AppendText(message + "\r\n");
                    if (ex != null) progressReport.AppendText(ex.Message + ": \r\n" + ex.StackTrace);
                    progressReport.BackColor = Color.Red;
                    progressReport.Visible = true;
                    Height = 298;
                    showLogButton.Text = "<<< Hide Log";
                    ControlBox = true;
                });
            };

            Load += (sender, e) => new Thread(() => RunInstall.Run(connStr, binBase, cacheBase, initialError)).Start();
        }

        // In this project it's only possible to close the window when the ControlBox is re-enabled after a failure. In
        // that case we should exit with a nonzero exit code.
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(1);
        }

        private void showLogButton_Click(object sender, EventArgs e)
        {
            if (progressReport.Visible)
            {
                progressReport.Visible = false;
                Height = 79;
                showLogButton.Text = "Show Log >>>";
            }
            else
            {
                progressReport.Visible = true;
                Height = 298;
                showLogButton.Text = "<<< Hide Log";
            }
        }
    }
}