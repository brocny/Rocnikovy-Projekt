namespace App
{
    partial class ChooseModeForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChooseModeForm));
            this.startButton = new System.Windows.Forms.Button();
            this.kinectRadioButton = new System.Windows.Forms.RadioButton();
            this.videoRadioButton = new System.Windows.Forms.RadioButton();
            this.trackedRadioButton = new System.Windows.Forms.RadioButton();
            this.exitButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(443, 58);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(86, 31);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // kinectRadioButton
            // 
            this.kinectRadioButton.AutoSize = true;
            this.kinectRadioButton.Checked = true;
            this.kinectRadioButton.Location = new System.Drawing.Point(12, 12);
            this.kinectRadioButton.Name = "kinectRadioButton";
            this.kinectRadioButton.Size = new System.Drawing.Size(133, 21);
            this.kinectRadioButton.TabIndex = 2;
            this.kinectRadioButton.TabStop = true;
            this.kinectRadioButton.Text = "Kinect - assisted";
            this.kinectRadioButton.UseVisualStyleBackColor = true;
            // 
            // videoRadioButton
            // 
            this.videoRadioButton.AutoSize = true;
            this.videoRadioButton.Location = new System.Drawing.Point(347, 12);
            this.videoRadioButton.Name = "videoRadioButton";
            this.videoRadioButton.Size = new System.Drawing.Size(182, 21);
            this.videoRadioButton.TabIndex = 3;
            this.videoRadioButton.TabStop = true;
            this.videoRadioButton.Text = "Video Only (no tracking)";
            this.videoRadioButton.UseVisualStyleBackColor = true;
            // 
            // trackedRadioButton
            // 
            this.trackedRadioButton.AutoSize = true;
            this.trackedRadioButton.Location = new System.Drawing.Point(151, 12);
            this.trackedRadioButton.Name = "trackedRadioButton";
            this.trackedRadioButton.Size = new System.Drawing.Size(190, 21);
            this.trackedRadioButton.TabIndex = 4;
            this.trackedRadioButton.TabStop = true;
            this.trackedRadioButton.Text = "Video Only (with tracking)";
            this.trackedRadioButton.UseVisualStyleBackColor = true;
            // 
            // exitButton
            // 
            this.exitButton.Location = new System.Drawing.Point(12, 58);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(86, 31);
            this.exitButton.TabIndex = 5;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // ChooseModeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(544, 101);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.trackedRadioButton);
            this.Controls.Add(this.videoRadioButton);
            this.Controls.Add(this.kinectRadioButton);
            this.Controls.Add(this.startButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ChooseModeForm";
            this.Text = "Select Program Mode";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.RadioButton kinectRadioButton;
        private System.Windows.Forms.RadioButton videoRadioButton;
        private System.Windows.Forms.RadioButton trackedRadioButton;
        private System.Windows.Forms.Button exitButton;
    }
}