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
using Core.Kinect;
using FsdkFaceLib.Properties;


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
        /// <param name="forceNewFace">If this parameter is set to <c>true</c>, a new face will always be added, overriding any previous recogntion</param>
        /// <returns>
        /// A Task, containing <see cref="TrackingStatus"/> of the face, Task will compelete once saving face's template is completed.
        /// </returns>
        public Task<TrackingStatus> Capture(long trakckingId, bool forceNewFace = false)
        {
            return _templateProc.Capture(trakckingId, forceNewFace);
        }

        public TaskScheduler TaskScheduler { get; }

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
                _handleArbitrayRot = value;;
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

        private static readonly string ActivationKey = FsdkSettings.Default.FsdkActiovationKey;

        private static bool _isLibraryActivated;
        public static void InitializeLibrary()
        {
            if (_isLibraryActivated) return;
            if (FSDK.FSDKE_OK != FSDK.ActivateLibrary(ActivationKey))
            {
                throw new ApplicationException("Invalid Luxand FSDK activation key! If the key is expired, a new key can be requested at https://www.luxand.com/facesdk/requestkey/");
            }
            FSDK.InitializeLibrary();
            _isLibraryActivated = true;
        }

        public FSDKFacePipeline(IFaceDatabase<byte[]> db = null, TaskScheduler taskScheduler = null, CancellationToken cancellationToken = default)
        {
            FaceDb = db ?? new DictionaryFaceDatabase<byte[]>();
            _templateProc = new TemplateProcessor(FaceDb);

            _options.CancellationToken = cancellationToken;
            _options.TaskScheduler = TaskScheduler = taskScheduler ?? TaskScheduler.Default;

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
                try
                {
                    bodyFrame.CopyBodiesTo(bodies);
                }
                catch (NullReferenceException)
                {

                }
                foreach (var body in bodies.Where(b => b != null && b.IsTracked))
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
                ImageBuffer = new ImageBuffer(colorResult, colorFrame.Width, colorFrame.Height, colorFrame.BytesPerPixel),
                FaceRectangles = bodyResult.faceRects,
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
                var pixelBuffer = faceLocations.ImageBuffer.Buffer.GetBufferRect(faceLocations.ImageBuffer.Width,
                    faceLocations.FaceRectangles[i], faceLocations.ImageBuffer.BytesPerPixel);
                var rect = faceLocations.FaceRectangles[i];
                result[i] = new FaceCutout
                {
                    OrigLocation = faceLocations.FaceRectangles[i].Location,
                    TrackingId = faceLocations.TrackingIds[i],
                    ImageBuffer = faceLocations.ImageBuffer.GetRectangle(faceLocations.FaceRectangles[i])
                };

                _bufferPool.Return(faceLocations.ImageBuffer.Buffer);
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
                    if (status.TopCandidate.Confirmations >=  _skipMinimumConfirmations && status.SkippedFrames <= _skipMaxSkips)
                    {
                        status.SkippedFrames++;
                        continue;
                    }
                    else
                    {
                        status.SkippedFrames = 0;
                    }
                }

                var result = new FSDKFaceImage();

                if (FSDK.FSDKE_OK == faceCutout.ImageBuffer.CreateFsdkImageHandle(out result.ImageHandle))
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
            if (!_detectFace)
            {
                return fsdkFaceImages;
            }

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
            if (!_detectFeatures)
            {
                return fsdkFaceImages;
            }

            Parallel.For(0, fsdkFaceImages.Length, i =>
            {
                fsdkFaceImages[i].DetectFeatures();
            });

            FacialFeatureDetectionComplete?.Invoke(this, fsdkFaceImages);
            return fsdkFaceImages;
        }
        
        private FaceTemplate[] ExtractTemplates(FSDKFaceImage[] faceImages)
        {
            var results = new FaceTemplate[faceImages.Length];

            Parallel.For(0,  faceImages.Length, i =>
            {
                var faceImage = faceImages[i];
                var gender = faceImage.GetGender();
                var template = faceImage.GetFaceTemplate();
                if (template == null)
                    return;
                results[i] = new FaceTemplate
                {
                    TrackingId = faceImage.TrackingId,
                    Template = template,
                    FaceImage = faceImage.ImageBuffer,
                    Age = faceImage.GetAge() ?? 0,
                    Gender = gender.gender,
                    GenderConfidence = gender.confidence,
                };
            });

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
        private bool _detectFace = FsdkSettings.Default.FsdkDetectFace;
        private bool _detectFeatures = FsdkSettings.Default.FsdkDetectFeatures;
        private int _faceDetectionThreshold = FsdkSettings.Default.FsdkFaceDetectionThreshold;
        private readonly int _skipMinimumConfirmations = FsdkSettings.Default.SkipMinimumConfirmations;
        private readonly int _skipMaxSkips = FsdkSettings.Default.MaxSkippedFrames;

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
        public ImageBuffer ImageBuffer;

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