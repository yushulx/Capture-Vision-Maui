using static Dynamsoft.DocumentScanner;

namespace Capture.Vision.Maui
{
    public class DocumentResult
    {
        public int Confidence { get; set; }

        public int[] Points { get; set; }

        public int Width;

        public int Height;

        public int Stride;

        public ImagePixelFormat Format;

        public byte[] Data { get; set; }

        public byte[] Binary2Grayscale()
        {
            byte[] array = new byte[Width * Height];
            int num = 0;
            int num2 = Stride * 8 - Width;
            int num3 = 0;
            int num4 = 1;
            byte[] data = Data;
            foreach (byte b in data)
            {
                int num5 = 7;
                while (num5 >= 0)
                {
                    int num6 = (b & (1 << num5)) >> num5;
                    if (num3 < Stride * 8 * num4 - num2)
                    {
                        if (num6 == 1)
                        {
                            array[num] = byte.MaxValue;
                        }
                        else
                        {
                            array[num] = 0;
                        }

                        num++;
                    }

                    num5--;
                    num3++;
                }

                if (num3 == Stride * 8 * num4)
                {
                    num4++;
                }
            }

            return array;
        }
    }
}
