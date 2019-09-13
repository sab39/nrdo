using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using NR.Win7;

namespace NR.nrdo.Install
{
    public partial class MainWindow : Form
    {
        private void doInvoke(Action action)
        {
            Invoke(action);
        }

        public MainWindow(string binBase, string cacheBase, string initialError)
        {
            InitializeComponent();
            Progress.Changed += (sender, e) => doInvoke(() =>
                {
                    progressBar1.Maximum = Progress.Total;
                    progressBar1.Value = Progress.Current;
                    this.SetTaskBarProgress(Progress.Current, Progress.Total, ProgressBarState.Normal);
                });
            Progress.Reported += message => doInvoke(() => progressReport.AppendText(message + "\r\n"));
            Progress.Completed += message => Environment.Exit(0);
            Progress.Failed += (message, ex) => doInvoke(() =>
                {
                    this.SetTaskBarProgress(Progress.Current, Progress.Total, ProgressBarState.Error);
                    progressBar1.SetErrorState(ProgressBarState.Error);
                    progressReport.AppendText(message + "\r\n");
                    progressReport.BackColor = Color.Red;
                    setLogVisible(true);
                    ControlBox = true;
                });

            Load += (sender, e) => new Thread(() => RunExtract.Run(binBase, cacheBase, initialError)).Start();
        }

        // In this project it's only possible to close the window when the ControlBox is re-enabled after a failure. In
        // that case we should exit with a nonzero exit code.
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(1);
        }

        private void setLogVisible(bool visible)
        {
            progressReport.Visible = visible;
            if (visible)
            {
                Width = 870;
                Height = 298;
                progressBar1.Width = 750;
                showLogButton.Left = 768;
                showLogButton.Text = "<<< Hide Log";
            }
            else
            {
                Width = 270;
                Height = 79;
                progressBar1.Width = 150;
                showLogButton.Left = 168;
                showLogButton.Text = "Show Log >>>";
            }
        }

        private void showLogButton_Click(object sender, EventArgs e)
        {
            setLogVisible(!progressReport.Visible);
        }
    }
}
