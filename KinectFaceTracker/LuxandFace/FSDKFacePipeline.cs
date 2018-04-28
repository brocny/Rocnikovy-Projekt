using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Face;
using Core;
using Luxand;
using System.Threading.Tasks.Dataflow;
using System.Buffers;
using LuxandFace;

namespace FsdkFaceLib
{
    public class FSDKFacePipeline
    {
        public event EventHandler<FaceCutout[]> FaceCuttingComplete;
        public event EventHandler<FSDKFaceImage[]> FsdkImageCreationComplete;
        public event EventHandler<FSDKFaceImage[]> FaceDetectionComplete;
        public event EventHandler<FSDKFaceImage[]> FacialFeatureDetectionComplete;
        public event EventHandler<FaceTemplate[]> FaceTemplateExtractionComplete;
        public event EventHandler<Match<byte[]>[]> TemplateProcessingComplete;
        public IReadOnlyDictionary<long, TrackingStatus> TrackedFaces => _templateProc.TrackedFaces;
        public Task<Task> Completion { get; private set; }

        private readonly ExecutionDataflowBlockOptions _options = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = FsdkSettings.Default.PipelineQueueDepth,
            MaxDegreeOfParallelism = FsdkSettings.Default.PipelineParallelism
        };

        /// <summary>
        /// Face with tracking ID equal to <paramref name="trakckingId"/> will have its template saved in the next frame,
        ///  and a data record will be created (if not previously created) from the first frame it is found in. 
        /// </summary>
        /// <param name="trakckingId"></param>
        /// <returns>
        /// A Task, containing <see cref="TrackingStatus"/> of the face, Task will compelete once saving face's template is completed.
        /// </returns>
        public Task<TrackingStatus> Capture(long trakckingId)
        {
            return _templateProc.Capture(trakckingId);
        }

        public TaskScheduler TaskScheduler { get; set; }

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

        public int FSDKFaceDetectionThreshold
        {
            get => _faceDetectionThreshold;
            set
            {
                _faceDetectionThreshold = value;
                FSDK.SetFaceDetectionThreshold(_faceDetectionThreshold);
            }
        }

        public IFaceDatabase<byte[]> FaceDb { get; set; }

        public const string ActivationKey =
            @"hp/l+aH4rdJKVKg0Jk+KIMmyzeKusO+5R4ZJ45xJJEIRM9PxoL4qrANFDvabmSzZt2rE1cQ6NNUUmrpTMgnrM4b/PpupNxRizmu/yRhzx0qKX3hLlLB6ZK73edGhxsrAH/NieibA6EFyCEwa2QErNVFGM/kplfxKw61XQ03zHAw=";

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

        public FSDKFacePipeline(IFaceDatabase<byte[]> db = null, TaskScheduler taskScheduler = null, CancellationToken cancellationToken = default)
        {
            FaceDb = db ?? new DictionaryFaceDatabase<byte[]>();
            _templateProc = new TemplateProcessor(FaceDb);

            _options.CancellationToken = cancellationToken;
            _options.TaskScheduler = taskScheduler ?? TaskScheduler.Default;

            ConstructPipeline(_options);
            

            _bufferPool = ArrayPool<byte>.Create(1920 * 1080 * 4, 8);

            SetFSDKParams();
        }

        private void ConstructPipeline(ExecutionDataflowBlockOptions options)
        {
            _faceCuttingBlock = new TransformBlock<FaceLocationResult, FaceCutout[]>
                (new Func<FaceLocationResult, FaceCutout[]>(CreateFaceImageCutouts), options);
            _fsdkImageCreatingBlock = new TransformBlock<FaceCutout[], FSDKFaceImage[]>
                (new Func<FaceCutout[], FSDKFaceImage[]>(CreateFSDKImages), options);
            _faceDetectionBlock = new TransformBlock<FSDKFaceImage[], FSDKFaceImage[]>
                (new Func<FSDKFaceImage[], FSDKFaceImage[]>(DetectFaces), options);
            _facialFeaturesBlock = new TransformBlock<FSDKFaceImage[], FSDKFaceImage[]>
                (new Func<FSDKFaceImage[], FSDKFaceImage[]>(DetectFacialFeatures), options);
            _templateExtractionBlock = new TransformBlock<FSDKFaceImage[], FaceTemplate[]>
                (new Func<FSDKFaceImage[], FaceTemplate[]>(ExtractTemplates), options);
            _templateProcessingBlock = new ActionBlock<FaceTemplate[]>
                (new Action<FaceTemplate[]>(ProcessTemplates), options);

            var nullBlock = new ActionBlock<object>(o => { });

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

            Completion = Task.WhenAny(
                _faceCuttingBlock.Completion, _fsdkImageCreatingBlock.Completion, _faceDetectionBlock.Completion,
                _facialFeaturesBlock.Completion, _templateExtractionBlock.Completion, _templateProcessingBlock.Completion);
        }

