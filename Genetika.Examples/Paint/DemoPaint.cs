using Genetika.Examples.Demonstration;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Genetika.Examples.Paint
{
    public class DemoPaint : Demo<ArtistEntity>
    {
        private static int runs = 0;

        public DemoPaint(int steps, GenetikaParameters parameters)
            : base(ArtistEntity.NumberOfInputs, ArtistEntity.NumberOfOutputs, steps, parameters)
        {
        }

        public override float Run()
        {
            var result = base.Run();
            var elites = this.Genome.GetElites(10);
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "paintings");
            Directory.CreateDirectory(dir);

            for (int i = 0; i < elites.Count; i++)
            {
                var elite = elites[i];
                var bitmap = ToBitmap(elite.entity.canvas);
                bitmap.Save(Path.Combine(dir, $"{runs}_{i}.png"), ImageFormat.Png);
                bitmap.Dispose();
            }
            runs++;
            return result;
        }

        /// <summary>
        /// Converts the RGBA canvas to a full-color bitmap with a subtle paper-toned background.
        /// </summary>
        public static Bitmap ToBitmap(byte[][][] data)
        {
            int size = CanvasEntity.canvasSize;
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);

            // Warm parchment background
            byte bgR = 245, bgG = 240, bgB = 230;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    byte r = data[x][y][0];
                    byte g = data[x][y][1];
                    byte b = data[x][y][2];
                    byte a = data[x][y][3];

                    // Composite paint over the paper background
                    float alpha = a / 255f;
                    byte finalR = (byte)(r * alpha + bgR * (1f - alpha));
                    byte finalG = (byte)(g * alpha + bgG * (1f - alpha));
                    byte finalB = (byte)(b * alpha + bgB * (1f - alpha));

                    bmp.SetPixel(x, y, Color.FromArgb(255, finalR, finalG, finalB));
                }
            }

            return bmp;
        }
    }
}
