using System;
using System.Windows.Forms;
using App.FSDKTracked;
using App.Main;
using App.Untracked;

namespace App
{
    public partial class ChooseModeForm : Form
    {
        public ChooseModeForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form chosenForm = null;

            if (kinectRadioButton.Checked)
            {
                chosenForm = new FormMain();
            }

            if (videoRadioButton.Checked)
            {
                chosenForm = new FormUntracked();
            }

            if (trackedRadioButton.Checked)
            {
                chosenForm = new FormFSDKTracked();
            }

            Hide();
            chosenForm?.ShowDialog();
            Close();
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
