using Java.Util.Concurrent;
using Android.Content;
using Android.Widget;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Image = Android.Media.Image;
using Android.Views;
using Android.Util;
using Android.Hardware.Camera2.Params;
using Android.Runtime;
using Android.OS;
using CameraCharacteristics = Android.Hardware.Camera2.CameraCharacteristics;
using Size = Android.Util.Size;
using Class = Java.Lang.Class;
using static Capture.Vision.Maui.CameraInfo;
using Dynamsoft;
using static Dynamsoft.BarcodeQRCodeReader;
using static Dynamsoft.MrzScanner;
using Java.Nio;
using static Dynamsoft.DocumentScanner;

namespace Capture.Vision.Maui.Platforms.Android
{
    internal class NativeCameraView : FrameLayout
    {
        private readonly CameraView cameraView;
        private IExecutorService executorService;
        private bool started = false;
        private bool initiated = false;
        private readonly Context context;
        private readonly TextureView textureView;
        public CameraCaptureSession previewSession;
        private CaptureRequest.Builder previewBuilder;
        private CameraDevice cameraDevice;
        private readonly CameraStateCallback stateListener;
        private Size videoSize;
        private CameraManager cameraManager;
        private readonly SparseIntArray ORIENTATIONS = new();
        private CameraCharacteristics camChars;
        private PreviewCaptureStateCallback sessionCallback;
        private ImageAvailableListener frameListener;
        private HandlerThread backgroundThread;
        private Handler backgroundHandler;
        private ImageReader imageReader;
        private BarcodeQRCodeReader barcodeReader;
        private MrzScanner mrzScanner;
        private DocumentScanner documentScanner;
        public NativeCameraView(Context context, CameraView cameraView) : base(context)
        {
            this.context = context;
            this.cameraView = cameraView;

            textureView = new(context);
            stateListener = new CameraStateCallback(this);

            AddView(textureView);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);
            InitCameras();

            barcodeReader = BarcodeQRCodeReader.Create();
            if (cameraView.BarcodeParameters != null)
            {
                barcodeReader.SetParameters(cameraView.BarcodeParameters);
            }

            mrzScanner = MrzScanner.Create();
            documentScanner = DocumentScanner.Create();
        }

        private void InitCameras()
        {
            if (!initiated && cameraView != null)
            {
                cameraManager = (CameraManager)context.GetSystemService(Context.CameraService);
                cameraView.Cameras.Clear();
                foreach (var id in cameraManager.GetCameraIdList())
                {
                    var cameraInfo = new CameraInfo { DeviceId = id };
                    var chars = cameraManager.GetCameraCharacteristics(id);
                    if ((int)(chars.Get(CameraCharacteristics.LensFacing) as Java.Lang.Number) == (int)LensFacing.Back)
                    {
                        cameraInfo.Name = "Back Camera";
                        cameraInfo.Pos = Position.Back;
                    }
                    else if ((int)(chars.Get(CameraCharacteristics.LensFacing) as Java.Lang.Number) == (int)LensFacing.Front)
                    {
                        cameraInfo.Name = "Front Camera";
                        cameraInfo.Pos = Position.Front;
                    }
                    else
                    {
                        cameraInfo.Name = "Camera " + id;
                        cameraInfo.Pos = Position.Unknown;
                    }
                    StreamConfigurationMap map = (StreamConfigurationMap)chars.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                    cameraInfo.AvailableResolutions = new();
                    foreach (var s in map.GetOutputSizes(Class.FromType(typeof(ImageReader))))
                        cameraInfo.AvailableResolutions.Add(new(s.Width, s.Height));
                    cameraView.Cameras.Add(cameraInfo);
                }
                executorService = Executors.NewSingleThreadExecutor();

                initiated = true;
                cameraView.UpdateCameras();
            }
        }

