namespace KeepSessionAlive
{
    partial class Form1
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
            this.button1           = new System.Windows.Forms.Button();
            this.buttonLog         = new System.Windows.Forms.Button();
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
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.trayContextMenu.SuspendLayout();
            this.SuspendLayout();
            //
            // button1
            //
            this.button1.Location = new System.Drawing.Point(12, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(140, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Always online : Off";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // buttonLog
            //
            this.buttonLog.Location = new System.Drawing.Point(158, 12);
            this.buttonLog.Name = "buttonLog";
            this.buttonLog.Size = new System.Drawing.Size(75, 23);
            this.buttonLog.TabIndex = 1;
            this.buttonLog.Text = "Log";
            this.buttonLog.UseVisualStyleBackColor = true;
            this.buttonLog.Click += new System.EventHandler(this.buttonLog_Click);
            //
            // textBox1 — hidden by default, toggled by buttonLog
            //
            this.textBox1.Location = new System.Drawing.Point(12, 41);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(471, 110);
            this.textBox1.TabIndex = 2;
            this.textBox1.Visible = false;
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
            this.dataGridView1.Size = new System.Drawing.Size(471, 185);
            this.dataGridView1.TabIndex = 3;
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
            // labelWorkTitle — left side
            //
            this.labelWorkTitle.Location = new System.Drawing.Point(12, 232);
            this.labelWorkTitle.Name = "labelWorkTitle";
            this.labelWorkTitle.Size = new System.Drawing.Size(230, 18);
            this.labelWorkTitle.Text = "Working Time";
            this.labelWorkTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // labelWorkTime — left side, green
            //
            this.labelWorkTime.Location = new System.Drawing.Point(12, 250);
            this.labelWorkTime.Name = "labelWorkTime";
            this.labelWorkTime.Size = new System.Drawing.Size(230, 85);
            this.labelWorkTime.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold);
            this.labelWorkTime.ForeColor = System.Drawing.Color.Green;
            this.labelWorkTime.Text = "0:00:00";
            this.labelWorkTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // labelIdleTitle — right side
            //
            this.labelIdleTitle.Location = new System.Drawing.Point(253, 232);
            this.labelIdleTitle.Name = "labelIdleTitle";
            this.labelIdleTitle.Size = new System.Drawing.Size(230, 18);
            this.labelIdleTitle.Text = "Idle Time";
            this.labelIdleTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // labelIdleTime — right side, goldenrod
            //
            this.labelIdleTime.Location = new System.Drawing.Point(253, 250);
            this.labelIdleTime.Name = "labelIdleTime";
            this.labelIdleTime.Size = new System.Drawing.Size(230, 85);
            this.labelIdleTime.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold);
            this.labelIdleTime.ForeColor = System.Drawing.Color.Goldenrod;
            this.labelIdleTime.Text = "0:00:00";
            this.labelIdleTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            this.ClientSize = new System.Drawing.Size(495, 348);
            this.Controls.Add(this.labelIdleTime);
            this.Controls.Add(this.labelIdleTitle);
            this.Controls.Add(this.labelWorkTime);
            this.Controls.Add(this.labelWorkTitle);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.buttonLog);
            this.Controls.Add(this.button1);
            this.MinimumSize = new System.Drawing.Size(511, 387);
            this.Name = "Form1";
            this.Text = "Keep Session Alive";
            this.Resize += new System.EventHandler(this.Form1_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.trayContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button buttonLog;
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
    }
}
