# .NET MAUI Camera View with Dynamsoft Vision SDKs
The project's goal is to assist developers in creating .NET MAUI applications featuring a custom camera view. It utilizes [Dynamsoft Vision SDKs](https://www.dynamsoft.com/) for barcode, MRZ (Machine Readable Zone), and document detection.

## Example
[https://github.com/yushulx/Capture-Vision-Maui/tree/main/Capture.Vision.Maui.Example](https://github.com/yushulx/Capture-Vision-Maui/tree/main/Capture.Vision.Maui.Example)

![.NET MAUI Windows QR code scanner](https://camo.githubusercontent.com/5b212f793f3ae53c7d2d2ba926f9edafeb3c117b9f63b2ea2ab668cc8938732f/68747470733a2f2f7777772e64796e616d736f66742e636f6d2f636f6465706f6f6c2f696d672f323032342f30312f646f746e65742d6d6175692d626172636f64652d646f63756d656e742d6d727a2e706e67)

## Demo Video: .NET MAUI QR Code Scanner

- Windows

  [https://github.com/yushulx/Capture-Vision-Maui/assets/2202306/df6ce0d1-93b8-4e26-be6a-cfe82ba3d267](https://github.com/yushulx/Capture-Vision-Maui/assets/2202306/df6ce0d1-93b8-4e26-be6a-cfe82ba3d267)

- Android

  [https://github.com/yushulx/Capture-Vision-Maui/assets/2202306/73551440-6720-4912-8605-cee9882bbee2](https://github.com/yushulx/Capture-Vision-Maui/assets/2202306/73551440-6720-4912-8605-cee9882bbee2)

## Supported Platforms
- Android
- iOS
- Windows

## Features
- Read 1D barcodes, QR codes, PDF417, DataMatrix, and other formats from camera frames.
- Recognize Machine Readable Zones (MRZ) from camera frames (**Windows & Android**).
- Detect document edges within camera frames (**Windows & Android**).

## Getting Started
1. Enable the camera view in `MauiProgram.cs`:

    ```csharp
    builder.UseNativeCameraView()
    ```

2. Request a [free trial license](https://www.dynamsoft.com/customer/license/trialLicense) and replace `LICENSE-KEY` with your own license key.
    
    **Windows App.xaml.cs**

    ```csharp
    using Dynamsoft;
    using Microsoft.UI.Xaml;
    namespace Capture.Vision.Maui.Example.WinUI
    {
        public partial class App : MauiWinUIApplication
        {
            public App()
            {
                this.InitializeComponent();
                BarcodeQRCodeReader.InitLicense("LICENSE-KEY");
                DocumentScanner.InitLicense("LICENSE-KEY");
                MrzScanner.InitLicense("LICENSE-KEY");
            }
    
            protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        }
    }
    ```

    **Android MainActivity.cs**

    ```csharp
    using Android.App;
    using Android.Content.PM;
    using Android.OS;
    using Dynamsoft;
    
    namespace Capture.Vision.Maui.Example
    {
        [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
        public class MainActivity : MauiAppCompatActivity
        {
            protected override void OnCreate(Bundle savedInstanceState)
            {
                base.OnCreate(savedInstanceState);
    
                // Your platform-specific code here
                BarcodeQRCodeReader.InitLicense("LICENSE-KEY");
                MrzScanner.InitLicense("LICENSE-KEY", this);
            }
        }
    }
    ```

3. Create a content page to add the camera view:

    ```xml
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
                VerticalOptions="FillAndExpand" EnableBarcode="True" EnableDocumentDetect="True" EnableMrz="True"
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

    Set `EnableBarcode`, `EnableDocumentDetect`, and `EnableMrz` to activate barcode, document, and MRZ (Machine Readable Zone) detection, respectively. Use the `cameraView_ResultReady` event to retrieve the results.

    ```csharp
    private void cameraView_ResultReady(object sender, ResultReadyEventArgs e)
    {
        if (e.Result is BarcodeResult[])
        {}
        else if (e.Result is DocumentResult)
        {}
        else if (e.Result is MrzResult)
        {}    
    }
    ```

## Custom Image Processing
In the `cameraView_FrameReady` event, you can access the camera frame for custom image processing. 

```csharp
private void cameraView_FrameReady(object sender, FrameReadyEventArgs e)
{
    // process image
}
```

The `FrameReadyEventArgs` provide the image buffer, width, height, stride and format.

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

Currently, frames captured from Windows and Android devices are in the `GRAYSCALE` format, whereas frames from iOS devices use the `BGRA8888` format.


## Barcode Parameters Configuration
Configure the barcode detection parameters to suit specific scenarios and requirements. This includes adjusting settings for barcode types, scanning precision, and other relevant factors.
    
```csharp
public CameraPage()
{
    InitializeComponent();
    ...
    // cameraView.BarcodeParameters = "{\"Version\":\"3.0\", \"ImageParameter\":{\"Name\":\"IP1\", \"BarcodeFormatIds\":[\"BF_QR_CODE\", \"BF_ONED\"], \"ExpectedBarcodesCount\":20}}";
}
```

## References
- Camera: [https://github.com/hjam40/Camera.MAUI](https://github.com/hjam40/Camera.MAUI)
- Barcode: [https://github.com/yushulx/dotnet-barcode-qr-code-sdk](https://github.com/yushulx/dotnet-barcode-qr-code-sdk)
- Capture Vision: [https://github.com/yushulx/Capture-Vision](https://github.com/yushulx/Capture-Vision)


## Building NuGet Package from Source Code

```bash
cd Capture.Vision.Maui
dotnet build --configuration Release
```