        private void StartPreview()
        {
            while (textureView.SurfaceTexture == null) Thread.Sleep(100);
            SurfaceTexture texture = textureView.SurfaceTexture;
            texture.SetDefaultBufferSize(videoSize.Width, videoSize.Height);

            previewBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
            var surfaces = new List<OutputConfiguration>();
            var previewSurface = new Surface(texture);
            surfaces.Add(new OutputConfiguration(previewSurface));
            previewBuilder.AddTarget(previewSurface);

            imageReader = ImageReader.NewInstance(videoSize.Width, videoSize.Height, ImageFormatType.Yuv420888, 1);
            backgroundThread = new HandlerThread("CameraBackground");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
            frameListener = new ImageAvailableListener(cameraView, barcodeReader, mrzScanner, documentScanner);
            imageReader.SetOnImageAvailableListener(frameListener, backgroundHandler);
            surfaces.Add(new OutputConfiguration(imageReader.Surface));
            previewBuilder.AddTarget(imageReader.Surface);

            sessionCallback = new PreviewCaptureStateCallback(this);
            SessionConfiguration config = new((int)SessionType.Regular, surfaces, executorService, sessionCallback);
            cameraDevice.CreateCaptureSession(config);
        }
        private void UpdatePreview()
        {
            if (null == cameraDevice)
                return;

            try
            {
                previewBuilder.Set(CaptureRequest.ControlMode, Java.Lang.Integer.ValueOf((int)ControlMode.Auto));
                AdjustAspectRatio(videoSize.Width, videoSize.Height);
                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }
        internal async Task<Status> StartCameraAsync()
        {
            var result = Status.Unavailable;
            if (initiated)
            {
                if (await CameraView.RequestPermissions())
                {
                    if (started) StopCamera();
                    if (cameraView.Camera != null)
                    {
                        try
                        {
                            camChars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);
                            StreamConfigurationMap map = (StreamConfigurationMap)camChars.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                            videoSize = ChooseVideoSize(map.GetOutputSizes(Class.FromType(typeof(ImageReader))));
                            cameraManager.OpenCamera(cameraView.Camera.DeviceId, executorService, stateListener);

                            started = true;
                            result = Status.Available;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return result;
        }

        internal Status StopCamera()
        {
            Status result = Status.Available;
            if (initiated)
            {
                try
                {
                    imageReader?.SetOnImageAvailableListener(null, null);
                    imageReader?.Dispose();
                    imageReader = null;
                    backgroundThread?.QuitSafely();
                    backgroundThread?.Join();
                    backgroundThread = null;
                    backgroundHandler = null;

                }
                catch { }
                try
                {
                    previewSession?.StopRepeating();
                    previewSession?.Dispose();
                }
                catch { }
                try
                {
                    cameraDevice?.Close();
                    cameraDevice?.Dispose();
                }
                catch { }
                previewSession = null;
                cameraDevice = null;
                previewBuilder = null;
                started = false;
            }
            else
                result = Status.Unavailable;

            return result;
        }
        internal void DisposeControl()
        {
            try
            {
                if (started) StopCamera();
                executorService?.Shutdown();
                executorService?.Dispose();
                RemoveAllViews();
                textureView?.Dispose();
                Dispose();
            }
            catch { }
        }

        private Size ChooseVideoSize(Size[] choices)
        {
            Size result = new Size(640, 480);
            foreach (Size size in choices)
            {
                if (size.Width == 1280 && size.Height == 720)
                {
                    result = size;
                    break;
                }
            }

            return result;
        }

        private void AdjustAspectRatio(int videoWidth, int videoHeight)
        {
            Matrix txform = new();
            float scaleX = (float)videoWidth / Width;
            float scaleY = (float)videoHeight / Height;
            if (IsDimensionSwapped())
            {
                scaleX = (float)videoHeight / Width;
                scaleY = (float)videoWidth / Height;
            }
            if (scaleX <= scaleY)
            {
                scaleY /= scaleX;
                scaleX = 1;
            }
            else
            {
                scaleX /= scaleY;
                scaleY = 1;
            }
            txform.PostScale(scaleX, scaleY, 0, 0);
            textureView.SetTransform(txform);
        }

        private bool IsDimensionSwapped()
        {
            IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var displayRotation = windowManager.DefaultDisplay.Rotation;
            var chars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);
            int sensorOrientation = (int)(chars.Get(CameraCharacteristics.SensorOrientation) as Java.Lang.Integer);
            bool swappedDimensions = false;
            switch (displayRotation)
            {
                case SurfaceOrientation.Rotation0:
                case SurfaceOrientation.Rotation180:
                    if (sensorOrientation == 90 || sensorOrientation == 270)
                    {
                        swappedDimensions = true;
                    }
                    break;
                case SurfaceOrientation.Rotation90:
                case SurfaceOrientation.Rotation270:
                    if (sensorOrientation == 0 || sensorOrientation == 180)
                    {
                        swappedDimensions = true;
                    }
                    break;
            }
            return swappedDimensions;
        }

        private class CameraStateCallback : CameraDevice.StateCallback
        {
            private readonly NativeCameraView cameraView;
            public CameraStateCallback(NativeCameraView camView)
            {
                cameraView = camView;
            }
            public override void OnOpened(CameraDevice camera)
            {
                if (camera != null)
                {
                    cameraView.cameraDevice = camera;
                    cameraView.StartPreview();
                }
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                camera.Close();
                cameraView.cameraDevice = null;
            }

            public override void OnError(CameraDevice camera, CameraError error)
            {
                camera?.Close();
                cameraView.cameraDevice = null;
            }
        }

        private class PreviewCaptureStateCallback : CameraCaptureSession.StateCallback
        {
            private readonly NativeCameraView cameraView;
            public PreviewCaptureStateCallback(NativeCameraView camView)
            {
                cameraView = camView;
            }
            public override void OnConfigured(CameraCaptureSession session)
            {
                cameraView.previewSession = session;
                cameraView.UpdatePreview();

            }
            public override void OnConfigureFailed(CameraCaptureSession session)
            {
            }
        }
        class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
        {
            private readonly CameraView cameraView;
            internal bool isReady = true;
            private BarcodeQRCodeReader barcodeReader;
            private MrzScanner mrzScanner;
            private DocumentScanner documentScanner;

            public ImageAvailableListener(CameraView camView, BarcodeQRCodeReader barcodeReader, MrzScanner mrzScanner, DocumentScanner documentScanner)
            {
                cameraView = camView;
                this.barcodeReader = barcodeReader;
                this.mrzScanner = mrzScanner;
                this.documentScanner = documentScanner;
            }

            public void OnImageAvailable(ImageReader reader)
            {
                try
                {
                    var image = reader?.AcquireLatestImage();
                    if (image == null)
                        return;

                    Image.Plane[] planes = image.GetPlanes();
                    if (planes == null) return;

                    int width = image.Width;
                    int height = image.Height;
                    ByteBuffer buffer = planes[0].Buffer;
                    byte[] bytes = new byte[buffer.Remaining()];
                    buffer.Get(bytes);
                    int nRowStride = planes[0].RowStride;
                    int nPixelStride = planes[0].PixelStride;
                    image.Close();

                    cameraView.NotifyFrameReady(bytes, width, height, nPixelStride * nRowStride, FrameReadyEventArgs.PixelFormat.GRAYSCALE);
                    if (cameraView.EnableBarcode)
                    {
                        BarcodeQRCodeReader.Result[] results = barcodeReader.DecodeBuffer(bytes, width, height, nPixelStride * nRowStride, BarcodeQRCodeReader.ImagePixelFormat.IPF_GRAYSCALED);
                        BarcodeResult[] barcodeResults = new BarcodeResult[0];
                        if (results != null && results.Length > 0)
                        {
                            barcodeResults = new BarcodeResult[results.Length];

                            for (int i = 0; i < results.Length; i++)
                            {
                                barcodeResults[i] = new BarcodeResult()
                                {
                                    Text = results[i].Text,
                                    Points = results[i].Points,
                                    Format1 = results[i].Format1,
                                    Format2 = results[i].Format2
                                };
                            }
                        }
                        cameraView.NotifyResultReady(barcodeResults, width, height);
                    }

                    if (cameraView.EnableDocumentDetect)
                    {
                        DocumentScanner.Result[] results = documentScanner.DetectBuffer(bytes, width, height, nPixelStride * nRowStride, DocumentScanner.ImagePixelFormat.IPF_GRAYSCALED);
                        DocumentResult documentResults = new DocumentResult();
                        if (results != null && results.Length > 0)
                        {
                            documentResults = new DocumentResult
                            {
                                Confidence = results[0].Confidence,
                                Points = results[0].Points
                            };

                            if (cameraView.EnableDocumentRectify)
                            {
                                NormalizedImage normalizedImage = documentScanner.NormalizeBuffer(bytes, width, height, nPixelStride * nRowStride, DocumentScanner.ImagePixelFormat.IPF_GRAYSCALED, documentResults.Points);
                                documentResults.Width = normalizedImage.Width;
                                documentResults.Height = normalizedImage.Height;
                                documentResults.Stride = normalizedImage.Stride;
                                documentResults.Format = normalizedImage.Format;
                                documentResults.Data = normalizedImage.Data;
                            }
                        }

                        cameraView.NotifyResultReady(documentResults, width, height);

                    }

                    if (cameraView.EnableMrz)
                    {
                        MrzResult mrzResults = new MrzResult();
                        try
                        {
                            MrzScanner.Result[] results = mrzScanner.DetectBuffer(bytes, width, height, nPixelStride * nRowStride, MrzScanner.ImagePixelFormat.IPF_GRAYSCALED);


                            if (results != null && results.Length > 0)
                            {
                                Line[] rawData = new Line[results.Length];
                                string[] lines = new string[results.Length];

                                for (int i = 0; i < results.Length; i++)
                                {
                                    rawData[i] = new Line()
                                    {
                                        Confidence = results[i].Confidence,
                                        Text = results[i].Text,
                                        Points = results[i].Points,
                                    };
                                    lines[i] = results[i].Text;
                                }


                                Dynamsoft.MrzResult info = MrzParser.Parse(lines);
                                mrzResults = new MrzResult()
                                {
                                    RawData = rawData,
                                    Type = info.Type,
                                    Nationality = info.Nationality,
                                    Surname = info.Surname,
                                    GivenName = info.GivenName,
                                    PassportNumber = info.PassportNumber,
                                    IssuingCountry = info.IssuingCountry,
                                    BirthDate = info.BirthDate,
                                    Gender = info.Gender,
                                    Expiration = info.Expiration,
                                    Lines = info.Lines
                                };

                            }
                        }

                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                        }

                        cameraView.NotifyResultReady(mrzResults, width, height);
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }
    }

}

