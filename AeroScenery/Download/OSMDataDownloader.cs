using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Windows.Forms;


//#DEVL_k
namespace AeroScenery.Download
{
    public class OSMDataDownloader
    {
        public string OverpassApiUrl { get; set; }
        public string OutputDirectory { get; set; }
        public string TileName { get; set; }
        public string BoundingBox { get; set; }

        public OSMDataDownloader(string overpassApiUrl, string outputDirectory, string tileName, string boundingBox)
        {
            OverpassApiUrl = overpassApiUrl;
            OutputDirectory = outputDirectory;
            TileName = tileName;
            BoundingBox = boundingBox;
        }

        public void DownloadOSMData()
        {
            // URL für die OSM-Daten
            string downloadUrl = $"{OverpassApiUrl}?bbox={BoundingBox}";
            string outputFilePath = Path.Combine(OutputDirectory, $"{TileName}.osm");

            try
            {
                // Sicherstellen, dass das Verzeichnis existiert
                //Directory.CreateDirectory(Path.GetDirectoryName(OutputDirectory));

                // WebClient zum Herunterladen verwenden
                using (var client = new WebClient())
                {
                    //MessageBox.Show("Downloading OSM Data from OpenStreetMap...");
                    client.DownloadFile(downloadUrl, outputFilePath);
                    //MessageBox.Show($"Download finished. File saved to: {outputFilePath}");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"An error occurred while downloading the OSM data: {ex.Message}");
            }
        }
    }
}
