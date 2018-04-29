using System;
using System.Windows.Forms;

namespace App
{
    public static class Program
    {
        [MTAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChooseModeForm());
        }
    }
}