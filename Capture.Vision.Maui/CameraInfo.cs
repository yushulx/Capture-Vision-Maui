namespace Capture.Vision.Maui
{
    public class CameraInfo
    {
        public enum Position
        {
            Back,
            Front,
            Unknown
        }

        public enum Status
        {
            Available,
            Unavailable
        }

        public string Name { get; internal set; }
        public string DeviceId { get; internal set; }
        public Position Pos { get; internal set; }

        public List<Size> AvailableResolutions { get; internal set; }
        public override string ToString()
        {
            return Name;
        }
    }

}
