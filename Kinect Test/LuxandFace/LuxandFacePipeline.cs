using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Face;
using KinectUnifier;
using Luxand;
using LuxandFaceLib;
using System.Threading.Tasks.Dataflow;

namespace LuxandFaceLib
{
    public class LuxandFacePipeline
    {
        public IReadOnlyDictionary<int, int> TrackedFaces => _trackedFaces;

        public void Feed(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper coordinateMapper)
        {
            _faceLocationBlock.Post((colorFrame, bodyFrame, coordinateMapper));
        }

        private TransformBlock<(IColorFrame, IBodyFrame, ICoordinateMapper), FaceLocationResult> _faceLocationBlock;
        private TransformBlock<FaceLocationResult, FaceImage[]> _faceCuttingBlock;
        private TransformBlock<FaceImage[], FSDKFaceImage[]> _fsdkImageCreatingBlock;
        private TransformBlock<FSDKFaceImage[], FSDKFaceImage[]> _faceDetectionBlock;
        private TransformBlock<FSDKFaceImage[], FSDKFaceImage[]> _facialFeaturesBlock;
        private TransformBlock<FSDKFaceImage[], FaceTemplate[]> _templateExtractionBlock;
        private ActionBlock<FaceTemplate[]> _templateProcessingBlock;

        public Action<Task> FaceLocationContinuation { get; set; }
        public Action<Task> FaceCuttingContinuation { get; set; }
        public Action<Task> ImageCreationContinuation { get; set; }
        public Action<Task> FSDKImageCreationContinuation { get; set; }
        public Action<Task> FacialFeaturesContinuation { get; set; }
        public Action<Task> TemplateExtractionContinuation { get; set; }
        public Action<Task> TemplateProcessingContinuation;

