# .NET MAUI Camera View with Dynamsoft Vision SDKs
The project aims to help developers build .NET MAUI apps with a custom camera view and [Dynamsoft](https://www.dynamsoft.com/) Vision SDKs, including barcode, MRZ, and document detection. 

![.NET MAUI Windows QR code scanner](https://www.dynamsoft.com/codepool/img/2023/06/dotnet-maui-windows-qr-code-scanner.png)

## Supported Platforms
- Android
- iOS
- Windows

## Features
- Recognize 1D barcode, QR code, PDF417, DataMatrix, and etc from camera frames

## Getting Started
Enable the camera view in `MauiProgram.cs`:
```csharp
builder.UseNativeCameraView()
```

Apply for a [trial license](https://www.dynamsoft.com/customer/license/trialLicense) to activate the Dynamsoft Vision SDKs in `MainPage.xaml.cs`:
```csharp
using Dynamsoft;

namespace Capture.Vision.Maui.Example
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            InitService();
        }

        private async void InitService()
        {
            await Task.Run(() =>
            {
                BarcodeQRCodeReader.InitLicense("DLS2eyJoYW5kc2hha2VDb2RlIjoiMjAwMDAxLTE2NDk4Mjk3OTI2MzUiLCJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSIsInNlc3Npb25QYXNzd29yZCI6IndTcGR6Vm05WDJrcEQ5YUoifQ==");

                return Task.CompletedTask;
            });
        }

        async void OnTakeVideoButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CameraPage());
        }
    }
}
```

Create a content page to add the camera view:
```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
            xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
            xmlns:skia="clr-namespace:SkiaSharp.Views.Maui.Controls;assembly=SkiaSharp.Views.Maui.Controls"
            xmlns:cv="clr-namespace:Capture.Vision.Maui;assembly=Capture.Vision.Maui"
            x:Class="Capture.Vision.Maui.Example.CameraPage"
            Title="CameraPage">
    <ScrollView>
        <Grid>
            <cv:CameraView x:Name="cameraView" HorizontalOptions="FillAndExpand"
            VerticalOptions="FillAndExpand" EnableBarcode="True" 
                                ResultReady="cameraView_ResultReady" FrameReady="cameraView_FrameReady"
                            />
            <skia:SKCanvasView x:Name="canvasView" 
                        Margin="0"
                        HorizontalOptions="FillAndExpand" VerticalOptions="FillAndExpand"
                        PaintSurface="OnCanvasViewPaintSurface" />
        </Grid>
    </ScrollView>
</ContentPage>
```

If you set `EnableBarcode` to `True`, you can get the barcode result in `cameraView_ResultReady`:
```csharp
private void cameraView_ResultReady(object sender, ResultReadyEventArgs e)
{
    if (e.Result != null)
    {
        Result[] results = (Result[])e.Result;
        foreach (Result result in results)
        {
            System.Diagnostics.Debug.WriteLine(result.Text);
        }
    }
}
```

If you only want to get the camera frame and do image processing by yourself, you can set `EnableBarcode` to `False` and get the frame in `cameraView_FrameReady`:
```csharp
private void cameraView_FrameReady(object sender, FrameReadyEventArgs e)
{
    // process image
}
```

The `FrameReadyEventArgs` contains the image data and the image format:
```csharp
public class FrameReadyEventArgs : EventArgs
{ 
    public enum PixelFormat
    {
        GRAYSCALE,
        RGB888,
        BGR888,
        RGBA8888,
        BGRA8888,
    }
    public FrameReadyEventArgs(byte[] buffer, int width, int height, int stride, PixelFormat pixelFormat)
    {
        Buffer = buffer;
        Width = width;
        Height = height;
        Stride = stride;
        Format = pixelFormat;
    }

    public byte[] Buffer { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Stride { get; private set; }
    public PixelFormat Format { get; private set; }
}
```

Currently, the frame from Windows and Android is in `GRAYSCALE` format, and the frame from iOS is in `BGRA8888` format. 


## TODO
- MRZ detection
- Document detection

## References
- Camera: [https://github.com/hjam40/Camera.MAUI](https://github.com/hjam40/Camera.MAUI)
- Barcode: [https://github.com/yushulx/dotnet-barcode-qr-code-sdk](https://github.com/yushulx/dotnet-barcode-qr-code-sdk)