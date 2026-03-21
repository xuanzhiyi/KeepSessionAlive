namespace KeepSessionAlive
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.textBox1          = new System.Windows.Forms.TextBox();
            this.dataGridView1     = new System.Windows.Forms.DataGridView();
            this.colApp            = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTime           = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.labelWorkTitle    = new System.Windows.Forms.Label();
            this.labelWorkTime     = new System.Windows.Forms.Label();
            this.labelIdleTitle    = new System.Windows.Forms.Label();
            this.labelIdleTime     = new System.Windows.Forms.Label();
            this.trayContextMenu   = new System.Windows.Forms.ContextMenuStrip();
            this.trayMenuRestore   = new System.Windows.Forms.ToolStripMenuItem();
            this.trayMenuExit      = new System.Windows.Forms.ToolStripMenuItem();
            this.notifyIcon1       = new System.Windows.Forms.NotifyIcon();
            this.panelTitleBar     = new System.Windows.Forms.Panel();
            this.labelTitle        = new System.Windows.Forms.Label();
            this.btnClose          = new System.Windows.Forms.Button();
            this.btnMinimize       = new System.Windows.Forms.Button();
            this.statusStrip1      = new System.Windows.Forms.StatusStrip();
            this.statusLog         = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusLock        = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.trayContextMenu.SuspendLayout();
            this.panelTitleBar.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // panelTitleBar — custom title bar (draggable)
            //
            this.panelTitleBar.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            this.panelTitleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTitleBar.Height = 35;
            this.panelTitleBar.Name = "panelTitleBar";
            this.panelTitleBar.Controls.Add(this.labelTitle);
            this.panelTitleBar.Controls.Add(this.btnMinimize);
            this.panelTitleBar.Controls.Add(this.btnClose);
            this.panelTitleBar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseDown);
            this.panelTitleBar.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseMove);
            this.panelTitleBar.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseUp);
            //
            // labelTitle
            //
            this.labelTitle.Text = "Keep Session Alive";
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = System.Drawing.Color.FromArgb(255, 140, 0);
            this.labelTitle.Location = new System.Drawing.Point(10, 0);
            this.labelTitle.Size = new System.Drawing.Size(320, 35);
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseDown);
            this.labelTitle.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseMove);
            this.labelTitle.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseUp);
            //
            // btnMinimize
            //
            this.btnMinimize.Text = "\u2500";
            this.btnMinimize.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnMinimize.Size = new System.Drawing.Size(45, 35);
            this.btnMinimize.Location = new System.Drawing.Point(405, 0);
            this.btnMinimize.Name = "btnMinimize";
            this.btnMinimize.TabStop = false;
            this.btnMinimize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMinimize.FlatAppearance.BorderSize = 0;
            this.btnMinimize.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            this.btnMinimize.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.btnMinimize.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnMinimize.Click += new System.EventHandler(this.BtnMinimize_Click);
            //
            // btnClose
            //
            this.btnClose.Text = "\u2715";
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnClose.Size = new System.Drawing.Size(45, 35);
            this.btnClose.Location = new System.Drawing.Point(450, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.TabStop = false;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            this.btnClose.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(180, 30, 30);
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            //
            // dataGridView1
            //
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.colApp, this.colTime });
            this.dataGridView1.Location = new System.Drawing.Point(12, 41);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(471, 220);
            this.dataGridView1.TabIndex = 0;
            //
            // colApp
            //
            this.colApp.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colApp.HeaderText = "Application";
            this.colApp.Name = "colApp";
            this.colApp.ReadOnly = true;
            //
            // colTime
            //
            this.colTime.HeaderText = "Time";
            this.colTime.Name = "colTime";
            this.colTime.ReadOnly = true;
            this.colTime.Width = 90;
            //
            // labelWorkTitle
            //
            this.labelWorkTitle.Location = new System.Drawing.Point(12, 267);
            this.labelWorkTitle.Name = "labelWorkTitle";
            this.labelWorkTitle.Size = new System.Drawing.Size(230, 18);
            this.labelWorkTitle.Text = "Working Time";
            this.labelWorkTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // labelWorkTime
            //
            this.labelWorkTime.Location = new System.Drawing.Point(12, 285);
            this.labelWorkTime.Name = "labelWorkTime";
            this.labelWorkTime.Size = new System.Drawing.Size(230, 85);
            this.labelWorkTime.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold);
            this.labelWorkTime.ForeColor = System.Drawing.Color.Green;
            this.labelWorkTime.Text = "0:00:00";
            this.labelWorkTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // labelIdleTitle
            //
            this.labelIdleTitle.Location = new System.Drawing.Point(253, 267);
            this.labelIdleTitle.Name = "labelIdleTitle";
            this.labelIdleTitle.Size = new System.Drawing.Size(230, 18);
            this.labelIdleTitle.Text = "Idle Time";
            this.labelIdleTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // labelIdleTime
            //
            this.labelIdleTime.Location = new System.Drawing.Point(253, 285);
            this.labelIdleTime.Name = "labelIdleTime";
            this.labelIdleTime.Size = new System.Drawing.Size(230, 85);
            this.labelIdleTime.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold);
            this.labelIdleTime.ForeColor = System.Drawing.Color.Goldenrod;
            this.labelIdleTime.Text = "0:00:00";
            this.labelIdleTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // textBox1 — hidden by default, below time labels, toggled by status bar
            //
            this.textBox1.Location = new System.Drawing.Point(12, 382);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(471, 110);
            this.textBox1.TabIndex = 3;
            this.textBox1.Visible = false;
            //
            // statusStrip1
            //
            this.statusOnline       = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusRecord       = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSpacer       = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.statusOnline,
                this.statusLog,
                this.statusLock,
                this.statusSpacer,
                this.statusRecord });
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(495, 40);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.ShowItemToolTips = true;
            //
            // statusOnline — FontAwesome power icon toggle
            //
            this.statusOnline.Name = "statusOnline";
            this.statusOnline.Text = "\uf011";
            this.statusOnline.Font = MainForm.FaFont(14F);
            this.statusOnline.ForeColor = System.Drawing.Color.Gray;
            this.statusOnline.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.statusOnline.ToolTipText = "Keep Online: Off";
            this.statusOnline.IsLink = true;
            this.statusOnline.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.statusOnline.Click += new System.EventHandler(this.statusOnline_Click);
            //
            // statusLog
            //
            this.statusLog.Name = "statusLog";
            this.statusLog.Text = "\uf15c";
            this.statusLog.Font = MainForm.FaFont(14F);
            this.statusLog.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.statusLog.ToolTipText = "Toggle Log";
            this.statusLog.IsLink = true;
            this.statusLog.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.statusLog.Click += new System.EventHandler(this.statusLog_Click);
            //
            // statusLock
            //
            this.statusLock.Name = "statusLock";
            this.statusLock.Text = "\uf023";
            this.statusLock.Font = MainForm.FaFont(14F);
            this.statusLock.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.statusLock.ToolTipText = "Lock Screen";
            this.statusLock.IsLink = true;
            this.statusLock.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.statusLock.Click += new System.EventHandler(this.statusLock_Click);
            //
            // statusSpacer — pushes record to the right
            //
            this.statusSpacer.Name = "statusSpacer";
            this.statusSpacer.Spring = true;
            this.statusSpacer.Text = "";
            //
            // statusRecord
            //
            this.statusRecord.Name = "statusRecord";
            this.statusRecord.Text = "\uf03d";
            this.statusRecord.Font = MainForm.FaFont(14F);
            this.statusRecord.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.statusRecord.ToolTipText = "Start Recording";
            this.statusRecord.IsLink = true;
            this.statusRecord.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.statusRecord.Click += new System.EventHandler(this.statusRecord_Click);
            //
            // trayContextMenu
            //
            this.trayContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.trayMenuRestore,
                this.trayMenuExit });
            this.trayContextMenu.Name = "trayContextMenu";
            this.trayContextMenu.Size = new System.Drawing.Size(114, 48);
            //
            // trayMenuRestore
            //
            this.trayMenuRestore.Name = "trayMenuRestore";
            this.trayMenuRestore.Size = new System.Drawing.Size(113, 22);
            this.trayMenuRestore.Text = "Restore";
            this.trayMenuRestore.Click += new System.EventHandler(this.TrayMenuRestore_Click);
            //
            // trayMenuExit
            //
            this.trayMenuExit.Name = "trayMenuExit";
            this.trayMenuExit.Size = new System.Drawing.Size(113, 22);
            this.trayMenuExit.Text = "Exit";
            this.trayMenuExit.Click += new System.EventHandler(this.TrayMenuExit_Click);
            //
            // notifyIcon1
            //
            this.notifyIcon1.ContextMenuStrip = this.trayContextMenu;
            this.notifyIcon1.Text = "Keep Session Alive";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.NotifyIcon1_DoubleClick);
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(495, 418);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Controls.Add(this.labelIdleTime);
            this.Controls.Add(this.labelIdleTitle);
            this.Controls.Add(this.labelWorkTime);
            this.Controls.Add(this.labelWorkTitle);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.panelTitleBar);
            this.Controls.Add(this.statusStrip1);
            this.MinimumSize = new System.Drawing.Size(495, 418);
            this.Name = "Form1";
            this.Text = "Keep Session Alive";
            this.Resize += new System.EventHandler(this.Form1_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.trayContextMenu.ResumeLayout(false);
            this.panelTitleBar.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.Label labelWorkTitle;
        private System.Windows.Forms.Label labelWorkTime;
        private System.Windows.Forms.Label labelIdleTitle;
        private System.Windows.Forms.Label labelIdleTime;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip trayContextMenu;
        private System.Windows.Forms.ToolStripMenuItem trayMenuRestore;
        private System.Windows.Forms.ToolStripMenuItem trayMenuExit;
        private System.Windows.Forms.Panel panelTitleBar;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnMinimize;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLog;
        private System.Windows.Forms.ToolStripStatusLabel statusLock;
        private System.Windows.Forms.ToolStripStatusLabel statusOnline;
        private System.Windows.Forms.ToolStripStatusLabel statusSpacer;
        private System.Windows.Forms.ToolStripStatusLabel statusRecord;
    }
}
