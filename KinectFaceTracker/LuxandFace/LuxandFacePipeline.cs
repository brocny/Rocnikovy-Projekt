using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Face;
using KinectUnifier;
using Luxand;
using System.Threading.Tasks.Dataflow;
using System.Buffers;

namespace LuxandFaceLib
{
    public class LuxandFacePipeline
    {
        /// <summary>
        /// Guaranteed thread-safety
        /// </summary>
        public IReadOnlyDictionary<long, TrackingStatus> TrackedFaces => _trackedFaces;

        public event EventHandler<FaceCutout[]> FaceCuttingComplete;
        public event EventHandler<FSDKFaceImage[]> FsdkImageCreationComplete;
        public event EventHandler<FSDKFaceImage[]> FaceDetectionComplete;
        public event EventHandler<FSDKFaceImage[]> FacialFeatureRecognitionComplete;
        public event EventHandler<FaceTemplate[]> FaceTemplateExtractionComplete;
        public event EventHandler<(long trackingId, (int faceId, float confidence) match)[]> TemplateProcessingComplete;

        public Task<Task> Completion;

        public TaskScheduler SynchContext { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public int FSDKInternalResizeWidth
        {
            get => _internalResizeWidth;
            set
            {
                _internalResizeWidth = value;
                SetFSDKParams();
            }
        }

        public bool FSDKDetermineLocation
        {
            get => _determineRotAngle;
            set
            {
                _determineRotAngle = value;
                SetFSDKParams();
            }
        }

        public bool FSDKHandleArbitrayRot
        {
            get => _handleArbitrayRot;
            set
            {
                _handleArbitrayRot = value;
                SetFSDKParams();
            }
        }

        public float SameFaceConfidenceThreshold { get; set; } = 0.92f;
        public float UntrackedFaceNewTemplateThreshold { get; set; } = 0.7f;
        public float TrackedFaceNewTemplateThreshold = 0.35f;

        internal const string ActivationKey =
            @"ocrTzAn1FVrzPDhjCSvECux7/jSDBHkqOxLbfDbYTJKjRkLhxUicRk5jpJOxvPAsKkfDHdvwJAD+PoTPoXhDDw55/AdPWagMzbbabtbDQQ4UbeeeN9IIqS09o78440AGXSmzUCfJHBBjwY1dIy5vL5DJgrOm27RTPd1kbGpOFFY=";

        private static bool _isLibraryActivated;
        public static void InitializeLibrary()
        {
            if (_isLibraryActivated) return;
            if (FSDK.FSDKE_OK != FSDK.ActivateLibrary(ActivationKey))
            {
                throw new ApplicationException("Invalid Luxand FSDK activation key");
            }
            FSDK.InitializeLibrary();
            _isLibraryActivated = true;
        }

        public LuxandFacePipeline(FaceDatabase<byte[]> db = null, TaskScheduler taskScheduler = null, IDictionary<long, TrackingStatus> trackedFaces = null)
        {
            _faceDb = db ?? new FaceDatabase<byte[]>();
            _trackedFaces = trackedFaces == null ? new ConcurrentDictionary<long, TrackingStatus>() : new ConcurrentDictionary<long, TrackingStatus>(trackedFaces);

            var options = new ExecutionDataflowBlockOptions {
                BoundedCapacity = 1,
                TaskScheduler = taskScheduler ?? TaskScheduler.Default,
                MaxDegreeOfParallelism = 1,
                CancellationToken = CancellationToken
            };

            _faceCuttingBlock = new TransformBlock<FaceLocationResult, FaceCutout[]>(new Func<FaceLocationResult, FaceCutout[]>(CreateFaceImageCutouts), options);
            _fsdkImageCreatingBlock = new TransformBlock<FaceCutout[], FSDKFaceImage[]>(new Func<FaceCutout[], FSDKFaceImage[]>(CreateFSDKImages), options);
            _faceDetectionBlock = new TransformBlock<FSDKFaceImage[], FSDKFaceImage[]>(new Func<FSDKFaceImage[], FSDKFaceImage[]>(DetectFaces), options);
            _facialFeaturesBlock = new TransformBlock<FSDKFaceImage[], FSDKFaceImage[]>(new Func<FSDKFaceImage[], FSDKFaceImage[]>(DetectFacialFeatures), options);
            _templateExtractionBlock = new TransformBlock<FSDKFaceImage[], FaceTemplate[]>(new Func<FSDKFaceImage[], FaceTemplate[]>(ExtractTemplates), options);
            _templateProcessingBlock = new ActionBlock<FaceTemplate[]>(new Action<FaceTemplate[]>(ProcessTemplates), options);

            var nullBlock = new ActionBlock<object>(o => {});
            
            _faceCuttingBlock.LinkTo(_fsdkImageCreatingBlock, obj => obj != null);
            _faceCuttingBlock.LinkTo(nullBlock, obj => obj == null);
            _fsdkImageCreatingBlock.LinkTo(_faceDetectionBlock, obj => obj != null);
            _fsdkImageCreatingBlock.LinkTo(nullBlock, obj => obj == null);
            _faceDetectionBlock.LinkTo(_facialFeaturesBlock, obj => obj != null);
            _faceDetectionBlock.LinkTo(nullBlock, obj => obj == null);
            _facialFeaturesBlock.LinkTo(_templateExtractionBlock, obj => obj != null);
            _facialFeaturesBlock.LinkTo(nullBlock, obj => obj == null);
            _templateExtractionBlock.LinkTo(_templateProcessingBlock, obj => obj != null);
            _templateExtractionBlock.LinkTo(nullBlock, obj => obj == null);

            _bufferPool = ArrayPool<byte>.Create(1920 * 1080 * 4, 5);

            Completion = Task.WhenAny(
                _faceCuttingBlock.Completion, _fsdkImageCreatingBlock.Completion, _faceDetectionBlock.Completion,
                _facialFeaturesBlock.Completion, _templateExtractionBlock.Completion, _templateProcessingBlock.Completion);

            SetFSDKParams();
        }

        public Task<FaceLocationResult> LocateFacesAsync(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper)
        {
            var retTask = Task.Run(() => LocateFaces(colorFrame, bodyFrame, mapper), CancellationToken);

            retTask.ContinueWith(t =>
            {
                if (_faceCuttingBlock.InputCount <= 1)
                {
                   _faceCuttingBlock.Post(t.Result);
                }
            }, CancellationToken);

            return retTask;
        }

        private FaceLocationResult LocateFaces(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper) 
        {
            var colorTask = Task.Run(() =>
            {
                byte[] buffer = _bufferPool.Rent(colorFrame.PixelDataLength);
                unsafe
                {
                    fixed (byte* p = buffer)
                    {
                        colorFrame.CopyFramePixelDataToIntPtr((IntPtr)p, colorFrame.PixelDataLength);
                    }
                }
                return buffer;
            }, CancellationToken);

            var bodyTask = Task.Run(() =>
            {
                var bodyCount = bodyFrame.BodyCount;
                var faceRects = new List<Rectangle>(bodyCount);
                var faceIds = new List<long>(bodyCount);
                var bodies = new IBody[bodyCount];
                bodyFrame.CopyBodiesTo(bodies);

                foreach (var body in bodies.Where(b => b.IsTracked))
                {
                    if (Util.TryGetHeadRectangleAndYawAngle(body, mapper, out var faceRect, out _))
                    {
                        faceRects.Add(faceRect);
                        faceIds.Add(body.TrackingId);
                    }
                }

                return (faceRects.ToArray(), faceIds.ToArray(), bodies);
            }, CancellationToken);

            return new FaceLocationResult
            {
                ImageBytesPerPixel = colorFrame.BytesPerPixel,
                ColorBuffer = colorTask.Result,
                FaceRectangles = bodyTask.Result.Item1,
                ImageHeight = colorFrame.Height,
                ImageWidth = colorFrame.Width,
                Bodies =  bodyTask.Result.Item3,
                TrackingIds = bodyTask.Result.Item2
            };
        }

        private FaceCutout[] CreateFaceImageCutouts(FaceLocationResult faceLocations) 
        {
            if (faceLocations?.FaceRectangles == null || faceLocations.FaceRectangles.Length == 0)
            {
                return null;
            }

            var numFaces = faceLocations.FaceRectangles.Length;
            var result = new FaceCutout[numFaces];
            for (int i = 0; i < numFaces; i++)
            {
                var pixelBuffer = faceLocations.ColorBuffer.GetBufferRect(faceLocations.ImageWidth,
                    faceLocations.FaceRectangles[i], faceLocations.ImageBytesPerPixel);
                result[i] = new FaceCutout
                {
                    PixelBuffer = pixelBuffer,
                    OrigLocation = faceLocations.FaceRectangles[i].Location,
                    TrackingId = faceLocations.TrackingIds[i],
                    Width = faceLocations.FaceRectangles[i].Width,
                    Height = faceLocations.FaceRectangles[i].Height,
                    BytesPerPixel = faceLocations.ImageBytesPerPixel
                };
                _bufferPool.Return(faceLocations.ColorBuffer);
            }
            

            FaceCuttingComplete?.Invoke(this, result);
            return result;
        }

        private  FSDKFaceImage[] CreateFSDKImages(FaceCutout[] faceCutouts)
        {
            var fsdkFaceImages = new List<FSDKFaceImage>(faceCutouts.Length);
            foreach (var faceCutout in faceCutouts)
            {
                if (_trackedFaces.TryGetValue(faceCutout.TrackingId, out var status))
                {
                    if (status.Confirmations >= 5 && status.SkippedFrames <= 10)
                    {
                        status.SkippedFrames = 0;
                        continue;
                    }

                    status.SkippedFrames++;
                }

                var result = new FSDKFaceImage
                {
                    Width = faceCutout.Width,
                    Height = faceCutout.Height,
                    OrigLocation = faceCutout.OrigLocation,
                    TrackingId = faceCutout.TrackingId,
                };

                if (FSDK.FSDKE_OK == FSDK.LoadImageFromBuffer(ref result.ImageHandle, faceCutout.PixelBuffer, faceCutout.Width,
                    faceCutout.Height, faceCutout.Width * faceCutout.BytesPerPixel,
                    LuxandUtil.ImageModeFromBytesPerPixel(faceCutout.BytesPerPixel)))
                {
                    fsdkFaceImages.Add(result);
                }
            }

            var res = fsdkFaceImages.ToArray();
            FsdkImageCreationComplete?.Invoke(this, res);
            return res;
        }

        private FSDKFaceImage[] DetectFaces(FSDKFaceImage[] fsdkFaceImages)
        {
            Parallel.For(0, fsdkFaceImages.Length, i =>
            {
                FSDK.DetectFace(fsdkFaceImages[i].ImageHandle, ref fsdkFaceImages[i].FacePosition);
            });

            var result = fsdkFaceImages.Where(i => i.FacePosition != null).ToArray();
            FaceDetectionComplete?.Invoke(this, result);
            return result;
        }

        private FSDKFaceImage[] DetectFacialFeatures(FSDKFaceImage[] fsdkFaceImages)
        {
            Parallel.For(0, fsdkFaceImages.Length, i =>
            {
                fsdkFaceImages[i].DetectFeatures();
            });

            FacialFeatureRecognitionComplete?.Invoke(this, fsdkFaceImages);
            return fsdkFaceImages;
        }
        
        private FaceTemplate[] ExtractTemplates(FSDKFaceImage[] f) 
        {
            var results = f.AsParallel()
                .Select(x =>
                {
                    var gender = x.GetGender();
                    return new FaceTemplate
                    {
                        TrackingId = x.TrackingId,
                        Template = x.GetFaceTemplate(),
                        Age = x.GetAge() ?? 0,
                        Gender = gender.gender,
                        GenderConfidence = gender.confidence
                    };
                })
                .ToArray();
            FaceTemplateExtractionComplete?.Invoke(this, results);
            return results;
        }

        private void ProcessTemplates(FaceTemplate[] templates)
        {
            var matches = new (long trackingId, (int faceId, float confidence) match)[templates.Length];
            for (var i = 0; i < templates.Length; i++)
            {
                var t = templates[i];
                // face is already tracked
                if (_trackedFaces.TryGetValue(t.TrackingId, out var trackingStatus))
                {
                    var faceInfo = _faceDb[trackingStatus.FaceId];
                    var similarity = faceInfo.GetSimilarity(t);
                    if (similarity > SameFaceConfidenceThreshold)
                    {
                        matches[i] = (t.TrackingId, (trackingStatus.FaceId, similarity));
                        trackingStatus.Confirmations++;
                    }
                    else if (similarity >= TrackedFaceNewTemplateThreshold)
                    {
                        faceInfo.AddTemplate(t);
                        matches[i] = (t.TrackingId, (trackingStatus.FaceId, similarity));
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
                    var bestMatch = _faceDb.GetBestMatch(t.Template);
                    if (bestMatch.confidence >= SameFaceConfidenceThreshold)
                    {
                        _trackedFaces[t.TrackingId] = new TrackingStatus {FaceId = bestMatch.id};
                    }
                    else if (bestMatch.confidence >= UntrackedFaceNewTemplateThreshold)
                    {
                        _faceDb.TryAddFaceTemplateToExistingFace(bestMatch.id, t.Template);
                        _trackedFaces[t.TrackingId] = new TrackingStatus {FaceId = bestMatch.id};
                    }
                    else
                    {
                        var id = _faceDb.NextId;
                        _faceDb.TryAddNewFace(id, t.Template);
                        _trackedFaces[t.TrackingId] = new TrackingStatus {FaceId = id};
                    }
                    matches[i] = (t.TrackingId, bestMatch);
                }
            }

            TemplateProcessingComplete?.Invoke(this, matches);
        }

        private void SetFSDKParams()
        {
            FSDK.SetFaceDetectionParameters(_handleArbitrayRot, _determineRotAngle, _internalResizeWidth);
        }

        private int _internalResizeWidth = 100;
        private bool _handleArbitrayRot = false;
        private bool _determineRotAngle = false;

        private FaceDatabase<byte[]> _faceDb;
        private ConcurrentDictionary<long, TrackingStatus> _trackedFaces;

        private TransformBlock<FaceLocationResult, FaceCutout[]> _faceCuttingBlock;
        private TransformBlock<FaceCutout[], FSDKFaceImage[]> _fsdkImageCreatingBlock;
        private TransformBlock<FSDKFaceImage[], FSDKFaceImage[]> _faceDetectionBlock;
        private TransformBlock<FSDKFaceImage[], FSDKFaceImage[]> _facialFeaturesBlock;
        private TransformBlock<FSDKFaceImage[], FaceTemplate[]> _templateExtractionBlock;
        private ActionBlock<FaceTemplate[]> _templateProcessingBlock;

        private ArrayPool<byte> _bufferPool;
    }



    public class FaceLocationResult
    {
        public int ImageWidth;
        public int ImageHeight;
        public int ImageBytesPerPixel;
        public byte[] ColorBuffer;
        public Rectangle[] FaceRectangles;
        public long[] TrackingIds;
        public IBody[] Bodies;
    }

    public class FaceCutout
    {
        /// <summary>
        /// Bitmap image of the face
        /// </summary>
        public byte[] PixelBuffer;

        public int Width;
        public int Height;

        public int BytesPerPixel;
        /// <summary>
        /// Original location of the top-left point of the face rectangle in the original image
        /// </summary>
        public Point OrigLocation;
        public long TrackingId;
    }

    public class FaceTemplate : IFaceTemplate<byte[]>
    {
        public byte[] Template { get; internal set; }
        public float Age { get; internal set; }
        public Gender Gender { get; internal set; }
        public float GenderConfidence { get; internal set; }
        public long TrackingId { get; internal set; }
    }

    public class TrackingStatus
    {
        public short Confirmations;
        public short SkippedFrames;
        public int FaceId;
    }
}