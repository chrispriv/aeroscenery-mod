using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Web;

namespace AeroScenery.Download
{
    public class ElevationDataDownloader
    {
        public string OverpassApiUrl { get; set; }
        public string OutputDirectory { get; set; }
        public string TileName { get; set; }

        public string DEMType { get; set; }
        public string BoundingBox { get; set; }
        public string APIKey { get; set; }

        public ElevationDataDownloader(string overpassApiUrl, string outputDirectory, string tileName, string demType, string boundingBox, string apiKey)
        {
            OverpassApiUrl = overpassApiUrl;
            OutputDirectory = outputDirectory;
            TileName = tileName;
            DEMType = demType;
            BoundingBox = boundingBox;
            APIKey = apiKey;
        }

        public void DownloadElevationData()
        {
            // URL für die Elevation Data Elevation-Daten from OpenTopography.org
            string downloadUrl = $"{OverpassApiUrl}{DEMType}&{BoundingBox}&outputFormat=GTiff&API_Key={APIKey}";
            string outputFilePath = Path.Combine(OutputDirectory, $"{TileName}.tif");

            try
            {
                // Sicherstellen, dass das Verzeichnis existiert
                //Directory.CreateDirectory(Path.GetDirectoryName(OutputDirectory));

                // WebClient zum Herunterladen verwenden
                using (var client = new WebClient())
                {
                    //MessageBox.Show("Downloading Elevation Data from OpenStreetMap...");
                    client.DownloadFile(downloadUrl, outputFilePath);
                    //MessageBox.Show($"Download finished. File saved to: {outputFilePath}");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"An error occurred while downloading the Elevation data: {ex.Message}");
            }
        }
    }
}
