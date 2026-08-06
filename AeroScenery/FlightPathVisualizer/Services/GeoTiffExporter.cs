using AeroScenery.FlightPathVisualizer.Models;
using OSGeo.GDAL;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.OSR;
using MaxRev.Gdal.Core;


namespace AeroScenery.FlightPathVisualizer.Services
{
    public class GeoTiffExporter
    {
        public static void SaveCutoutAsGeoTiff(float[,] heightMap, TerrainData terrainData, double originLon, double originLat, string outputPath)
        {
            try
            { 
                // Setze GDAL-Umgebungsvariablen
                GdalBase.ConfigureAll();   // <-- automatisch richtig
                Gdal.AllRegister();

                int width = heightMap.GetLength(1);   // Spalten
                int height = heightMap.GetLength(0);  // Zeilen

                // GDAL-Treiber holen
                Driver driver = Gdal.GetDriverByName("GTiff");
                if (driver == null)
                    throw new Exception("GTiff driver not available.");

                if (IsFileLocked(outputPath))
                    throw new Exception($"The file '{outputPath}' is locked resp. open with another app and cannot be overwritten.");

                // Dataset anlegen
                using (Dataset ds = driver.Create(outputPath, width, height, 1, DataType.GDT_Float32, null))
                {
                    if (ds == null)
                        throw new Exception($"Unable to create output file {outputPath}");

                    // Berechne GeoTransform (Startpunkt ist obere linke Ecke)
                    double pixelSizeX = terrainData.PixelSizeX;
                    double pixelSizeY = terrainData.PixelSizeY; // sollte negativ sein
                    double originX = originLon;
                    //double originY = originLat + height * pixelSizeY; // Korrektur: obere linke Ecke
                    double originY = originLat;

                    double[] gt = new double[6];
                    gt[0] = originX;       // top-left X
                    gt[1] = pixelSizeX;    // pixel width
                    gt[2] = 0;             // rotation
                    gt[3] = originY;       // top-left Y (korrigiert)
                    gt[4] = 0;             // rotation
                    gt[5] = pixelSizeY;    // pixel height (negativ für "north up")

                    ds.SetGeoTransform(gt);

                    //ds.SetProjection("EPSG:4326");
                    var srs = new SpatialReference("");
                    srs.ImportFromEPSG(4326);
                    srs.ExportToWkt(out string wkt, null); // korrekt!
                    ds.SetProjection(wkt);                 // dann setzen


                    // Daten in Band schreiben
                    Band band = ds.GetRasterBand(1);
                    float[] buffer = new float[width * height];

                    // Zeilenweise in den 1D-Buffer schreiben
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                            buffer[y * width + x] = heightMap[y, x];

                    band.WriteRaster(0, 0, width, height, buffer, width, height, 0, 0);
                    band.FlushCache();
                }
            }
            catch (Exception ex) 
            {
                throw; // Exception weitergeben!
            }
        }

        public static bool IsFileLocked(string filePath)
        {
            if (!File.Exists(filePath))
                return false; // Datei existiert nicht, keine Sperre

            FileStream fs = null;
            try
            {
                fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
                return false; // Datei ist nicht gesperrt
            }
            catch (IOException)
            {
                return true; // Datei ist gesperrt
            }
            finally
            {
                if (fs != null)
                    fs.Dispose(); // Stellt sicher, dass das FileStream-Objekt korrekt geschlossen wird
            }
        }

    }
}
