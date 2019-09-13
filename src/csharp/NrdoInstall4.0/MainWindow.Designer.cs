namespace NR.nrdo.Install
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.progressReport = new System.Windows.Forms.TextBox();
            this.showLogButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 12);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(750, 23);
            this.progressBar1.TabIndex = 0;
            // 
            // progressReport
            // 
            this.progressReport.Location = new System.Drawing.Point(12, 42);
            this.progressReport.Multiline = true;
            this.progressReport.Name = "progressReport";
            this.progressReport.ReadOnly = true;
            this.progressReport.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.progressReport.Size = new System.Drawing.Size(840, 212);
            this.progressReport.TabIndex = 1;
            this.progressReport.Visible = false;
            // 
            // showLogButton
            // 
            this.showLogButton.Location = new System.Drawing.Point(768, 12);
            this.showLogButton.Name = "showLogButton";
            this.showLogButton.Size = new System.Drawing.Size(84, 23);
            this.showLogButton.TabIndex = 2;
            this.showLogButton.Text = "Show Log >>>";
            this.showLogButton.UseVisualStyleBackColor = true;
            this.showLogButton.Click += new System.EventHandler(this.showLogButton_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(864, 47);
            this.ControlBox = false;
            this.Controls.Add(this.showLogButton);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.progressReport);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainWindow";
            this.Text = "nrdo database installer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox progressReport;
        private System.Windows.Forms.Button showLogButton;
    }
}

