namespace Capture.Vision.Maui.Example
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        async void OnTakeVideoButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CameraPage());
        }
    }
}