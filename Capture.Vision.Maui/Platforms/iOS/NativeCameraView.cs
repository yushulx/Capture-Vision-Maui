﻿using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Dynamsoft;
using Foundation;
using UIKit;
using static Capture.Vision.Maui.CameraInfo;
using static Dynamsoft.DocumentScanner;
namespace Capture.Vision.Maui.Platforms.iOS
{

    internal class NativeCameraView : UIView, IAVCaptureVideoDataOutputSampleBufferDelegate, IAVCapturePhotoCaptureDelegate
    {
        private AVCaptureDevice[] camDevices;
        private readonly CameraView cameraView;
        private readonly AVCaptureVideoPreviewLayer PreviewLayer;
        private readonly AVCaptureVideoDataOutput videoDataOutput;
        private readonly AVCaptureSession captureSession;
        private bool started = false;
        private readonly DispatchQueue cameraDispacher;
        private bool initiated = false;
        private BarcodeQRCodeReader barcodeReader;
        private MrzScanner mrzScanner;
        private DocumentScanner documentScanner;
        private DispatchQueue queue = new DispatchQueue("ReadTask", true);
        private NSData buffer;
        private volatile bool ready = true;
        public nint width;
        public nint height;
        private nint bpr;
        private BarcodeQRCodeReader.Result[] results;
        private AVCaptureDevice captureDevice;
        private AVCaptureInput captureInput = null;

        public NativeCameraView(CameraView cameraView)
        {
            this.cameraView = cameraView;

            captureSession = new AVCaptureSession
            {
                SessionPreset = AVCaptureSession.PresetPhoto
            };
            PreviewLayer = new(captureSession)
            {
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };
            Layer.AddSublayer(PreviewLayer);
            videoDataOutput = new AVCaptureVideoDataOutput();
            var videoSettings = NSDictionary.FromObjectAndKey(
                new NSNumber((int)CVPixelFormatType.CV32BGRA),
                CVPixelBuffer.PixelFormatTypeKey);
            videoDataOutput.WeakVideoSettings = videoSettings;
            videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
            cameraDispacher = new DispatchQueue("CameraDispacher");

            videoDataOutput.SetSampleBufferDelegate(this, cameraDispacher);
            NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, OrientationChanged);
            InitCameras();

            barcodeReader = BarcodeQRCodeReader.Create();
            if (cameraView.BarcodeParameters != null)
            {
                barcodeReader.SetParameters(cameraView.BarcodeParameters);
            }

            mrzScanner = MrzScanner.Create();
            documentScanner = DocumentScanner.Create();
        }

