using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace KeepSessionAlive
{
    public class AdLookupForm : Form
    {
        private TextBox txtSearch;
        private ComboBox cbDomain;
        private Button btnSearch;
        private DataGridView dgv;
        private Label lblStatus;

        public AdLookupForm()
        {
            InitUI();
            ApplyDarkTheme();
        }

        // --- Custom title bar drag ---
        private Point _dragStart;
        private bool _dragging;

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; }
        }
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                var p = PointToScreen(e.Location);
                this.Location = new Point(p.X - _dragStart.X, p.Y - _dragStart.Y);
            }
        }
        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        // --- Resize via bottom-right corner ---
        private const int RESIZE_GRIP = 12;
        private bool _resizing;
        private Point _resizeStart;
        private Size _resizeOrigSize;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && IsInResizeGrip(e.Location))
            {
                _resizing = true;
                _resizeStart = PointToScreen(e.Location);
                _resizeOrigSize = this.Size;
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_resizing)
            {
                var cur = PointToScreen(e.Location);
                int w = _resizeOrigSize.Width + (cur.X - _resizeStart.X);
                int h = _resizeOrigSize.Height + (cur.Y - _resizeStart.Y);
                this.Size = new Size(Math.Max(MinimumSize.Width, w), Math.Max(MinimumSize.Height, h));
            }
            else
            {
                this.Cursor = IsInResizeGrip(e.Location) ? Cursors.SizeNWSE : Cursors.Default;
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _resizing = false;
        }
        private bool IsInResizeGrip(Point p)
        {
            return p.X >= ClientSize.Width - RESIZE_GRIP && p.Y >= ClientSize.Height - RESIZE_GRIP;
        }

        private void InitUI()
        {
            this.Text = "AD Lookup";
            this.Size = new Size(680, 480);
            this.MinimumSize = new Size(500, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };

            // Custom title bar
            var panelTitleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(20, 20, 20)
            };
            panelTitleBar.MouseDown += TitleBar_MouseDown;
            panelTitleBar.MouseMove += TitleBar_MouseMove;
            panelTitleBar.MouseUp += TitleBar_MouseUp;

            var labelTitle = new Label
            {
                Text = "AD Lookup",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 0),
                Location = new Point(10, 0),
                Size = new Size(200, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            labelTitle.MouseDown += TitleBar_MouseDown;
            labelTitle.MouseMove += TitleBar_MouseMove;
            labelTitle.MouseUp += TitleBar_MouseUp;

            var btnClose = new Button
            {
                Text = "\u2715",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(45, 35),
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Dock = DockStyle.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 30, 30);
            btnClose.Click += (s, e) => this.Close();

            panelTitleBar.Controls.Add(labelTitle);
            panelTitleBar.Controls.Add(btnClose);

            // Top panel: search bar
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(8, 8, 8, 4)
            };

            cbDomain = new ComboBox
            {
                Width = 90,
                DropDownStyle = ComboBoxStyle.DropDown,
                Dock = DockStyle.Left,
                Font = new Font("Segoe UI", 10F)
            };
            cbDomain.Items.AddRange(new object[] { "ONEADR", "QAONEADR" });
            cbDomain.SelectedIndex = 0;

            var domainSpacer = new Panel { Dock = DockStyle.Left, Width = 6 };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    DoSearch();
                }
            };

            var btnSpacer = new Panel { Dock = DockStyle.Right, Width = 6 };

            btnSearch = new Button
            {
                Text = "Search",
                Width = 80,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnSearch.Click += (s, e) => DoSearch();

            // Order matters for Dock: right-docked first, then left, then fill
            topPanel.Controls.Add(txtSearch);
            topPanel.Controls.Add(domainSpacer);
            topPanel.Controls.Add(cbDomain);
            topPanel.Controls.Add(btnSpacer);
            topPanel.Controls.Add(btnSearch);

            // Status label
            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Font = new Font("Segoe UI", 8.5F)
            };

            // DataGridView
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9F)
            };
            dgv.CellDoubleClick += Dgv_CellDoubleClick;

            this.Controls.Add(dgv);
            this.Controls.Add(topPanel);
            this.Controls.Add(panelTitleBar);
            this.Controls.Add(lblStatus);
        }

        private void SetupColumnsForGroups()
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Group Name",
                DataPropertyName = "Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
        }

        private void SetupColumnsForUsers()
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                DataPropertyName = "Name",
                Width = 200,
                ReadOnly = true
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Account",
                DataPropertyName = "SamAccountName",
                Width = 150,
                ReadOnly = true
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Email",
                DataPropertyName = "Email",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Enabled",
                DataPropertyName = "Enabled",
                Width = 60,
                ReadOnly = true
            });
        }

        private bool IsGroupName(string keyword)
        {
            var upper = keyword.ToUpperInvariant();
            return upper.StartsWith("SEC-") || upper.StartsWith("DLCOM") || upper.StartsWith("O365");
        }

        private void DoSearch()
        {
            var keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword)) return;

            var domain = cbDomain.Text;

            btnSearch.Enabled = false;
            lblStatus.Text = "Searching...";
            dgv.DataSource = null;
            this.Cursor = Cursors.WaitCursor;

            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                e.Result = IsGroupName(keyword)
                    ? (object)LookupUsersInGroup(keyword, domain)
                    : (object)LookupGroupsOfUser(keyword, domain);
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                this.Cursor = Cursors.Default;
                btnSearch.Enabled = true;

                if (e.Error != null)
                {
                    lblStatus.Text = "Error: " + e.Error.Message;
                    return;
                }

                if (e.Result is string errorMsg)
                {
                    lblStatus.Text = errorMsg;
                    return;
                }

                if (e.Result is List<AdEntry> users)
                {
                    SetupColumnsForUsers();
                    dgv.DataSource = new BindingList<AdEntry>(users);
                    lblStatus.Text = $"{users.Count} user(s) in group \"{keyword}\"";
                    HighlightDisabled();
                }
                else if (e.Result is List<AdGroupEntry> groups)
                {
                    SetupColumnsForGroups();
                    dgv.DataSource = new BindingList<AdGroupEntry>(groups);
                    lblStatus.Text = $"{groups.Count} group(s) for user \"{keyword}\"";
                }
            };
            worker.RunWorkerAsync();
        }

        private object LookupGroupsOfUser(string userId, string domain)
        {
            using (var ctx = new PrincipalContext(ContextType.Domain, domain))
            using (var user = Principal.FindByIdentity(ctx, userId))
            {
                if (user == null) return $"User \"{userId}\" not found.";

                var groups = user.GetGroups();
                return groups
                    .Select(g => new AdGroupEntry { Name = g.Name })
                    .OrderBy(g => g.Name)
                    .ToList();
            }
        }

        private object LookupUsersInGroup(string groupName, string domain)
        {
            using (var ctx = new PrincipalContext(ContextType.Domain, domain))
            using (var group = GroupPrincipal.FindByIdentity(ctx, groupName))
            {
                if (group == null) return $"Group \"{groupName}\" not found.";

                var members = group.GetMembers(true);
                return members
                    .Select(p => new AdEntry
                    {
                        Name = p.Name,
                        SamAccountName = p.SamAccountName,
                        Email = p.UserPrincipalName,
                        Enabled = p.DistinguishedName != null &&
                                  !p.DistinguishedName.Contains("Disabled-Accounts")
                    })
                    .OrderBy(u => u.Name)
                    .ToList();
            }
        }

        private void HighlightDisabled()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.DataBoundItem is AdEntry entry && !entry.Enabled)
                {
                    row.DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
        }

        private void Dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Double-click a group row → search users in that group
            if (dgv.Rows[e.RowIndex].DataBoundItem is AdGroupEntry group)
            {
                txtSearch.Text = group.Name;
                DoSearch();
            }
            // Double-click a user row → search groups of that user
            else if (dgv.Rows[e.RowIndex].DataBoundItem is AdEntry user)
            {
                txtSearch.Text = user.SamAccountName;
                DoSearch();
            }
        }

        private void ApplyDarkTheme()
        {
            var bg      = Color.FromArgb(28, 28, 28);
            var surface = Color.FromArgb(45, 45, 45);
            var border  = Color.FromArgb(70, 70, 70);
            var orange  = Color.FromArgb(255, 140, 0);
            var text    = Color.FromArgb(220, 220, 220);

            this.BackColor = bg;
            this.ForeColor = text;

            txtSearch.BackColor = surface;
            txtSearch.ForeColor = text;
            txtSearch.BorderStyle = BorderStyle.FixedSingle;

            cbDomain.BackColor = surface;
            cbDomain.ForeColor = text;
            cbDomain.FlatStyle = FlatStyle.Flat;

            btnSearch.BackColor = Color.FromArgb(60, 60, 60);
            btnSearch.ForeColor = orange;
            btnSearch.FlatAppearance.BorderColor = border;
            btnSearch.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);

            lblStatus.BackColor = Color.FromArgb(20, 20, 20);
            lblStatus.ForeColor = Color.FromArgb(180, 180, 180);

            dgv.BackgroundColor = bg;
            dgv.GridColor = border;
            dgv.DefaultCellStyle.BackColor = surface;
            dgv.DefaultCellStyle.ForeColor = text;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            dgv.DefaultCellStyle.SelectionForeColor = text;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = text;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = text;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = orange;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 35, 35);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = orange;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        // --- Data classes ---
        public class AdEntry
        {
            public string Name { get; set; }
            public string SamAccountName { get; set; }
            public string Email { get; set; }
            public bool Enabled { get; set; }
        }

        public class AdGroupEntry
        {
            public string Name { get; set; }
        }
    }
}
