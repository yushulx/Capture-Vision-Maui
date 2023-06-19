namespace Capture.Vision.Maui
{
    public static class AppBuilderExtensions
    {
        public static MauiAppBuilder UseNativeCameraView(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers(h =>
            {
                h.AddHandler(typeof(CameraView), typeof(CameraViewHandler));
            });
            return builder;
        }
    }

}
