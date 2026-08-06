using AeroScenery.Common;
using log4net;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AeroScenery.ImageProcessing
{
    internal class OsmBasedMasking
    {
        public Bitmap OsmImageMasking(string baseImagePath, string osmImagePath, int maskingRange)
        {
            maskingRange++;

            var detector = new OsmFeatureDetector();
            var matrix = detector.DetectFeatures(osmImagePath);

            var transparentFeatures = new HashSet<OsmFeatureType>
            {
                OsmFeatureType.Water1,
                OsmFeatureType.Water2,
                OsmFeatureType.Water3,
                OsmFeatureType.Building1,
                OsmFeatureType.Building2,
                OsmFeatureType.Road1,
                OsmFeatureType.Road2,
                OsmFeatureType.Runway
            };

            Bitmap maskedImage = new Bitmap(baseImagePath); 

            Rectangle rect = new Rectangle(0, 0, maskedImage.Width, maskedImage.Height);
            BitmapData bmpData = maskedImage.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                /*
                for (int y = 0; y < maskedImage.Height; y++)
                {
                    byte* row = ptr + (y * stride);
                    for (int x = 0; x < maskedImage.Width; x++)
                    {
                        if (transparentFeatures.Contains(matrix.GetFeature(x, y)))
                        {
                            byte* pixel = row + x * 4;
                            pixel[3] = 0;
                        }
                    }
                }
                */


                for (int y = 0; y < maskedImage.Height; y++)
                {
                    for (int x = 0; x < maskedImage.Width; x++)
                    {
                        if (transparentFeatures.Contains(matrix.GetFeature(x, y)))
                        {
                            int maskingFeatureRange = maskingRange;
                            //# Set maskingFeatureRange to 0 if the feature is a building (means just mask the building itself without range)
                            if ((matrix.GetFeature(x, y) == OsmFeatureType.Building1) || matrix.GetFeature(x, y) == OsmFeatureType.Building2)
                            {
                                maskingFeatureRange = 0;
                            }

                            for (int dy = -maskingFeatureRange; dy <= maskingFeatureRange; dy++)
                            {
                                for (int dx = -maskingFeatureRange; dx <= maskingFeatureRange; dx++)
                                {
                                    int nx = x + dx;
                                    int ny = y + dy;
                                    if (nx < 0 || ny < 0 || nx >= maskedImage.Width || ny >= maskedImage.Height)
                                        continue;

                                    double distance = Math.Sqrt(dx * dx + dy * dy);
                                    if (distance > maskingFeatureRange)
                                        continue;

                                    byte* pixel = (byte*)ptr + ny * stride + nx * 4;
                                    pixel[3] = 0;
                                }
                            }
                        }
                    }
                }

            }

            maskedImage.UnlockBits(bmpData);
            return maskedImage; 
        }

    }
}
