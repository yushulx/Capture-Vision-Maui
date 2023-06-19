using Dynamsoft;
using SkiaSharp;
using SkiaSharp.Views.Maui;
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

    public CameraPage()
    {
        InitializeComponent();
        this.Disappearing += OnDisappearing;
    }

    private void OnDisappearing(object sender, EventArgs e)
    {
        cameraView.ShowCameraView = false;
    }

    private void cameraView_FrameReady(object sender, FrameReadyEventArgs e)
    {
        // process image
    }

    private void cameraView_ResultReady(object sender, ResultReadyEventArgs e)
    {
        if (e.Result != null)
        {
            Result[] results = (Result[])e.Result;
            foreach (Result result in results)
            {
                System.Diagnostics.Debug.WriteLine(result.Text);
            }
            data = BarcodeQrData.Convert((Result[])e.Result);


        }
        imageWidth = e.PreviewWidth;
        imageHeight = e.PreviewHeight;

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
        double width = canvasView.Width;
        double height = canvasView.Height;

        var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
        var orientation = mainDisplayInfo.Orientation;
        var rotation = mainDisplayInfo.Rotation;
        var density = mainDisplayInfo.Density;

        width *= density;
        height *= density;

        double scale, widthScale, heightScale, scaledWidth, scaledHeight;

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

        if (data != null)
        {
            foreach (BarcodeQrData barcodeQrData in data)
            {
                //ResultLabel.Text += barcodeQrData.text + "\n";

                for (int i = 0; i < 4; i++)
                {
                    if (orientation == DisplayOrientation.Portrait)
                    {
                        barcodeQrData.points[i] = rotateCW90(barcodeQrData.points[i], imageHeight);
                    }

                    if (widthScale < heightScale)
                    {
                        barcodeQrData.points[i].X = (float)(barcodeQrData.points[i].X / scale);
                        barcodeQrData.points[i].Y = (float)(barcodeQrData.points[i].Y / scale - (scaledHeight - height) / 2);
                    }
                    else
                    {
                        barcodeQrData.points[i].X = (float)(barcodeQrData.points[i].X / scale - (scaledWidth - width) / 2);
                        barcodeQrData.points[i].Y = (float)(barcodeQrData.points[i].Y / scale);
                    }
                }

                canvas.DrawText(barcodeQrData.text, barcodeQrData.points[0], textPaint);
                canvas.DrawLine(barcodeQrData.points[0], barcodeQrData.points[1], skPaint);
                canvas.DrawLine(barcodeQrData.points[1], barcodeQrData.points[2], skPaint);
                canvas.DrawLine(barcodeQrData.points[2], barcodeQrData.points[3], skPaint);
                canvas.DrawLine(barcodeQrData.points[3], barcodeQrData.points[0], skPaint);
            }
        }

    }
}