using System.Windows.Forms;
using Core.Kinect;

namespace App
{
    public static class KinectInitializeHelper
    {
        public static IKinect InitializeKinect()
        {
            while (true)
            {
                var kinect = KinectFactory.KinectFactory.GetKinect();
                if (kinect != null)
                    return kinect;

                var result = MessageBox.Show("Error: No Kinect device found!", "Kinect not found",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Error);

                if (result == DialogResult.Cancel) return null;
            }
        }
    }
}
