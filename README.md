# .NET MAUI Camera View with Dynamsoft Vision SDKs
This project helps developers create .NET MAUI applications featuring a custom camera view using [Dynamsoft Vision SDKs](https://www.dynamsoft.com/). These SDKs provide capabilities for barcode, MRZ (Machine Readable Zone), and document detection.

## Example
[https://github.com/yushulx/Capture-Vision-Maui/tree/main/Capture.Vision.Maui.Example](https://github.com/yushulx/Capture-Vision-Maui/tree/main/Capture.Vision.Maui.Example)

- Windows

    ![.NET MAUI Windows QR code scanner](https://camo.githubusercontent.com/ae08b9a3805d127862780ccfc20a695c0c9f9c69719c92a285c06c9e10173d37/68747470733a2f2f7777772e64796e616d736f66742e636f6d2f636f6465706f6f6c2f696d672f323032332f30362f646f746e65742d6d6175692d77696e646f77732d71722d636f64652d7363616e6e65722e706e67)

- iOS
    
    ![.NET MAUI iOS: detect barcode, document and mrz](https://camo.githubusercontent.com/c05b55c16a669c49d18eb2f7e8e0a7f45814ee230e4520a01d897cb275e1d109/68747470733a2f2f7777772e64796e616d736f66742e636f6d2f636f6465706f6f6c2f696d672f323032342f30332f646f746e65742d6d6175692d696f732d626172636f64652d646f63756d656e742d6d727a2e706e67)

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
- Scan 1D barcodes, QR codes, PDF417, DataMatrix, and other barcode formats.
- Recognize Machine Readable Zones (MRZ) from camera frames.
- Detect document edges in real-time.

## Getting Started
1. Enable the camera view in `MauiProgram.cs`:

    ```csharp
    builder.UseNativeCameraView()
    ```

2. Request a [free trial license](https://www.dynamsoft.com/customer/license/trialLicense/?product=dcv&package=cross-platform) and replace `LICENSE-KEY` with your own license key in the platform-specific code.
    
    **App.xaml.cs for Windows:**

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
        }
    }
    ```

    **MainActivity.cs for Android:**

    ```csharp
    using Android.App;
    using Android.Content.PM;
    using Android.OS;
    using Dynamsoft;
    
    namespace Capture.Vision.Maui.Example
    {
        public class MainActivity : MauiAppCompatActivity
        {
            protected override void OnCreate(Bundle savedInstanceState)
            {
                base.OnCreate(savedInstanceState);
    
                // Your platform-specific code here
                BarcodeQRCodeReader.InitLicense("LICENSE-KEY");
                DocumentScanner.InitLicense("LICENSE-KEY");
                MrzScanner.InitLicense("LICENSE-KEY");
            }
        }
    }
    ```

    **Program.cs for iOS:**

    ```csharp
    using ObjCRuntime;
    using UIKit;
    using Dynamsoft;

    static void Main(string[] args)
    {
        DocumentScanner.InitLicense("LICENSE-KEY");
        BarcodeQRCodeReader.InitLicense("LICENSE-KEY");
        MrzScanner.InitLicense("LICENSE-KEY");
    
        UIApplication.Main(args, null, typeof(AppDelegate));
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
In the `cameraView_FrameReady` event, access the camera frame for custom image processing:

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
Customize barcode detection parameters to fit specific requirements, such as supported barcode formats and expected counts:
    
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
