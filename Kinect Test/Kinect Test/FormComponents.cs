using System.Windows.Forms;

namespace Kinect_Test
{
    public class FormComponents
    {
        public FormComponents(Label label, PictureBox pictureBox)
        {
            PictureBox = pictureBox;
            Label = label;
        }

        public PictureBox PictureBox { get; }
        public Label Label { get; }

    }
}
