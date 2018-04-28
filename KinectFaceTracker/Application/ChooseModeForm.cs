using System;
using System.Windows.Forms;
using App.FSDKTracked;
using App.KinectTracked;

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
                chosenForm = null; // TODO
            }

            if (trackedRadioButton.Checked)
            {
                chosenForm = new Form1();
            }

            Hide();
            chosenForm?.ShowDialog();
            Close();
        }
    }
}
