using Microsoft.Maui.Handlers;
using static Capture.Vision.Maui.CameraInfo;
#if IOS
using PlatformView = Capture.Vision.Maui.Platforms.iOS.NativeCameraView;
#elif ANDROID
using PlatformView = Capture.Vision.Maui.Platforms.Android.NativeCameraView;
#elif WINDOWS
using PlatformView = Capture.Vision.Maui.Platforms.Windows.NativeCameraView;
#else
using PlatformView = System.Object;
#endif

namespace Capture.Vision.Maui
{
    


    internal partial class CameraViewHandler : ViewHandler<CameraView, PlatformView>
    {
        public static IPropertyMapper<CameraView, CameraViewHandler> PropertyMapper = new PropertyMapper<CameraView, CameraViewHandler>(ViewMapper)
        {
        };
        public static CommandMapper<CameraView, CameraViewHandler> CommandMapper = new(ViewCommandMapper)
        {
        };
        public CameraViewHandler() : base(PropertyMapper, CommandMapper)
        {
        }

#if ANDROID
    protected override PlatformView CreatePlatformView() => new(Context, VirtualView);
#elif IOS || WINDOWS
        protected override PlatformView CreatePlatformView() => new(VirtualView);
#else
    protected override PlatformView CreatePlatformView() => new();
#endif
        protected override void ConnectHandler(PlatformView platformView)
        {
            base.ConnectHandler(platformView);
        }

        protected override void DisconnectHandler(PlatformView platformView)
        {
#if WINDOWS || IOS || ANDROID
            platformView.DisposeControl();
#endif
            base.DisconnectHandler(platformView);
        }

        public Task<Status> StartCameraAsync()
        {
            if (PlatformView != null)
            {
#if WINDOWS || ANDROID || IOS
                return PlatformView.StartCameraAsync();
#endif
            }
            return Task.Run(() => { return Status.Unavailable; });
        }

        public Task<Status> StopCameraAsync()
        {
            if (PlatformView != null)
            {
#if WINDOWS
            return PlatformView.StopCameraAsync();
#elif ANDROID || IOS
                var task = new Task<Status>(() => { return PlatformView.StopCamera(); });
                task.Start();
                return task;
#endif
            }
            return Task.Run(() => { return Status.Unavailable; });
        }
    }

}
