using AeroScenery.FlightPathVisualizer.Models;
using OSGeo.GDAL;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.FlightPathVisualizer.Services
{
    public class GeoTiffLoader
    {
        public TerrainData Load(string filePath)
        {
            Gdal.AllRegister();

            Dataset ds = Gdal.Open(filePath, Access.GA_ReadOnly);
            if (ds == null)
                throw new Exception("Failed to open DEM file: " + filePath);

            Band band = ds.GetRasterBand(1);

            int width = ds.RasterXSize;
            int height = ds.RasterYSize;

            double[] geoTransform = new double[6];
            ds.GetGeoTransform(geoTransform);

            double originX = geoTransform[0];
            double pixelWidth = geoTransform[1];
            double originY = geoTransform[3];
            double pixelHeight = geoTransform[5]; // i. d. R. negativ (top-down)

            // NoData-Wert
            double noData;
            int hasNoData;
            band.GetNoDataValue(out noData, out hasNoData);
            if (hasNoData == 0)
                noData = double.NaN;

            // Höhenwerte einlesen
            float[] buffer = new float[width * height];
            band.ReadRaster(0, 0, width, height, buffer, width, height, 0, 0);

            double[,] elevationGrid = new double[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float val = buffer[y * width + x];
                    //elevationGrid[y, x] = (val == noData) ? double.NaN : val;
                    if (val == noData)
                    {
                        elevationGrid[y, x] = double.NaN;
                    }
                    // Wenn der Wert negativ ist, setze 0
                    else if (val < 0)
                    {
                        elevationGrid[y, x] = 0;
                    }
                    else
                    {
                        elevationGrid[y, x] = val;
                    }
                }
            }
 
            // Koordinatentransformation prüfen  
            string projWkt = ds.GetProjection();
            var sourceSRS = new SpatialReference(projWkt);

            var epsgCode = sourceSRS.AutoIdentifyEPSG();
            string authority = sourceSRS.GetAuthorityName(null);
            string code = sourceSRS.GetAuthorityCode(null);

            double originLon = originX;
            double originLat = originY;
            double pixelSizeLon = pixelWidth;
            double pixelSizeLat = pixelHeight;

            if ((code != "4326") && (code != "4269"))
                throw new Exception($"DEM file has wrong format {authority}-{code} (EPSG:4326-WGS 84 or 4269-NAD83 required) in " + filePath);

            ds.Dispose();  // GDAL Datei-Handle schließen
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return new TerrainData
            {
                Width = width,
                Height = height,
                ElevationGrid = elevationGrid,
                OriginLongitude = originLon,
                OriginLatitude = originLat,
                PixelSizeX = pixelSizeLon,
                PixelSizeY = pixelSizeLat,
                NoDataValue = noData
            };

        }

    }

}
