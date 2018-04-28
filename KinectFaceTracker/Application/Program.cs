using System;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using App.FSDKTracked;
using Kinect_Test;

namespace App.KinectTracked
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