using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Graphics
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
