using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using log4net;

namespace AeroScenery.ImageProcessing
{
    public class OsmMaskingBatchProcessor
    {
        private static readonly ILog log = LogManager.GetLogger("AeroScenery");

        public static void RunMaskingBatch(string inImagesDirectory, string maskingImagesDirectory, string maskedImagesDirectory, int maskingRange)
        {
            // Sicherstellen, dass Output-Folder existiert
            Directory.CreateDirectory(maskedImagesDirectory);

            var imageFiles = Directory.GetFiles(inImagesDirectory, "*.png");

            foreach (var baseImagePath in imageFiles)
            {
                string fileName = Path.GetFileName(baseImagePath);
                
                string[] parts = fileName.Split(new[] { '_' }, 2);

                if (parts.Length < 2) continue; // Safety Check

                string osmFileName = "c-mask_" + parts[1]; // Replace prefix
                string osmImagePath = Path.Combine(maskingImagesDirectory, osmFileName);
                string outputImagePath = Path.Combine(maskedImagesDirectory, fileName);

                string aeroFileName = Path.ChangeExtension(fileName, ".aero"); // Replace extension
                string sourceAeroPath = Path.Combine(inImagesDirectory, aeroFileName);
                string targetAeroPath = Path.Combine(maskedImagesDirectory, aeroFileName);

                // Copy if destination file doesn't exist yet
                if (File.Exists(sourceAeroPath) && !File.Exists(targetAeroPath))
                {
                    File.Copy(sourceAeroPath, targetAeroPath);
                }

                // Check if the output file already exists or if the OSM image is missing
                if (File.Exists(outputImagePath)) continue;
                if (!File.Exists(osmImagePath))
                {
                    log.Info($"Masked Image File missing: {osmImagePath}");
                    continue;
                }

                try
                {
                    // Performing masking
                    Bitmap masked = new OsmBasedMasking()
                        .OsmImageMasking(baseImagePath, osmImagePath, maskingRange);
                    
                    if (masked != null) 
                    {
                        masked.Save(outputImagePath, ImageFormat.Png);
                        masked.Dispose();
                    }

                    // Only for debugging purposes
                    //MessageBox.Show($"File saved: {outputImagePath}");
                }
                catch (Exception ex)
                {
                    log.Info($"Error processing {fileName}: {ex.Message}");
                }
            }
        }
    }
}
