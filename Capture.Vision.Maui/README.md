# .NET MAUI Camera View with Dynamsoft Vision SDKs
The project aims to help developers build .NET MAUI apps that feature a custom camera view, utilizing [Dynamsoft](https://www.dynamsoft.com/) Vision SDKs for barcode, MRZ, and document detection.

![.NET MAUI Windows QR code scanner](https://www.dynamsoft.com/codepool/img/2023/06/dotnet-maui-windows-qr-code-scanner.png)

## Supported Platforms
- Android
- iOS
- Windows

## Features
- Recognize 1D barcodes, QR codes, PDF417, DataMatrix, and more from camera frames.

## Getting Started
1. Enable the camera view in `MauiProgram.cs`:

    ```csharp
    builder.UseNativeCameraView()
    ```

2. Request a [free trial license](https://www.dynamsoft.com/customer/license/trialLicense?product=dbr) and replace `LICENSE-KEY` with your own license key in `MainPage.xaml.cs`:

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
                    BarcodeQRCodeReader.InitLicense("LICENSE-KEY");
    
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

    If you set `EnableBarcode` to `True`, the barcode result will be available in the `cameraView_ResultReady` event:

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
    
    If you wish to obtain the camera frame for independent image processing, set `EnableBarcode` to False. The frame will then be accessible in the `cameraView_FrameReady` event.

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

    Currently, the frame from Windows and Android devices is in the `GRAYSCALE` format, while the frame from iOS devices is in the `BGRA8888` format.

4. Specify barcode detection parameters according to the requirements of specific scenarios.
    
    ```csharp
    public CameraPage()
    {
        InitializeComponent();
        ...
        // cameraView.BarcodeParameters = "{\"Version\":\"3.0\", \"ImageParameter\":{\"Name\":\"IP1\", \"BarcodeFormatIds\":[\"BF_QR_CODE\", \"BF_ONED\"], \"ExpectedBarcodesCount\":20}}";
    }
    ```
    
    

## TODO
- MRZ detection
- Document detection

## References
- Camera: [https://github.com/hjam40/Camera.MAUI](https://github.com/hjam40/Camera.MAUI)
- Barcode: [https://github.com/yushulx/dotnet-barcode-qr-code-sdk](https://github.com/yushulx/dotnet-barcode-qr-code-sdk)