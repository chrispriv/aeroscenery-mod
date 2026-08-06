using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//#MOD_k
namespace AeroScenery.ImageProcessing
{
    public enum OsmFeatureType { None, Water1, Water2, Water3, Road1, Road2, Building1, Building2, Forest, Runway }

    public class OsmFeatureMatrix
    {
        public OsmFeatureType[,] features;

        public OsmFeatureMatrix(int width, int height)
        {
            features = new OsmFeatureType[width, height];
        }

        public OsmFeatureType GetFeature(int x, int y) => features[x, y];
        public void SetFeature(int x, int y, OsmFeatureType feature) => features[x, y] = feature;
        public int Width => features.GetLength(0);
        public int Height => features.GetLength(1);
    }

}
