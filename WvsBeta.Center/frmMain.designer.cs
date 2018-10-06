using WvsBeta.Common;

namespace WvsBeta.Center
{
    partial class frmMain : System.Windows.Forms.Form
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.lvServers = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Population = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.txtTotalConnections = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPingEntries = new System.Windows.Forms.TextBox();
            this.txtLog = new WvsBeta.Common.RollingTextBox();
            this.SuspendLayout();
            // 
            // lvServers
            // 
            this.lvServers.Alignment = System.Windows.Forms.ListViewAlignment.SnapToGrid;
            this.lvServers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lvServers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.Population,
            this.columnHeader3});
            this.lvServers.FullRowSelect = true;
            this.lvServers.GridLines = true;
            this.lvServers.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvServers.HideSelection = false;
            this.lvServers.Location = new System.Drawing.Point(12, 35);
            this.lvServers.Name = "lvServers";
            this.lvServers.ShowItemToolTips = true;
            this.lvServers.Size = new System.Drawing.Size(433, 284);
            this.lvServers.SmallImageList = this.imageList1;
            this.lvServers.TabIndex = 0;
            this.lvServers.TabStop = false;
            this.lvServers.UseCompatibleStateImageBehavior = false;
            this.lvServers.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 117;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "IP";
            this.columnHeader2.Width = 126;
            // 
            // Population
            // 
            this.Population.Text = "Connections";
            this.Population.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Population.Width = 74;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Rate";
            this.columnHeader3.Width = 95;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Red.png");
            this.imageList1.Images.SetKeyName(1, "Green.png");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Total Connections:";
            // 
            // txtTotalConnections
            // 
            this.txtTotalConnections.Location = new System.Drawing.Point(114, 9);
            this.txtTotalConnections.Name = "txtTotalConnections";
            this.txtTotalConnections.ReadOnly = true;
            this.txtTotalConnections.Size = new System.Drawing.Size(97, 20);
            this.txtTotalConnections.TabIndex = 2;
            this.txtTotalConnections.Text = "-1";
            this.txtTotalConnections.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(277, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Ping entries:";
            // 
            // txtPingEntries
            // 
            this.txtPingEntries.Location = new System.Drawing.Point(348, 9);
            this.txtPingEntries.Name = "txtPingEntries";
            this.txtPingEntries.ReadOnly = true;
            this.txtPingEntries.Size = new System.Drawing.Size(97, 20);
            this.txtPingEntries.TabIndex = 5;
            this.txtPingEntries.Text = "-1";
            this.txtPingEntries.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(451, 9);
            this.txtLog.MaxLength = 327670;
            this.txtLog.MaxLines = 15;
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(267, 316);
            this.txtLog.TabIndex = 3;
            this.txtLog.Text = "Running in DEBUG mode.";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 331);
            this.Controls.Add(this.txtPingEntries);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.txtTotalConnections);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lvServers);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.Text = "WvsBeta.Center";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lvServers;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader Population;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTotalConnections;
		private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private RollingTextBox txtLog;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPingEntries;
    }
}

