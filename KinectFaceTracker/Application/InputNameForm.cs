using System;
using System.Windows.Forms;

namespace App
{
    public sealed partial class InputNameForm : Form
    {
        public string UserName { get; private set; }

        public InputNameForm(string caption = null, string text = null)
        {
            InitializeComponent();
            Text = caption ?? Text;
            label1.Text = text ?? label1.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UserName = textBox1.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
