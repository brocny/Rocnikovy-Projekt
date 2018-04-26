namespace Kinect_Test
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
            this.startButton = new System.Windows.Forms.Button();
            this.kinectRadioButton = new System.Windows.Forms.RadioButton();
            this.videoRadioButton = new System.Windows.Forms.RadioButton();
            this.trackedRadioButton = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(329, 40);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(86, 41);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // kinectRadioButton
            // 
            this.kinectRadioButton.AutoSize = true;
            this.kinectRadioButton.Checked = true;
            this.kinectRadioButton.Location = new System.Drawing.Point(13, 13);
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
            this.videoRadioButton.Location = new System.Drawing.Point(152, 12);
            this.videoRadioButton.Name = "videoRadioButton";
            this.videoRadioButton.Size = new System.Drawing.Size(98, 21);
            this.videoRadioButton.TabIndex = 3;
            this.videoRadioButton.TabStop = true;
            this.videoRadioButton.Text = "Video Only";
            this.videoRadioButton.UseVisualStyleBackColor = true;
            // 
            // trackedRadioButton
            // 
            this.trackedRadioButton.AutoSize = true;
            this.trackedRadioButton.Location = new System.Drawing.Point(256, 13);
            this.trackedRadioButton.Name = "trackedRadioButton";
            this.trackedRadioButton.Size = new System.Drawing.Size(159, 21);
            this.trackedRadioButton.TabIndex = 4;
            this.trackedRadioButton.TabStop = true;
            this.trackedRadioButton.Text = "Video Only (tracked)";
            this.trackedRadioButton.UseVisualStyleBackColor = true;
            // 
            // ChooseModeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(441, 93);
            this.Controls.Add(this.trackedRadioButton);
            this.Controls.Add(this.videoRadioButton);
            this.Controls.Add(this.kinectRadioButton);
            this.Controls.Add(this.startButton);
            this.Name = "ChooseModeForm";
            this.Text = "ChooseModeForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.RadioButton kinectRadioButton;
        private System.Windows.Forms.RadioButton videoRadioButton;
        private System.Windows.Forms.RadioButton trackedRadioButton;
    }
}