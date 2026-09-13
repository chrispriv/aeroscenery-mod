using AeroScenery.Common;
using AeroScenery.Controls;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace AeroScenery.AFS2
{
    public class AFSFileGenerator
    {
        private XmlSerializer xmlSerializer;

        private readonly ILog log = LogManager.GetLogger("AeroScenery");

        public AFSFileGenerator()
        {
            this.xmlSerializer = new XmlSerializer(typeof(StitchedImage));
        }

        // Calculates the shift linearly referencing to the selected user shift level, depending on the selected grid size tile level  (referenence = level 9)
        private double CalculateShift(
            int tileLevel,
            int userShiftLevel,
            bool reverseMode)
        {
            if (userShiftLevel == 0)
                return 0.0;

            const double baseShift = 0.00001;

            double shiftCurrent = 0;

            if (tileLevel <= 12) 
            {
                shiftCurrent = userShiftLevel * baseShift * Math.Pow(2, 12 - tileLevel);
            } 

            double shift;

            if (reverseMode)
            {
                // Reference = Level 9
                double shiftLevel9 = userShiftLevel * baseShift * Math.Pow(2, 12 - 9);

                shift = shiftCurrent - shiftLevel9;
            }
            else
            {
                shift = shiftCurrent;
            }

            return shift;
        }

        //#MOD_k
        // A new option for shift correction based on the user shift level has been added via the GUI to correct incorrect positioning along the north–south axis
        // The user shift level is referenced based on grid size tile level 9 (level 13 and 14 tiles are not shifted, as their position is correct)
        public async Task GenerateAFSFilesAsync(AFS2GridSquare afs2GridSquare, string stitchedTilesDirectory, string afsGridSquareDirectory, IProgress<AFSFileGeneratorProgress> progress)
        {
            await Task.Run(() =>
            {
                var afsFileGeneratorProgress = new AFSFileGeneratorProgress();

                StitchedImage firstStitchedImageAeroFile = null;

                // The number of stiched tiles should always be pretty manageable so we can get a list of filenames
                if (Directory.Exists(stitchedTilesDirectory))
                {
                    string[] stitchedImagesAeroFiles = Directory.GetFiles(stitchedTilesDirectory, "*.aero");

                    int i = 0;

                    foreach (string aeroFilename in stitchedImagesAeroFiles)
                    {
                        try
                        {
                            StitchedImage stitchedImageAeroFile;

                            using (StreamReader reader = new StreamReader(aeroFilename))
                            {
                                stitchedImageAeroFile = (StitchedImage)xmlSerializer.Deserialize(reader);
                                reader.Close();
                            }

                            if (i == 0)
                            {
                                firstStitchedImageAeroFile = stitchedImageAeroFile;
                            }

                            double stepsPerPixelX = Math.Abs((stitchedImageAeroFile.WestLongitude - stitchedImageAeroFile.EastLongitude) / stitchedImageAeroFile.Width);
                            double stepsPerPixelY = -Math.Abs((stitchedImageAeroFile.NorthLatitude - stitchedImageAeroFile.SouthLatitude) / stitchedImageAeroFile.Height);

                            int userShiftLevel = 0;
                            bool reverseMode = false; // Reverse mode option (means smaller tiles are shifted to wrong grid size tile level 9) is not implemented in the GUI, but kept for testing purposes 
                            if (AeroSceneryManager.Instance.Settings.AllowShiftCorrectionProcessing == true)
                            {
                                userShiftLevel = AeroSceneryManager.Instance.Settings.AllowShiftCorrectionLevel.Value;
                            }

                            double latShift = CalculateShift(afs2GridSquare.Level,userShiftLevel, reverseMode);
                            double shiftInPixels = -latShift / stepsPerPixelY;

                            if (AeroSceneryManager.Instance.Settings.AllowShiftCorrectionProcessing == true)
                            {
                                log.Info($"AFSLevel={afs2GridSquare.Level}, ShiftLevel={userShiftLevel}, Shift={latShift}, ShiftPixels={shiftInPixels}, ReverseMode={reverseMode}");
                            }

                            var aidFile = new AIDFile();

                            aidFile.ImageFile = stitchedImageAeroFile.FileName + "." + stitchedImageAeroFile.ImageExtension;
                            aidFile.FlipVertical = false;
                            aidFile.StepsPerPixelX = stepsPerPixelX;
                            aidFile.StepsPerPixelY = stepsPerPixelY;
                            aidFile.X = stitchedImageAeroFile.WestLongitude;
                            aidFile.Y = stitchedImageAeroFile.NorthLatitude + latShift; 

                            var aidFileStr = aidFile.ToString();

                            string path = stitchedTilesDirectory + stitchedImageAeroFile.FileName + ".aid";

                            log.InfoFormat("Writing AID file {0}", path);
                            File.WriteAllText(path, aidFileStr);
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }

                        i++;
                    }

                    if (firstStitchedImageAeroFile != null)
                    {
                        this.GenerateTMCFile(afs2GridSquare, stitchedTilesDirectory, afsGridSquareDirectory, firstStitchedImageAeroFile);
                    }
                    else
                    {
                        var messageBox = new CustomMessageBox("No stitched images found for this grid square and this image detail (zoom) level.\nRun the 'Download Image Tiles' and 'Stitch Image Tiles' actions first.",
                            "AeroScenery",
                            MessageBoxIcon.Error);

                        messageBox.ShowDialog();
                    }

                }
            });


        }

        private void GenerateTMCFile(AFS2GridSquare afs2GridSquare, string stitchedTilesDirectory, string afsGridSquareDirectory, StitchedImage firstStitchedImageAeroFile)
        {
            // Create directories for Geoconvert output if they do not exist.
            // Better to do this here in case anyone wants to run Geoconvert manually
            var geoConvertRawDirectory = String.Format("{0}-geoconvert-raw\\", firstStitchedImageAeroFile.ZoomLevel);
            var geoConvertTTCDirectory = String.Format("{0}-geoconvert-ttc\\", firstStitchedImageAeroFile.ZoomLevel);
            var geoConvertRawPath = afsGridSquareDirectory + geoConvertRawDirectory;
            var geoConvertTTCPath = afsGridSquareDirectory + geoConvertTTCDirectory;

            if (!Directory.Exists(geoConvertRawPath))
            {
                Directory.CreateDirectory(geoConvertRawPath);
            }

            if (!Directory.Exists(geoConvertTTCPath))
            {
                Directory.CreateDirectory(geoConvertTTCPath);
            }

            var tmcFile = new TMCFile();

            tmcFile.AlwaysOverwrite = true;
            tmcFile.DoHeightmaps = false;
            tmcFile.FolderDestinationRaw = geoConvertRawPath;
            tmcFile.FolderDestinationTTC = geoConvertTTCPath;
            tmcFile.FolderSourceFiles = stitchedTilesDirectory;
            tmcFile.WriteImagesWithMask = AeroSceneryManager.Instance.Settings.GeoConvertWriteImagesWithMask.Value;
            tmcFile.WriteRawFiles = AeroSceneryManager.Instance.Settings.GeoConvertWriteRawFiles.Value;
            tmcFile.WriteTTCFiles = true;

            // All TMC regions will have the same lat / lon max and min
            // Create a template Region here to base other regions off
            TMCRegion tmcRegionTemplate = new TMCRegion();

            // Really NW Corner
            tmcRegionTemplate.LatMin = afs2GridSquare.NorthLatitude;
            tmcRegionTemplate.LonMin = afs2GridSquare.WestLongitude;

            // Realy SE Corner
            tmcRegionTemplate.LatMax = afs2GridSquare.SouthLatitude;
            tmcRegionTemplate.LonMax = afs2GridSquare.EastLongitude;

            tmcFile.Regions = this.GenerateTMCFileRegions(tmcRegionTemplate);

            var tmcFileStr = tmcFile.ToString();

            var filenameParts = firstStitchedImageAeroFile.FileName.Split('_');
            var tmcFilename = String.Format("{0}_{1}_{2}", filenameParts[0], filenameParts[1], filenameParts[2]);

            string path = String.Format("{0}{1}.tmc", stitchedTilesDirectory, tmcFilename);

            File.WriteAllText(path, tmcFileStr);
        }

        private List<TMCRegion> GenerateTMCFileRegions(TMCRegion tmcRegionTemplate)
        {
            var settings = AeroSceneryManager.Instance.Settings;

            List<TMCRegion> regions = new List<TMCRegion>();

            foreach (int afsLevel in settings.AFSLevelsToGenerate)
            {
                var region = new TMCRegion();
                region.LatMax = tmcRegionTemplate.LatMax;
                region.LonMax = tmcRegionTemplate.LonMax;
                region.LatMin = tmcRegionTemplate.LatMin;
                region.LonMin = tmcRegionTemplate.LonMin;
                region.Level = afsLevel;
                region.WriteImagesWithMask = AeroSceneryManager.Instance.Settings.GeoConvertWriteImagesWithMask.Value;
                regions.Add(region);
            }

            return regions;

        }
    }
}