        private void ReadTask()
        {
            try
            {
                if (buffer != null)
                {
                    byte[] bytearray = buffer.ToArray();
                    cameraView.NotifyFrameReady(bytearray, (int)width, (int)height, (int)bpr, FrameReadyEventArgs.PixelFormat.RGBA8888);
                    if (cameraView.EnableBarcode)
                    {
                        results = barcodeReader.DecodeBuffer(bytearray,
                                                (int)width,
                                                (int)height,
                                                (int)bpr,
                                                BarcodeQRCodeReader.ImagePixelFormat.IPF_ARGB_8888);
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
                        cameraView.NotifyResultReady(barcodeResults, (int)width, (int)height);
                    }

                    if (cameraView.EnableDocumentDetect)
                    {
                        DocumentScanner.Result[] results = documentScanner.DetectBuffer(bytearray,
                                                (int)width,
                                                (int)height,
                                                (int)bpr,
                                                DocumentScanner.ImagePixelFormat.IPF_ARGB_8888);
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
                                NormalizedImage normalizedImage = documentScanner.NormalizeBuffer(bytearray,
                                                (int)width,
                                                (int)height,
                                                (int)bpr,
                                                DocumentScanner.ImagePixelFormat.IPF_ARGB_8888, documentResults.Points);
                                documentResults.Width = normalizedImage.Width;
                                documentResults.Height = normalizedImage.Height;
                                documentResults.Stride = normalizedImage.Stride;
                                documentResults.Format = normalizedImage.Format;
                                documentResults.Data = normalizedImage.Data;
                            }
                        }

                        cameraView.NotifyResultReady(documentResults, (int)width, (int)height);

                    }

                    if (cameraView.EnableMrz)
                    {
                        MrzResult mrzResults = new MrzResult();
                        try
                        {
                            MrzScanner.Result[] results = mrzScanner.DetectBuffer(bytearray,
                                                (int)width,
                                                (int)height,
                                                (int)bpr,
                                                MrzScanner.ImagePixelFormat.IPF_ARGB_8888);


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

                        cameraView.NotifyResultReady(mrzResults, (int)width, (int)height);
                    }
                }

                ready = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void OrientationChanged(NSNotification notification)
        {
            LayoutSubviews();
        }
        private void InitCameras()
        {
            if (!initiated)
            {
                try
                {
                    var deviceDescoverySession = AVCaptureDeviceDiscoverySession.Create(new AVCaptureDeviceType[] { AVCaptureDeviceType.BuiltInWideAngleCamera }, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
                    camDevices = deviceDescoverySession.Devices;
                    cameraView.Cameras.Clear();
                    foreach (var device in camDevices)
                    {
                        Position position = device.Position switch
                        {
                            AVCaptureDevicePosition.Back => Position.Back,
                            AVCaptureDevicePosition.Front => Position.Front,
                            _ => Position.Unknown
                        };
                        cameraView.Cameras.Add(new CameraInfo
                        {
                            Name = device.LocalizedName,
                            DeviceId = device.UniqueID,
                            Pos = position,
                            AvailableResolutions = new() { new(1920, 1080), new(1280, 720), new(640, 480), new(352, 288) }
                        });
                    }
                    deviceDescoverySession.Dispose();
                    initiated = true;
                    cameraView.UpdateCameras();
                }
                catch
                {
                }
            }
        }

        public async Task<Status> StartCameraAsync()
        {
            Status result = Status.Unavailable;
            if (initiated)
            {
                if (started) StopCamera();
                if (await CameraView.RequestPermissions())
                {
                    if (cameraView.Camera != null && captureSession != null)
                    {
                        try
                        {
                            Size FrameSize = new(0, 0);
                            captureSession.SessionPreset = FrameSize.Width switch
                            {
                                352 => AVCaptureSession.Preset352x288,
                                640 => AVCaptureSession.Preset640x480,
                                1280 => AVCaptureSession.Preset1280x720,
                                1920 => AVCaptureSession.Preset1920x1080,
                                _ => AVCaptureSession.PresetPhoto
                            };
                            captureDevice = camDevices.First(d => d.UniqueID == cameraView.Camera.DeviceId);
                            captureInput = new AVCaptureDeviceInput(captureDevice, out var err);
                            captureSession.AddInput(captureInput);
                            captureSession.AddOutput(videoDataOutput);
                            captureSession.StartRunning();
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
        public Status StopCamera()
        {
            Status result = Status.Available;
            if (initiated)
            {
                try
                {
                    if (captureSession != null)
                    {
                        if (captureSession.Running)
                            captureSession.StopRunning();

                        foreach (var output in captureSession.Outputs)
                            captureSession.RemoveOutput(output);
                        foreach (var input in captureSession.Inputs)
                        {
                            captureSession.RemoveInput(input);
                            input.Dispose();
                        }
                    }
                    started = false;
                }
                catch
                {
                    result = Status.Unavailable;
                }
            }
            else
                result = Status.Unavailable;

            return result;
        }
        public void DisposeControl()
        {
            if (started) StopCamera();
            NSNotificationCenter.DefaultCenter.RemoveObserver(UIDevice.OrientationDidChangeNotification);
            PreviewLayer?.Dispose();
            captureSession?.Dispose();
            Dispose();
        }

        [Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
        public void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            if (ready)
            {
                ready = false;
                CVPixelBuffer cVPixelBuffer = (CVPixelBuffer)sampleBuffer.GetImageBuffer();

                cVPixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
                nint dataSize = cVPixelBuffer.DataSize;
                width = cVPixelBuffer.Width;
                height = cVPixelBuffer.Height;
                IntPtr baseAddress = cVPixelBuffer.BaseAddress;
                bpr = cVPixelBuffer.BytesPerRow;
                cVPixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
                buffer = NSData.FromBytes(baseAddress, (nuint)dataSize);
                cVPixelBuffer.Dispose();
                queue.DispatchAsync(ReadTask);
            }
            sampleBuffer.Dispose();
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            CATransform3D transform = CATransform3D.MakeRotation(0, 0, 0, 1.0f);
            switch (UIDevice.CurrentDevice.Orientation)
            {
                case UIDeviceOrientation.Portrait:
                    transform = CATransform3D.MakeRotation(0, 0, 0, 1.0f);
                    break;
                case UIDeviceOrientation.PortraitUpsideDown:
                    transform = CATransform3D.MakeRotation((nfloat)Math.PI, 0, 0, 1.0f);
                    break;
                case UIDeviceOrientation.LandscapeLeft:
                    transform = CATransform3D.MakeRotation((nfloat)(-Math.PI / 2), 0, 0, 1.0f);
                    break;
                case UIDeviceOrientation.LandscapeRight:
                    transform = CATransform3D.MakeRotation((nfloat)Math.PI / 2, 0, 0, 1.0f);
                    break;
            }

            PreviewLayer.Transform = transform;
            PreviewLayer.Frame = Layer.Bounds;
        }
    }


}