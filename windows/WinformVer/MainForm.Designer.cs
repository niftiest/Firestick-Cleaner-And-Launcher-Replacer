using System.Drawing;
using System.Windows.Forms;

namespace FirestickCleanup
{
    partial class MainForm
    {
        private Label lblTitle;
        private Label lblDeviceIP;
        private TextBox txtDeviceIP;
        private Button btnStart;
        private Button btnRevert;
        private GroupBox grpOptions;
        private CheckBox chkVideoServices;
        private Label lblVideoWarning;
        private CheckBox chkAppstore;
        private CheckBox chkPhotos;
        private CheckBox chkMusic;
        private CheckBox chkFreevee;
        private CheckBox chkMinitv;
        private CheckBox chkGames;
        private CheckBox chkLivetv;
        private CheckBox chkAlexa;
        private CheckBox chkAlexaExtras;
        private CheckBox chkSilk;
        private CheckBox chkSmarthome;
        private CheckBox chkCast;
        private GroupBox grpLog;
        private TextBox txtLog;
        private Label lblStatus;
        private ProgressBar progressBar;

        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.lblDeviceIP = new Label();
            this.txtDeviceIP = new TextBox();
            this.btnStart = new Button();
            this.btnRevert = new Button();
            this.grpOptions = new GroupBox();
            this.chkVideoServices = new CheckBox();
            this.lblVideoWarning = new Label();
            this.chkAppstore = new CheckBox();
            this.chkPhotos = new CheckBox();
            this.chkMusic = new CheckBox();
            this.chkFreevee = new CheckBox();
            this.chkMinitv = new CheckBox();
            this.chkGames = new CheckBox();
            this.chkLivetv = new CheckBox();
            this.chkAlexa = new CheckBox();
            this.chkAlexaExtras = new CheckBox();
            this.chkSilk = new CheckBox();
            this.chkSmarthome = new CheckBox();
            this.chkCast = new CheckBox();
            this.grpLog = new GroupBox();
            this.txtLog = new TextBox();
            this.lblStatus = new Label();
            this.progressBar = new ProgressBar();

            this.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.grpLog.SuspendLayout();

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new Point(12, 10);
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTitle.Text = "Firestick Cleanup Tool - Projectivy Launcher + Debloat";

            // lblDeviceIP
            this.lblDeviceIP.AutoSize = true;
            this.lblDeviceIP.Location = new Point(14, 50);
            this.lblDeviceIP.Text = "Fire TV IP:";

            // txtDeviceIP
            this.txtDeviceIP.Location = new Point(85, 47);
            this.txtDeviceIP.Size = new Size(150, 23);

            // btnStart
            this.btnStart.Location = new Point(250, 45);
            this.btnStart.Size = new Size(140, 28);
            this.btnStart.Text = "Start Cleanup";
            this.btnStart.Click += new System.EventHandler(this.BtnStart_Click);

            // btnRevert
            this.btnRevert.Location = new Point(400, 45);
            this.btnRevert.Size = new Size(140, 28);
            this.btnRevert.Text = "Revert Changes";
            this.btnRevert.Click += new System.EventHandler(this.BtnRevert_Click);

            // grpOptions
            this.grpOptions.Location = new Point(12, 85);
            this.grpOptions.Size = new Size(360, 480);
            this.grpOptions.Text = "Optional packages to disable";
            this.grpOptions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            this.grpOptions.Controls.Add(this.chkVideoServices);
            this.grpOptions.Controls.Add(this.lblVideoWarning);
            this.grpOptions.Controls.Add(this.chkAppstore);
            this.grpOptions.Controls.Add(this.chkPhotos);
            this.grpOptions.Controls.Add(this.chkMusic);
            this.grpOptions.Controls.Add(this.chkFreevee);
            this.grpOptions.Controls.Add(this.chkMinitv);
            this.grpOptions.Controls.Add(this.chkGames);
            this.grpOptions.Controls.Add(this.chkLivetv);
            this.grpOptions.Controls.Add(this.chkAlexa);
            this.grpOptions.Controls.Add(this.chkAlexaExtras);
            this.grpOptions.Controls.Add(this.chkSilk);
            this.grpOptions.Controls.Add(this.chkSmarthome);
            this.grpOptions.Controls.Add(this.chkCast);

            int y = 25;
            this.chkVideoServices.Location = new Point(15, y); y += 22;
            this.chkVideoServices.AutoSize = true;
            this.chkVideoServices.Text = "Disable Amazon Video services";
            this.chkVideoServices.Checked = false;

            this.lblVideoWarning.Location = new Point(33, y); y += 36;
            this.lblVideoWarning.Size = new Size(310, 32);
            this.lblVideoWarning.ForeColor = Color.Firebrick;
            this.lblVideoWarning.Text = "Leave UNCHECKED if you use Prime Video, Freevee\nor MiniTV. Disables the Prime Video app too.";

            ConfigureCheck(this.chkAppstore,    "Disable Amazon Appstore", ref y);
            ConfigureCheck(this.chkPhotos,      "Disable Amazon Photos", ref y);
            ConfigureCheck(this.chkMusic,       "Disable Amazon Music", ref y);
            ConfigureCheck(this.chkFreevee,     "Disable Freevee / IMDb TV", ref y);
            ConfigureCheck(this.chkMinitv,      "Disable Amazon MiniTV", ref y);
            ConfigureCheck(this.chkGames,       "Disable Amazon Game Hub", ref y);
            ConfigureCheck(this.chkLivetv,      "Disable Amazon Live TV", ref y);
            ConfigureCheck(this.chkAlexa,       "Disable Alexa alerts/notifications", ref y);
            ConfigureCheck(this.chkAlexaExtras, "Disable Alexa extras (voice search still works)", ref y);
            ConfigureCheck(this.chkSilk,        "Disable Silk Browser", ref y);
            ConfigureCheck(this.chkSmarthome,   "Disable Smart Home features", ref y);
            ConfigureCheck(this.chkCast,        "Disable WhisperPlay (casting)", ref y);

            // grpLog
            this.grpLog.Location = new Point(385, 85);
            this.grpLog.Size = new Size(595, 480);
            this.grpLog.Text = "Log";
            this.grpLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.grpLog.Controls.Add(this.txtLog);

            // txtLog
            this.txtLog.Location = new Point(10, 22);
            this.txtLog.Size = new Size(575, 448);
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.Font = new Font("Consolas", 9F);
            this.txtLog.BackColor = Color.Black;
            this.txtLog.ForeColor = Color.LightGreen;
            this.txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(14, 575);
            this.lblStatus.Text = "Ready.";
            this.lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            // progressBar
            this.progressBar.Location = new Point(14, 600);
            this.progressBar.Size = new Size(966, 22);
            this.progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // MainForm
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(992, 635);
            this.Font = new Font("Segoe UI", 9F);
            this.MinimumSize = new Size(820, 540);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblDeviceIP);
            this.Controls.Add(this.txtDeviceIP);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnRevert);
            this.Controls.Add(this.grpOptions);
            this.Controls.Add(this.grpLog);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Text = "Firestick Cleanup Tool";
            this.StartPosition = FormStartPosition.CenterScreen;

            this.grpOptions.ResumeLayout(false);
            this.grpOptions.PerformLayout();
            this.grpLog.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private static void ConfigureCheck(CheckBox cb, string text, ref int y)
        {
            cb.Location = new Point(15, y);
            cb.AutoSize = true;
            cb.Text = text;
            cb.Checked = true;
            y += 22;
        }
    }
}
