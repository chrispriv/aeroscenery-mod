using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.FlightPathVisualizer.Models
{
    public class TerrainData
    {
        public double[,] ElevationGrid { get; set; }  // Altitude values in meters
        public int Width { get; set; }
        public int Height { get; set; }

        // Geo reference data
        public double OriginLongitude { get; set; }  // Top-Left Lon coordinates
        public double OriginLatitude { get; set; }   // Top-Left Lat coordinates
        public double PixelSizeX { get; set; }        // Number of pixels
        public double PixelSizeY { get; set; }        // Number of pixels

        public double NoDataValue { get; set; } = double.NaN;

        // Additional property for creation and saving of HeightMap 
        public float[,] HeightMap
        {
            get
            {
                if (ElevationGrid == null)
                    return null;

                int w = ElevationGrid.GetLength(0);
                int h = ElevationGrid.GetLength(1);
                float[,] heightMap = new float[w, h];

                for (int i = 0; i < w; i++)
                    for (int j = 0; j < h; j++)
                        heightMap[i, j] = (float)ElevationGrid[i, j];

                return heightMap;
            }
            set
            {
                if (value == null)
                {
                    ElevationGrid = null;
                    return;
                }

                int w = value.GetLength(0);
                int h = value.GetLength(1);
                ElevationGrid = new double[w, h];

                for (int i = 0; i < w; i++)
                    for (int j = 0; j < h; j++)
                        ElevationGrid[i, j] = value[i, j];
            }
        }
    }
}
