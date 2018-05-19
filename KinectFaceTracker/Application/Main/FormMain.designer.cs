namespace App.KinectTracked
{
    partial class FormMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.mainPictureBox = new System.Windows.Forms.PictureBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.focusedFacePictureBox = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startStopSpaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.faceLabel = new System.Windows.Forms.Label();
            this.matchedFacePictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.mainPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.focusedFacePictureBox)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.matchedFacePictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // mainPictureBox
            // 
            this.mainPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainPictureBox.Location = new System.Drawing.Point(0, 32);
            this.mainPictureBox.Name = "mainPictureBox";
            this.mainPictureBox.Size = new System.Drawing.Size(960, 520);
            this.mainPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.mainPictureBox.TabIndex = 0;
            this.mainPictureBox.TabStop = false;
            this.mainPictureBox.SizeChanged += new System.EventHandler(this.MainPictureBox_SizeChanged);
            this.mainPictureBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MainPictureBox_MouseClick);
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(12, 546);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(131, 17);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Waiting for Kinect...";
            // 
            // focusedFacePictureBox
            // 
            this.focusedFacePictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.focusedFacePictureBox.Location = new System.Drawing.Point(978, 32);
            this.focusedFacePictureBox.Name = "focusedFacePictureBox";
            this.focusedFacePictureBox.Size = new System.Drawing.Size(177, 188);
            this.focusedFacePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.focusedFacePictureBox.TabIndex = 4;
            this.focusedFacePictureBox.TabStop = false;
            this.focusedFacePictureBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FocusedFacePictureBox_MouseClick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.startStopSpaceToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1167, 28);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.clearToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            this.saveAsToolStripMenuItem.Text = "Save As";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveToolStripMenuItem_Click);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.ClearToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(222, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // startStopSpaceToolStripMenuItem
            // 
            this.startStopSpaceToolStripMenuItem.Name = "startStopSpaceToolStripMenuItem";
            this.startStopSpaceToolStripMenuItem.Size = new System.Drawing.Size(106, 24);
            this.startStopSpaceToolStripMenuItem.Text = "Start (Space)";
            this.startStopSpaceToolStripMenuItem.Click += new System.EventHandler(this.StartStopSpaceToolStripMenuItem_Click);
            // 
            // faceLabel
            // 
            this.faceLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.faceLabel.AutoSize = true;
            this.faceLabel.Location = new System.Drawing.Point(975, 223);
            this.faceLabel.Name = "faceLabel";
            this.faceLabel.Size = new System.Drawing.Size(0, 17);
            this.faceLabel.TabIndex = 7;
            // 
            // matchedFacePictureBox
            // 
            this.matchedFacePictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.matchedFacePictureBox.Location = new System.Drawing.Point(978, 363);
            this.matchedFacePictureBox.Name = "matchedFacePictureBox";
            this.matchedFacePictureBox.Size = new System.Drawing.Size(177, 189);
            this.matchedFacePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.matchedFacePictureBox.TabIndex = 9;
            this.matchedFacePictureBox.TabStop = false;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1167, 584);
            this.Controls.Add(this.matchedFacePictureBox);
            this.Controls.Add(this.faceLabel);
            this.Controls.Add(this.focusedFacePictureBox);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.mainPictureBox);
            this.Controls.Add(this.menuStrip1);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(490, 340);
            this.Name = "FormMain";
            this.Text = "Kinect Face Tracker";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FormMain_KeyPress);
            ((System.ComponentModel.ISupportInitialize)(this.mainPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.focusedFacePictureBox)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.matchedFacePictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox mainPictureBox;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.PictureBox focusedFacePictureBox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startStopSpaceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label faceLabel;
        private System.Windows.Forms.PictureBox matchedFacePictureBox;
    }
}

