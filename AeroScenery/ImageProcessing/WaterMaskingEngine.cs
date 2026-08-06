using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//#MOD_k
namespace AeroScenery.ImageProcessing
{
    public class WaterMaskingEngine
    {
        private int fadeThreshold;
        private int hardThreshold;

        public WaterMaskingEngine(int fadeThreshold, int hardThreshold)
        {
            this.fadeThreshold = fadeThreshold;
            this.hardThreshold = hardThreshold;
        }

        public unsafe void ApplyWaterMask(byte* imagePtr, int width, int height, int stride,
                                    OsmFeatureMatrix features, int[,] distanceMatrix)
        {
            for (int y = 0; y < height; y++)
            {
                byte* row = imagePtr + (y * stride);

                for (int x = 0; x < width; x++)
                {
                    var feature = features.GetFeature(x, y);
                    if (!IsWater(feature))
                        continue;

                    int distance = distanceMatrix[x, y];
                    byte* pixel = row + x * 4;
                    byte b = pixel[0];
                    byte g = pixel[1];
                    byte r = pixel[2];

                    //var rgbBrightness = (b + g + r) / 3; // Durchschnittswert RGB

                    // 1.
                    /*
                    if ((rgbBrightness > 200) || (rgbBrightness < 5))
                    {
                        //
                        b = Convert.ToByte((30 + b) / 2);  // B
                        g = Convert.ToByte((20 + g) / 2);  // G
                        r = Convert.ToByte((10 + r) / 2);  // R

                    }
                    else
                    {
                    */
                        b = Convert.ToByte((30 + b * 3) / 4);  // B
                        g = Convert.ToByte((20 + g * 3) / 4);  // G
                        r = Convert.ToByte((10 + r * 3) / 4);  // R
                    //}

                    // 2.
                    if (distance < fadeThreshold)
                    {
                        // Keine zusätzliche Maskierung – Küstennähe
                        pixel[0] = b;
                        pixel[1] = g;
                        pixel[2] = r;
                        continue;
                    }
                    else if (distance < hardThreshold)
                    {
                        double blend = (distance - fadeThreshold) / (double)(hardThreshold - fadeThreshold);
                        pixel[0] = (byte)((1 - blend) * b + blend * 30); // B
                        pixel[1] = (byte)((1 - blend) * g + blend * 20); // G
                        pixel[2] = (byte)((1 - blend) * r + blend * 10); // R

                        /*
                        //for Testing purposes, colorizing with blue color
                        double blend = (distance - fadeThreshold) / (double)(hardThreshold - fadeThreshold);
                        pixel[0] = (byte)((1 - blend) * b + blend * 0); // B
                        pixel[1] = (byte)((1 - blend) * g + blend * 0); // G
                        pixel[2] = (byte)(((1 - blend) * r + blend * 255)/1.5 + 40); // R
                        */


                    }
                    else
                    {
                        // Volle Maskierung → Wasserfarbe 10/20/30
                        pixel[0] = 30;
                        pixel[1] = 20;
                        pixel[2] = 10;

                        /*
                        //for Testing purposes, colorizing with red color
                        pixel[0] = 0;
                        pixel[1] = 0;
                        pixel[2] = 255;
                        */
                        
                    }
                }
            }
        }

        private bool IsWater(OsmFeatureType feature)
        {
            return feature == OsmFeatureType.Water1 ||
                   feature == OsmFeatureType.Water2 ||
                   feature == OsmFeatureType.Water3;
        }
    }
}