        public void Resume(CancellationToken newCancellationToken = default)
        {
            if (!_options.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            _options.CancellationToken = newCancellationToken;
            ConstructPipeline(_options);
        }

        /// <summary>
        /// Computes approximate positions of faces based on joint data in  <paramref name="bodyFrame"/>,  
        /// </summary>
        /// <param name="colorFrame">An <see cref="IColorFrame"/></param>
        /// <param name="bodyFrame">An <see cref="IBodyFrame"/> containing the bodies found in frame</param>
        /// <param name="mapper">An <see cref="ICoordinateMapper"/> used to map body positions in the <paramref name="bodyFrame"/></param>
        /// <param name="post">If set to <c>true</c>, the results will be sent along the pipeline, if it has capacity</param>
        /// <returns>A Task of type <see cref="FaceLocationResult"/> containing processed information
        /// from the <paramref name="bodyFrame"/> and <paramref name="colorFrame"/>
        /// </returns>
        public async Task<FaceLocationResult> LocateFacesAsync
        (IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper, bool post = true)
        {
            var faces = await Task.Run(() => LocateFaces(colorFrame, bodyFrame, mapper), _options.CancellationToken);
            if (post) _faceCuttingBlock.Post(faces);
            return faces;
        }

        private async Task<FaceLocationResult> LocateFaces(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper) 
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
            }, _options.CancellationToken);

            var bodyTask = Task.Run(() =>
            {
                int bodyCount = bodyFrame.BodyCount;
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

                return (faceRects: faceRects.ToArray(), faceIds: faceIds.ToArray(), bodies);
            }, _options.CancellationToken);

            var bodyResult = await bodyTask;
            var colorResult = await colorTask;

            return new FaceLocationResult
            {
                ImageBytesPerPixel = colorFrame.BytesPerPixel,
                ColorBuffer = colorResult,
                FaceRectangles = bodyResult.faceRects,
                ImageHeight = colorFrame.Height,
                ImageWidth = colorFrame.Width,
                Bodies = bodyResult.bodies,
                TrackingIds = bodyResult.faceIds
            };
        }

        private FaceCutout[] CreateFaceImageCutouts(FaceLocationResult faceLocations) 
        {
            if (faceLocations?.FaceRectangles == null || faceLocations.FaceRectangles.Length == 0)
            {
                return null;
            }

            int numFaces = faceLocations.FaceRectangles.Length;
            var result = new FaceCutout[numFaces];
            for (int i = 0; i < numFaces; i++)
            {
                var pixelBuffer = faceLocations.ColorBuffer.GetBufferRect(faceLocations.ImageWidth,
                    faceLocations.FaceRectangles[i], faceLocations.ImageBytesPerPixel);
                var rect = faceLocations.FaceRectangles[i];
                result[i] = new FaceCutout
                {
                    OrigLocation = faceLocations.FaceRectangles[i].Location,
                    TrackingId = faceLocations.TrackingIds[i],
                    ImageBuffer = new ImageBuffer(pixelBuffer, rect.Width, rect.Height, faceLocations.ImageBytesPerPixel)
                };

                _bufferPool.Return(faceLocations.ColorBuffer);
            }

            FaceCuttingComplete?.Invoke(this, result);
            return result;
        }

        private FSDKFaceImage[] CreateFSDKImages(FaceCutout[] faceCutouts)
        {
            var fsdkFaceImages = new List<FSDKFaceImage>(faceCutouts.Length);
            foreach (var faceCutout in faceCutouts)
            {
                if (_templateProc.TrackedFaces.TryGetValue(faceCutout.TrackingId, out var status))
                {
                    var topCand = status.TopCandidate;
                    if (topCand != null)
                    {
                        if (topCand.Confirmations >=  _skipMinimumConfirmations && topCand.SkippedFrames <= _skipMaxSkips)
                        {
                            topCand.SkippedFrames++;
                            continue;
                        }

                        topCand.SkippedFrames = 0;
                    }
                        
                }

                var result = new FSDKFaceImage();

                if (FSDK.FSDKE_OK == faceCutout.ImageBuffer.ToFsdkImage(out result.ImageHandle))
                {
                    result.OrigLocation = faceCutout.OrigLocation;
                    result.TrackingId = faceCutout.TrackingId;
                    result.ImageBuffer = faceCutout.ImageBuffer;
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

            FacialFeatureDetectionComplete?.Invoke(this, fsdkFaceImages);
            return fsdkFaceImages;
        }
        
        private FaceTemplate[] ExtractTemplates(FSDKFaceImage[] faceImages) 
        {
            var results = faceImages.AsParallel()
                .Select(x =>
                {
                    var gender = x.GetGender();
                    return new FaceTemplate
                    {
                        TrackingId = x.TrackingId,
                        Template = x.GetFaceTemplate(),
                        FaceImage = x.ImageBuffer,
                        Age = x.GetAge() ?? 0,
                        Gender = gender.gender,
                        GenderConfidence = gender.confidence,
                    };
                })
                .ToArray();
            FaceTemplateExtractionComplete?.Invoke(this, results);
            return results;
        }

        private void ProcessTemplates(FaceTemplate[] templates)
        {
            var matches = _templateProc.ProcessTemplates(templates);
            TemplateProcessingComplete?.Invoke(this, matches.ToArray());
        }

        private void SetFSDKParams()
        {
            FSDK.SetFaceDetectionParameters(_handleArbitrayRot, _determineRotAngle, _internalResizeWidth);
        }

        private int _internalResizeWidth = FsdkSettings.Default.FsdkInternalResizeWidth;
        private bool _handleArbitrayRot = FsdkSettings.Default.FsdkHandleArbitraryRot;
        private bool _determineRotAngle = FsdkSettings.Default.FsdkDetermineRotAngle;
        private int _faceDetectionThreshold = FsdkSettings.Default.FsdkFaceDetectionThreshold;
        private readonly int _skipMinimumConfirmations = FsdkSettings.Default.SkipMinimumConfirmations;
        private readonly int _skipMaxSkips = FsdkSettings.Default.SkipMaxSkips;

        private readonly TemplateProcessor _templateProc;

        private TransformBlock<FaceLocationResult, FaceCutout[]> _faceCuttingBlock;
        private TransformBlock<FaceCutout[], FSDKFaceImage[]> _fsdkImageCreatingBlock;
        private TransformBlock<FSDKFaceImage[], FSDKFaceImage[]> _faceDetectionBlock;
        private TransformBlock<FSDKFaceImage[], FSDKFaceImage[]> _facialFeaturesBlock;
        private TransformBlock<FSDKFaceImage[], FaceTemplate[]> _templateExtractionBlock;
        private ActionBlock<FaceTemplate[]> _templateProcessingBlock;

        private readonly ArrayPool<byte> _bufferPool;
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
        public ImageBuffer ImageBuffer;
        
        /// <summary>
        /// Original location of the top-left point of the face rectangle in the original image
        /// </summary>
        public Point OrigLocation;
        public long TrackingId;
    }

    public class FaceTemplate : IFaceTemplate<byte[]>
    {
        public byte[] Template { get; internal set; }
        public ImageBuffer FaceImage { get; internal set; }
        public float Age { get; internal set; }
        public Gender Gender { get; internal set; }
        public float GenderConfidence { get; internal set; }
        public long TrackingId { get; internal set; }
    }
}