        public TaskScheduler SynchContext { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public int FSDKInternalResizeWidth
        {
            get{ return _internalResizeWidth; }
            set
            {
                _internalResizeWidth = value;
                SetFSDKParams();
            }
        }

        public bool FSDKDetermineLocation
        {
            get { return _determineRotAngle; }
            set
            {
                _determineRotAngle = value;
                SetFSDKParams();
            }
        }

        public bool FSDKHandleArbitrayRot
        {
            get { return _handleArbitrayRot; }
            set
            {
                _handleArbitrayRot = value;
                SetFSDKParams();
            }
        }

        public float SameFaceConfidenceThreshold { get; set; } = 0.95f;
        public float SameFaceNewTemplateThreshold { get; set; } = 0.85f;

        public static string ActivationKey { get; set; } =
            @"qkCo6wATHvarVxeIrN1PI/b1aBxCe1GYdqhGmWEQ3VQqEmjgBtGUDBn5oyu9DqUSxsI4YABRzKDYQ/7Y0MCARdMJs7bgxBt7npmXidPq/4qPgC6bzQZ/bzk9VJBtMBQ08c8T6855C5NDnw8L3QybU+Ou0tnmMN3CtM8mhjQCtvQ=";

        public LuxandFacePipeline()
        {
            _faceLocationBlock = new TransformBlock<ValueTuple<IColorFrame, IBodyFrame, ICoordinateMapper>, FaceLocationResult>(_faceLocationFunc);
            _faceCuttingBlock = new TransformBlock<FaceLocationResult, FaceImage[]>(_faceCuttingFunc);
            _fsdkImageCreatingBlock = new TransformBlock<FaceImage[], FSDKFaceImage[]>(_FSDKFaceImageCreationFunc);
            _faceDetectionBlock = new TransformBlock<FSDKFaceImage[], FSDKFaceImage[]>(_faceDetectionFunc);
            _facialFeaturesBlock = new TransformBlock<FSDKFaceImage[], FSDKFaceImage[]>(_facialFeaturesAction);
            _templateExtractionBlock = new TransformBlock<FSDKFaceImage[], FaceTemplate[]>(_templateExtractionFunc);
            _templateProcessingBlock = new ActionBlock<FaceTemplate[]>(_templateProcessingAction);

            _faceLocationBlock.LinkTo(_faceCuttingBlock);
            _faceCuttingBlock.LinkTo(_fsdkImageCreatingBlock);
            _fsdkImageCreatingBlock.LinkTo(_faceDetectionBlock);
            _faceDetectionBlock.LinkTo(_facialFeaturesBlock);
            _facialFeaturesBlock.LinkTo(_templateExtractionBlock);
            _templateExtractionBlock.LinkTo(_templateProcessingBlock);
        }

        private static bool _isLibraryActivated;
        public static void InitializeLibrary()
        {
            if (_isLibraryActivated) return;
            if (FSDK.FSDKE_OK != FSDK.ActivateLibrary(ActivationKey))
            {
                throw new ApplicationException("Invalid Luxand FSDK activation key");
            }
            _isLibraryActivated = true;
        }

        public LuxandFacePipeline(FaceDatabase<byte[]> db, IDictionary<int, int> trackedFaces = null) : this()
        {
            _faceDb = db;
            _trackedFaces = trackedFaces == null ? new Dictionary<int, int>() : new Dictionary<int, int>(trackedFaces);
        }

        private FaceDatabase<byte[]> _faceDb;
        private Dictionary<int, int> _trackedFaces;

        public Task<FaceLocationResult> LocateFacesAsync(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper)
        {
            var retTask = Task.Run(() =>
            {
                var colorTask = Task.Run(() =>
                {
                    var buffer = new byte[colorFrame.PixelDataLength];
                    colorFrame.CopyFramePixelDataToArray(buffer);
                    return buffer;
                });

                var bodyTask = Task.Run(() =>
                {
                    var bodyCount = bodyFrame.BodyCount;
                    var faceRects = new List<Rectangle>(bodyCount);
                    var faceIds = new List<long>(bodyCount);
                    var bodies = new IBody[bodyCount];
                    bodyFrame.CopyBodiesTo(bodies);
                    for (int i = 0; i < bodyCount; i++)
                    {
                        if (Util.TryGetHeadRectangle(bodies[i], mapper, out var faceRect))
                        {
                            faceRects.Add(faceRect);
                            faceIds.Add(bodies[i].TrackingId);
                        }
                    }
                    return (faceRects.ToArray(), faceIds.ToArray(), bodies);
                });

                return new FaceLocationResult
                {
                    BytesPerPixel = colorFrame.BytesPerPixel,
                    ColorBuffer = colorTask.Result,
                    FaceRectangles = bodyTask.Result.Item1,
                    Height = colorFrame.Height,
                    Width = colorFrame.Width,
                    Bodies = bodyTask.Result.Item3
                };
            });

            retTask.ContinueWith(t =>
            {
                if (_faceCuttingBlock.InputCount <= 1)
                {
                   _faceCuttingBlock.Post(t.Result);
                }
            });

            return retTask;
        }

        private Func<(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper), FaceLocationResult> _faceLocationFunc =>
        f =>
        {
            var colorFrame = f.colorFrame;
            var bodyFrame = f.bodyFrame;
            var mapper = f.mapper;
            var colorTask = Task.Run(() =>
            {
                var buffer = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyFramePixelDataToArray(buffer);
                colorFrame.Dispose();
                return buffer;
            });

            var bodyTask = Task.Run(() =>
            {
                var bodyCount = bodyFrame.BodyCount;
                var faceRects = new List<Rectangle>(bodyCount);
                var faceIds = new List<long>(bodyCount);
                var bodies = new IBody[bodyCount];
                bodyFrame.CopyBodiesTo(bodies);
                for (int i = 0; i < bodyCount; i++)
                {
                    if (Util.TryGetHeadRectangle(bodies[i], mapper, out var faceRect))
                    {
                        faceRects.Add(faceRect);
                        faceIds.Add(bodies[i].TrackingId);
                    }
                }
                return (faceRects.ToArray(), faceIds.ToArray());
            });

            return new FaceLocationResult
            {
                BytesPerPixel = 3,
                ColorBuffer = colorTask.Result,
                FaceRectangles = bodyTask.Result.Item1,
                Height = colorFrame.BytesPerPixel,
                Width = colorFrame.Width
            };
        };


        private Func<FaceLocationResult, FaceImage[]> _faceCuttingFunc => 
        f =>
        {
            var numFaces = f.FaceRectangles.Length;
            var result = new FaceImage[numFaces];
            Parallel.For(0, numFaces, i =>
            {
                result[i].PixelBuffer = f.ColorBuffer.GetBufferRect(f.Width, f.FaceRectangles[i], f.BytesPerPixel);
                result[i].OrigLocation = f.FaceRectangles[i].Location;
                result[i].TrackingId = (int)f.TrackingIds[i];
            });

            return result;
        };

        private Func<FaceImage[], FSDKFaceImage[]> _FSDKFaceImageCreationFunc =>
        f =>
        {
            var results = new FSDKFaceImage[f.Length];

            for (var i = 0; i < f.Length; i++)
            {
                var fImage = f[i];
                results[i] = new FSDKFaceImage
                {
                    Width = fImage.Width,
                    Height = fImage.Height,
                    OrigLocation = fImage.OrigLocation,
                    TrackingId = fImage.TrackingId
                };
                FSDK.LoadImageFromBuffer(ref results[i].ImageHandle,
                    fImage.PixelBuffer,
                    fImage.Height,
                    fImage.Width,
                    fImage.Height * fImage.BytesPerPixel,
                    LuxandUtil.ImageModeFromBytesPerPixel(fImage.BytesPerPixel));
            }

            return results;
        };

        private Func<FSDKFaceImage[], FSDKFaceImage[]> _faceDetectionFunc =>
        f =>
        {
            Parallel.For(0, f.Length, i =>
            {
                FSDK.DetectFace(f[i].ImageHandle, ref f[i].FacePosition);
            });

            return f;
        };

        private Func<FSDKFaceImage[], FSDKFaceImage[]> _facialFeaturesAction =>
        f =>
        {
            Parallel.For(0, f.Length, i =>
            {
                FSDK.DetectFacialFeaturesInRegion(f[i].ImageHandle, ref f[i].FacePosition, out f[i].Features);
            });

            return f;
        };
        
        private Func<FSDKFaceImage[], FaceTemplate[]> _templateExtractionFunc =>
        f =>
        {
            var results = new FaceTemplate[f.Length];
            Parallel.For(0, f.Length, i =>
            {
                var fResult = f[i];
                var result = new FaceTemplate {TrackingId = fResult.TrackingId};
                if (fResult.Features.Length == FSDK.FSDK_FACIAL_FEATURE_COUNT)
                {
                    FSDK.GetFaceTemplateUsingFeatures(fResult.ImageHandle, ref fResult.Features, out result.Template);
                }
                else
                {
                    FSDK.GetFaceTemplateInRegion(fResult.ImageHandle, ref fResult.FacePosition, out result.Template);
                }
            });

            return results;
        };

        private Action<FaceTemplate[]> _templateProcessingAction =>
        f =>
        {
            foreach (FaceTemplate t in f)
            {
                if (_trackedFaces.TryGetValue(t.TrackingId, out var faceId))
                {
                    var faceInfo = _faceDb.GetFaceInfo(faceId);
                    var similarity = faceInfo.GetSimilarity(t.Template);
                    if (similarity > SameFaceConfidenceThreshold)
                    {

                    }
                    else if (similarity >= SameFaceNewTemplateThreshold)
                    {
                        faceInfo.AddTemplate(t.Template);
                    }
                    else
                    {
                        NewFace();
                    }
                }
                else
                {
                    NewFace();
                }

                void NewFace()
                {
                    var match = _faceDb.GetBestMatch(t.Template);
                    if (match.confidence >= SameFaceConfidenceThreshold)
                    {
                        _trackedFaces[t.TrackingId] = match.id;
                    }
                    else if (match.confidence >= SameFaceNewTemplateThreshold)
                    {
                        _faceDb.TryAddFaceTemplateToExistingFace(match.id, t.Template);
                        _trackedFaces[t.TrackingId] = match.id;
                    }
                    else
                    {
                        var id = _faceDb.NextId;
                        _faceDb.TryAddNewFace(id, t.Template);
                        _trackedFaces[t.TrackingId] = id;
                    }
                }
                
            }
        };

        public Task ProccesTemplateAsync(Task<FaceTemplate> fTask)
        {
            return fTask.ContinueWith(t =>
            {
                
            }, CancellationToken);
        }

        private void SetFSDKParams()
        {
            FSDK.SetFaceDetectionParameters(_handleArbitrayRot, _determineRotAngle, _internalResizeWidth);
        }

        private int _internalResizeWidth;
        private bool _handleArbitrayRot;
        private bool _determineRotAngle;
    }



    public class FaceLocationResult
    {
        public int Width;
        public int Height;
        public byte[] ColorBuffer;
        public Rectangle[] FaceRectangles;
        public long[] TrackingIds;
        public int BytesPerPixel;
        public IBody[] Bodies;
    }

    public class FSDKFaceImage
    {
        public int ImageHandle;
        public int Width;
        public int Height;
        public Point OrigLocation;
        public FSDK.TFacePosition FacePosition;
        public FSDK.TPoint[] Features;
        public Point[] GetFacialFeatures() => Features.Select(x => x.ToPoint() + new Size(OrigLocation)).ToArray();
        public int TrackingId;
    }

    public class FaceImage
    {
        public byte[] PixelBuffer;
        public int BytesPerPixel;
        public Point OrigLocation;
        public int TrackingId;
        public int Height;
        public int Width;
    }

    public class FaceTemplate
    {
        public byte[] Template;
        public int TrackingId;
    }
}