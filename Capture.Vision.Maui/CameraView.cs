using System.Collections.ObjectModel;
using static Capture.Vision.Maui.CameraInfo;

namespace Capture.Vision.Maui
{
    public class ResultReadyEventArgs : EventArgs
    {
        public ResultReadyEventArgs(object result, int previewWidth, int previewHeight)
        {
            Result = result;
            PreviewWidth = previewWidth;
            PreviewHeight = previewHeight;
        }

        public object Result { get; private set; }
        public int PreviewWidth { get; private set; }
        public int PreviewHeight { get; private set; }

    }

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

    public class CameraView : View
    {
        public static readonly BindableProperty CamerasProperty = BindableProperty.Create(nameof(Cameras), typeof(ObservableCollection<CameraInfo>), typeof(CameraView), new ObservableCollection<CameraInfo>());
        public static readonly BindableProperty CameraProperty = BindableProperty.Create(nameof(Camera), typeof(CameraInfo), typeof(CameraView), null);
        public static readonly BindableProperty EnableBarcodeProperty = BindableProperty.Create(nameof(EnableBarcode), typeof(bool), typeof(CameraView), false);
        public static readonly BindableProperty ShowCameraViewProperty = BindableProperty.Create(nameof(ShowCameraView), typeof(bool), typeof(CameraView), false, propertyChanged: ShowCameraViewChanged);
        public event EventHandler<ResultReadyEventArgs> ResultReady;
        public event EventHandler<FrameReadyEventArgs> FrameReady;

        public ObservableCollection<CameraInfo> Cameras
        {
            get { return (ObservableCollection<CameraInfo>)GetValue(CamerasProperty); }
            set { SetValue(CamerasProperty, value); }
        }

        public CameraInfo Camera
        {
            get { return (CameraInfo)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public bool EnableBarcode
        {
            get { return (bool)GetValue(EnableBarcodeProperty); }
            set { SetValue(EnableBarcodeProperty, value); }
        }

        public bool ShowCameraView
        {
            get { return (bool)GetValue(ShowCameraViewProperty); }
            set { SetValue(ShowCameraViewProperty, value); }
        }

        public void NotifyResultReady(object result, int previewWidth, int previewHeight)
        {
            if (ResultReady != null)
            {
                ResultReady(this, new ResultReadyEventArgs(result, previewWidth, previewHeight));
            }
        }

        public void NotifyFrameReady(byte[] buffer, int width, int height, int stride, FrameReadyEventArgs.PixelFormat format)
        {
            if (FrameReady != null)
            {
                FrameReady(this, new FrameReadyEventArgs(buffer, width, height, stride, format));
            }
        }

        private static async void ShowCameraViewChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (oldValue != newValue && bindable is CameraView control)
            {
                try
                {
                    if ((bool)newValue)
                        await control.StartCameraAsync();
                    else
                        await control.StopCameraAsync();
                }
                catch { }
            }
        }

        public async Task<Status> StartCameraAsync()
        {
            Status result = Status.Unavailable;
            if (Camera != null)
            {
                if (Handler != null && Handler is CameraViewHandler handler)
                {
                    result = await handler.StartCameraAsync();
                }
            }

            return result;
        }

        public async Task<Status> StopCameraAsync()
        {
            Status result = Status.Unavailable;
            if (Handler != null && Handler is CameraViewHandler handler)
            {
                result = await handler.StopCameraAsync();
            }
            return result;
        }

        public void UpdateCameras()
        {
            Task.Run(() => {

                MainThread.BeginInvokeOnMainThread(() => {
                    if (Cameras.Count > 0)
                    {
                        Camera = Cameras.First();
                        ShowCameraView = true;
                        OnPropertyChanged(nameof(ShowCameraView));
                    }
                });

            });
        }
        public static async Task<bool> RequestPermissions()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted) return false;
            }
            return true;
        }
    }
}
