using System;
using KinectFactory;
using System.Windows.Forms;
using Core.Face;
using Core.Kinect;
using FsdkFaceLib;

namespace App.Untracked
{
    public partial class FormUntracked : Form
    {
        private const string FileFilter = "Xml files|*.xml|All files|*.*";
        private IKinect _kinect;
        private IColorFrameStream _colorFrameStream;
        private IFaceDatabase<byte[]> _faceDb = new DictionaryFaceDatabase<byte[]>();
        private byte[] _colorBuffer;
        private Renderer _renderer;

        public FormUntracked()
        {
            InitializeComponent();
            FSDKFacePipeline.InitializeLibrary();
            _kinect = KinectInitializeHelper.InitializeKinect();
            _colorFrameStream = _kinect.ColorFrameStream;
            _colorFrameStream.ColorFrameReady += ColorFrameStreamOnColorFrameReady;

            _renderer = new Renderer(_colorFrameStream.FrameWidth, _colorFrameStream.FrameHeight);
        }

        private void ColorFrameStreamOnColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            using (var colorFrame = e.ColorFrame)
            {
                if (_colorBuffer.Length != colorFrame.PixelDataLength)
                {
                    _colorBuffer = new byte[colorFrame.PixelDataLength];
                }

                colorFrame.CopyFramePixelDataToArray(_colorBuffer);
            }
        }

        private void MainPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StartStopSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void FormMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MainPictureBox_SizeChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
