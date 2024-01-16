using static Dynamsoft.DocumentScanner;

namespace Capture.Vision.Maui
{
    public class DocumentResult
    {
        public int Confidence { get; set; }

        public int[] Points { get; set; }

        public NormalizedImage Image { get; set; }
    }
}
