﻿using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;
using Core;
using Core.Face;
using Core.Kinect;
using FsdkFaceLib;
using FsdkFaceLib.Properties;
using Luxand;

namespace App.Untracked
{
    public partial class FormUntracked : Form
    {
        private readonly IKinect _kinect;
        private readonly IColorFrameStream _colorFrameStream;
        private readonly DatabaseHelper<byte[]> _databaseHelper = new DatabaseHelper<byte[]>(new DictionaryFaceDatabase<byte[]>(new FSDKFaceInfo()));
        private byte[] _previousColorBuffer;
        private byte[] _colorBuffer;
        private ProgramState _programState;
        private readonly Renderer _renderer;
        private (Rectangle Rectangle, IFaceInfo<byte[]> FaceInfo)[] _faces;
        private readonly FpsCounter _fpsCounter = new FpsCounter();

        public FormUntracked()
        {
            InitializeComponent();
            FSDKFacePipeline.InitializeLibrary();
            _kinect = KinectInitializeHelper.InitializeKinect();
            _colorFrameStream = _kinect.ColorFrameStream;
            _colorFrameStream.ColorFrameReady += ColorFrameStreamOnColorFrameReady;

            _renderer = new Renderer(_colorFrameStream.FrameWidth, _colorFrameStream.FrameHeight);

            _kinect.Open();
            _colorFrameStream.Open(true);
            _programState = ProgramState.Running;
        }

        private void ColorFrameStreamOnColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            using (var colorFrame = e.ColorFrame)
            {
                if (colorFrame == null) return;

                if (_colorBuffer?.Length != colorFrame.PixelDataLength)
                {
                    _colorBuffer = new byte[colorFrame.PixelDataLength];
                }

                var copyBufferTask = Task.Run(() => colorFrame.CopyFramePixelDataToArray(_colorBuffer));

                if (_previousColorBuffer == null)
                {
                    copyBufferTask.Wait();
                    _previousColorBuffer = _colorBuffer;
                    _colorBuffer = null;
                    return;
                }

                var previousImage = new ImageBuffer(_previousColorBuffer, colorFrame.Width, colorFrame.Height, colorFrame.BytesPerPixel);

                var bitmapTask = Task.Run(() =>
                {
                    var bitmap =  previousImage.ToBitmap();
                    _renderer.Image = bitmap;
                });

                _fpsCounter.NewFrame();
                statusLabel.Text = $"{_fpsCounter.Fps :F2} FPS";

                previousImage.CreateFsdkImageHandle(out int fsdkImageHandle);
                int detectedFaceCount = 0;
                var err = FSDK.DetectMultipleFaces(fsdkImageHandle, ref detectedFaceCount, out var detectedFaces, 4096);
                Task recTask;

                if (err == FSDK.FSDKE_OK && detectedFaces != null)
                {
                    recTask = RecognizeFaces(detectedFaceCount, detectedFaces, fsdkImageHandle, previousImage);
                }
                else
                {
                    recTask = Task.FromResult<string[]>(null);
                }

                var renderTask = Task.Factory.ContinueWhenAll(new[] {bitmapTask, recTask}, tasks =>
                {
                    var names = ((Task<string[]>) recTask).Result;

                    for (int i = 0; i < detectedFaceCount; i++)
                    {
                        if (names[i] != null)
                        {
                            _renderer.DrawRectangle(_faces[i].Rectangle, 0);
                            _renderer.DrawName(names[i], _faces[i].Rectangle.Left, _faces[i].Rectangle.Bottom, 0);
                        }
                        else
                        {
                            _renderer.DrawRectangle(_faces[i].Rectangle, 1);
                        }
                    }
                });

                Application.DoEvents();
                copyBufferTask.Wait();
                renderTask.Wait();
                mainPictureBox.Image = _renderer.Image;
                var temp = _colorBuffer;
                _colorBuffer = _previousColorBuffer;
                _previousColorBuffer = temp;
                FSDK.FreeImage(fsdkImageHandle);
            }
        }

        private Task<string[]> RecognizeFaces(int detectedFaceCount, FSDK.TFacePosition[] detectedFaces, int fsdkImageHandle,
            ImageBuffer previousImage)
        {
            return Task.Run(() =>
            {
                _faces = new(Rectangle, IFaceInfo<byte[]>)[detectedFaceCount];
                var names = new string[detectedFaceCount];

                Parallel.For(0, detectedFaces.Length, i =>
                {
                    var facePosition = detectedFaces[i];
                    if (FSDK.FSDKE_OK !=
                        FSDK.GetFaceTemplateInRegion(fsdkImageHandle, ref facePosition, out var template))
                    {
                        return;
                    }

                    var faceRect = facePosition.ToRectangle();
                    _faces[i].Rectangle = faceRect;


                    var bestMatch = _databaseHelper.FaceDatabase.GetBestMatch(template);
                    if (bestMatch == null || !bestMatch.IsValid)
                        return;


                    if (bestMatch.Similarity > FsdkSettings.Default.NewTemplateThreshold)
                    {
                        if (bestMatch.Similarity < FsdkSettings.Default.InstantMatchThreshold)
                        {
                            var faceImageBuffer = previousImage.GetRectangle(faceRect);
                            bestMatch.FaceInfo.AddTemplate(template, faceImageBuffer);
                        }

                        names[i] = bestMatch.FaceInfo.Name ?? $"ID: {bestMatch.FaceId}";
                        _faces[i].FaceInfo = bestMatch.FaceInfo;
                    }
                });

                return names;
            });

            
        }

        private void MainPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if(_faces == null)
                return;

            var clickPos = e.Location.Rescale(mainPictureBox.Width, mainPictureBox.Height, _colorFrameStream.FrameWidth,
                _colorFrameStream.FrameHeight);

            foreach (var face in _faces)
            {
                if (face.Rectangle.Contains(clickPos))
                {
                    switch (e.Button)
                    {
                        // TODO
                        case MouseButtons.Left:
                            break;
                        case MouseButtons.Right:
                            break;
                    }
                }
            }

        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _databaseHelper.SaveAs();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var origState = _programState;
            Pause();

            _databaseHelper.Open();

            if(origState == ProgramState.Running) Unpause();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var origState = _programState;
            Pause();

            _databaseHelper.Save();

            if(origState == ProgramState.Running) Unpause();
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _databaseHelper.FaceDatabase.Clear();
        }

        private void StartStopSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TogglePause();
        }

        private void Pause()
        {
            if (_programState != ProgramState.Paused)
            {
                _kinect.Close();
                _programState = ProgramState.Paused;
            }
        }

        private void Unpause()
        {
            if (_programState != ProgramState.Running)
            {
                _kinect.Open();
                _programState = ProgramState.Running;
            }
        }

        private void TogglePause()
        {
            if (_programState == ProgramState.Running)
            {
                Pause();
            }
            else if (_programState == ProgramState.Paused)
            {
                Unpause();
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Pause();
            _databaseHelper.SaveBeforeClose();
        }

        private void FormMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                TogglePause();
            }
        }

        private enum ProgramState
        {
            Running, Paused
        }
    }
}
