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
            this.button1 = new System.Windows.Forms.Button();
            this.buttonLog = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.colApp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.labelIdleTitle = new System.Windows.Forms.Label();
            this.labelIdleTime = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            //
            // button1
            //
            this.button1.Location = new System.Drawing.Point(12, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Start";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // buttonLog
            //
            this.buttonLog.Location = new System.Drawing.Point(95, 12);
            this.buttonLog.Name = "buttonLog";
            this.buttonLog.Size = new System.Drawing.Size(75, 23);
            this.buttonLog.TabIndex = 1;
            this.buttonLog.Text = "Log";
            this.buttonLog.UseVisualStyleBackColor = true;
            this.buttonLog.Click += new System.EventHandler(this.buttonLog_Click);
            //
            // textBox1 — hidden by default, shown when Log is toggled
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
            // labelIdleTitle
            //
            this.labelIdleTitle.Location = new System.Drawing.Point(12, 232);
            this.labelIdleTitle.Name = "labelIdleTitle";
            this.labelIdleTitle.Size = new System.Drawing.Size(471, 18);
            this.labelIdleTitle.Text = "Total Idle Time";
            this.labelIdleTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // labelIdleTime
            //
            this.labelIdleTime.Location = new System.Drawing.Point(12, 250);
            this.labelIdleTime.Name = "labelIdleTime";
            this.labelIdleTime.Size = new System.Drawing.Size(471, 85);
            this.labelIdleTime.Font = new System.Drawing.Font("Segoe UI", 42F, System.Drawing.FontStyle.Bold);
            this.labelIdleTime.Text = "0:00:00";
            this.labelIdleTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(495, 348);
            this.Controls.Add(this.labelIdleTime);
            this.Controls.Add(this.labelIdleTitle);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.buttonLog);
            this.Controls.Add(this.button1);
            this.MinimumSize = new System.Drawing.Size(511, 387);
            this.Name = "Form1";
            this.Text = "Keep Session Alive";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
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
        private System.Windows.Forms.Label labelIdleTitle;
        private System.Windows.Forms.Label labelIdleTime;
    }
}
