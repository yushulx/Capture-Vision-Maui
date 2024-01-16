using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Runtime.InteropServices;
using static Capture.Vision.Maui.FrameReadyEventArgs;

namespace Capture.Vision.Maui.Example;

public class BarcodeQrData
{
    public BarcodeResult Reference { get; set; }
    public SKPoint[] Points;
}

public class DocumentData
{
    public DocumentResult Reference { get; set; }
    public SKPoint[] Points;
}

public class MrzData
{
    public MrzResult Reference { get; set; }
    public SKPoint[] Points;
}

public partial class CameraPage : ContentPage
{
    BarcodeQrData[] barcodeData = null;
    DocumentData documentData = null;
    MrzResult mrzData = null;
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
        //cameraView.BarcodeParameters = "{\"Version\":\"3.0\", \"ImageParameter\":{\"Name\":\"IP1\", \"BarcodeFormatIds\":[\"BF_QR_CODE\", \"BF_ONED\"], \"ExpectedBarcodesCount\":20}}";
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

                if (e.Result is BarcodeResult[])
                {
                    barcodeData = null;
                    BarcodeResult[] barcodeResults = (BarcodeResult[])e.Result;

                    if (barcodeResults != null && barcodeResults.Length > 0)
                    {
                        barcodeData = new BarcodeQrData[barcodeResults.Length];

                        for (int index = 0; index < barcodeResults.Length; ++index)
                        {
                            barcodeData[index] = new BarcodeQrData()
                            {
                                Reference = barcodeResults[index]
                            };
                            int[] coordinates = barcodeResults[index].Points;
                            if (coordinates != null && coordinates.Length == 8)
                            {
                                barcodeData[index].Points = new SKPoint[4];

                                for (int i = 0; i < 4; ++i)
                                {
                                    SKPoint p = new SKPoint();
                                    p.X = coordinates[i * 2];
                                    p.Y = coordinates[i * 2 + 1];
                                    barcodeData[index].Points[i] = p;

                                    if (orientation == DisplayOrientation.Portrait)
                                    {
                                        barcodeData[index].Points[i] = rotateCW90(barcodeData[index].Points[i], imageHeight);
                                    }

                                    barcodeData[index].Points[i].X = (float)(barcodeData[index].Points[i].X / scale);
                                    barcodeData[index].Points[i].Y = (float)(barcodeData[index].Points[i].Y / scale);
                                }
                            }
                        }
                    }
                }
                else if (e.Result is DocumentResult)
                {
                    documentData = null;
                    DocumentResult documentResult = (DocumentResult)e.Result;

                    if (documentResult != null)
                    {
                        documentData = new DocumentData()
                        {
                            Reference = (DocumentResult)e.Result
                        };
                        int[] coordinates = documentData.Reference.Points;
                        if (coordinates != null && coordinates.Length == 8)
                        {
                            documentData.Points = new SKPoint[4];

                            for (int i = 0; i < 4; ++i)
                            {
                                SKPoint p = new SKPoint();
                                p.X = coordinates[i * 2];
                                p.Y = coordinates[i * 2 + 1];
                                documentData.Points[i] = p;

                                if (orientation == DisplayOrientation.Portrait)
                                {
                                    documentData.Points[i] = rotateCW90(documentData.Points[i], imageHeight);
                                }

                                documentData.Points[i].X = (float)(documentData.Points[i].X / scale);
                                documentData.Points[i].Y = (float)(documentData.Points[i].Y / scale);
                            }
                        }
                    }
                }
                else if (e.Result is MrzResult)
                {
                    mrzData = null;
                    MrzResult mrzResult = (MrzResult)e.Result;

                    if (mrzResult != null)
                    {

                    }
                }
                
            }
            else
            {
                barcodeData = null;
                documentData = null;
                mrzData = null;
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

        lock (_lockObject)
        {
            if (barcodeData != null)
            {
                SKPaint skPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Blue,
                    StrokeWidth = 10,
                };

                SKPaint textPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Blue,
                    TextSize = (float)(18 * density),
                    StrokeWidth = 4,
                };

                foreach (BarcodeQrData barcodeQrData in barcodeData)
                {
                    canvas.DrawText(barcodeQrData.Reference.Text, barcodeQrData.Points[0], textPaint);
                    canvas.DrawLine(barcodeQrData.Points[0], barcodeQrData.Points[1], skPaint);
                    canvas.DrawLine(barcodeQrData.Points[1], barcodeQrData.Points[2], skPaint);
                    canvas.DrawLine(barcodeQrData.Points[2], barcodeQrData.Points[3], skPaint);
                    canvas.DrawLine(barcodeQrData.Points[3], barcodeQrData.Points[0], skPaint);
                }
            }

            if (documentData != null)
            {
                SKPaint skPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Red,
                    StrokeWidth = 10,
                };

                SKPaint textPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Red,
                    TextSize = (float)(18 * density),
                    StrokeWidth = 4,
                };

                canvas.DrawText("Detected Document", documentData.Points[0], textPaint);
                canvas.DrawLine(documentData.Points[0], documentData.Points[1], skPaint);
                canvas.DrawLine(documentData.Points[1], documentData.Points[2], skPaint);
                canvas.DrawLine(documentData.Points[2], documentData.Points[3], skPaint);
                canvas.DrawLine(documentData.Points[3], documentData.Points[0], skPaint);
            }

            if (mrzData != null)
            {
                SKPaint skPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Yellow,
                    StrokeWidth = 10,
                };

                SKPaint textPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Yellow,
                    TextSize = (float)(18 * density),
                    StrokeWidth = 4,
                };
            }
        }
    }
}