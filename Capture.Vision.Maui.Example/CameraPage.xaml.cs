using Dynamsoft;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Runtime.InteropServices;
using static Capture.Vision.Maui.FrameReadyEventArgs;
using static Dynamsoft.BarcodeQRCodeReader;

namespace Capture.Vision.Maui.Example;

public class BarcodeQrData
{
    public string text;
    public string format;
    public SKPoint[] points;

    public static BarcodeQrData[] Convert(BarcodeQRCodeReader.Result[] results)
    {
        BarcodeQrData[] output = null;
        if (results != null && results.Length > 0)
        {
            output = new BarcodeQrData[results.Length];
            for (int index = 0; index < results.Length; ++index)
            {
                BarcodeQRCodeReader.Result result = results[index];
                BarcodeQrData data = new BarcodeQrData
                {
                    text = result.Text,
                    format = result.Format1
                };
                int[] coordinates = result.Points;
                if (coordinates != null && coordinates.Length == 8)
                {
                    data.points = new SKPoint[4];

                    for (int i = 0; i < 4; ++i)
                    {
                        SKPoint p = new SKPoint();
                        p.X = coordinates[i * 2];
                        p.Y = coordinates[i * 2 + 1];
                        data.points[i] = p;
                    }
                }


                output[index] = data;
            }
        }
        return output;
    }
}

public partial class CameraPage : ContentPage
{
    BarcodeQrData[] data = null;
    private int imageWidth;
    private int imageHeight;
    bool saveImage = true;
    private static object _lockObject = new object();
    DisplayOrientation orientation;
    DisplayRotation rotation;
    DisplayInfo mainDisplayInfo;
    double density, scale, widthScale, heightScale, scaledWidth, scaledHeight, width, height;
    bool isFirstTime = true;
    public CameraPage()
    {
        InitializeComponent();
        this.Disappearing += OnDisappearing;

        mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
        orientation = mainDisplayInfo.Orientation;
        rotation = mainDisplayInfo.Rotation;
        density = mainDisplayInfo.Density;
    }

    private void OnDisappearing(object sender, EventArgs e)
    {
        cameraView.ShowCameraView = false;
    }

    private void cameraView_FrameReady(object sender, FrameReadyEventArgs e)
    {
        // process image
        if (saveImage)
        {
            saveImage = false;
            byte[] buffer = (byte[])e.Buffer;
            int width = e.Width;
            int height = e.Height;
            int stride = e.Stride;
            PixelFormat format = e.Format;

            SKImageInfo info = new SKImageInfo(width, height, SKColorType.Gray8, SKAlphaType.Premul);

            // Create the SKBitmap
            SKBitmap bitmap = new SKBitmap(info);
            bitmap.SetPixels((Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0)));

            // Save the data to a file
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
            var appDataDirectory = FileSystem.AppDataDirectory;

            // Combine the app's data directory path with your file name
            var filePath = Path.Combine(appDataDirectory, "yourfile.jpg");

            using FileStream stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }
    }

    private void cameraView_ResultReady(object sender, ResultReadyEventArgs e)
    {
        lock (_lockObject)
        {
            if (e.Result != null)
            {
                imageWidth = e.PreviewWidth;
                imageHeight = e.PreviewHeight;
                if (orientation == DisplayOrientation.Portrait)
                {
                    widthScale = imageHeight / width;
                    heightScale = imageWidth / height;
                    scale = widthScale < heightScale ? widthScale : heightScale;
                    scaledWidth = imageHeight / scale;
                    scaledHeight = imageWidth / scale;
                }
                else
                {
                    widthScale = imageWidth / width;
                    heightScale = imageHeight / height;
                    scale = widthScale < heightScale ? widthScale : heightScale;
                    scaledWidth = imageWidth / scale;
                    scaledHeight = imageHeight / scale;
                }

                data = BarcodeQrData.Convert((Result[])e.Result);
                foreach (BarcodeQrData barcodeQrData in data)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (orientation == DisplayOrientation.Portrait)
                        {
                            barcodeQrData.points[i] = rotateCW90(barcodeQrData.points[i], imageHeight);
                        }

                        barcodeQrData.points[i].X = (float)(barcodeQrData.points[i].X / scale);
                        barcodeQrData.points[i].Y = (float)(barcodeQrData.points[i].Y / scale);
                    }
                }
            }
            else
            {
                data = null;
            }
        }
            

        MainThread.BeginInvokeOnMainThread(() =>
        {
            canvasView.InvalidateSurface();
        });
    }

    public static SKPoint rotateCW90(SKPoint point, int width)
    {
        SKPoint rotatedPoint = new SKPoint();
        rotatedPoint.X = width - point.Y;
        rotatedPoint.Y = point.X;
        return rotatedPoint;
    }

    void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
    {
        if (isFirstTime)
        {
            isFirstTime = false;
            width = canvasView.Width;
            height = canvasView.Height;

            width *= density;
            height *= density;
        }

        SKImageInfo info = args.Info;
        SKSurface surface = args.Surface;
        SKCanvas canvas = surface.Canvas;

        canvas.Clear();

        SKPaint skPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Blue,
            StrokeWidth = 10,
        };

        SKPaint textPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Red,
            TextSize = (float)(18 * density),
            StrokeWidth = 4,
        };

        lock (_lockObject)
        {
            if (data != null)
            {
                foreach (BarcodeQrData barcodeQrData in data)
                {
                    canvas.DrawText(barcodeQrData.text, barcodeQrData.points[0], textPaint);
                    canvas.DrawLine(barcodeQrData.points[0], barcodeQrData.points[1], skPaint);
                    canvas.DrawLine(barcodeQrData.points[1], barcodeQrData.points[2], skPaint);
                    canvas.DrawLine(barcodeQrData.points[2], barcodeQrData.points[3], skPaint);
                    canvas.DrawLine(barcodeQrData.points[3], barcodeQrData.points[0], skPaint);
                }
            }
        }
    }
}