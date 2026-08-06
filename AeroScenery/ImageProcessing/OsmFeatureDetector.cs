using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.ImageProcessing
{
    //#MOD_k
    internal class OsmFeatureDetector
    {
        private Dictionary<OsmFeatureType, Color> featureColors;

        public OsmFeatureDetector()
        {
            featureColors = new Dictionary<OsmFeatureType, Color>();
            featureColors.Add(OsmFeatureType.Water1, Color.FromArgb(212, 218, 220));
            featureColors.Add(OsmFeatureType.Water2, Color.FromArgb(209, 219, 223));
            featureColors.Add(OsmFeatureType.Water3, Color.FromArgb(212, 221, 225));
            featureColors.Add(OsmFeatureType.Road1, Color.FromArgb(254, 254, 254));
            featureColors.Add(OsmFeatureType.Road2, Color.FromArgb(252, 252, 252));
            featureColors.Add(OsmFeatureType.Building1, Color.FromArgb(237, 237, 237));
            featureColors.Add(OsmFeatureType.Building2, Color.FromArgb(226, 223, 224));
            featureColors.Add(OsmFeatureType.Forest, Color.FromArgb(238, 243, 239));
            featureColors.Add(OsmFeatureType.Runway, Color.FromArgb(232, 232, 232));
        }

        public OsmFeatureMatrix DetectFeatures(string imagePath, int tolerance = 1)
        {

            using (Bitmap osmmap = new Bitmap(imagePath))
            {
                int width = osmmap.Width;
                int height = osmmap.Height;
                var matrix = new OsmFeatureMatrix(width, height);

                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData bmpData = osmmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                int stride = bmpData.Stride;
                IntPtr scan0 = bmpData.Scan0;

                unsafe
                {
                    byte* ptr = (byte*)scan0.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + (y * stride);

                        for (int x = 0; x < width; x++)
                        {
                            byte b = row[x * 3 + 0];
                            byte g = row[x * 3 + 1];
                            byte r = row[x * 3 + 2];

                            foreach (var kvp in featureColors)
                            {
                                Color target = kvp.Value;
                                if (IsWithinTolerance(r, g, b, target, tolerance))
                                {
                                    matrix.SetFeature(x, y, kvp.Key);
                                    break;
                                }
                            }
                        }
                    }
                }

                osmmap.UnlockBits(bmpData);
                return matrix;
            }

        }
        private bool IsWithinTolerance(byte r, byte g, byte b, Color target, int tolerance)
        {
            return
                Math.Abs(r - target.R) <= tolerance &&
                Math.Abs(g - target.G) <= tolerance &&
                Math.Abs(b - target.B) <= tolerance;
        }

    }

}
