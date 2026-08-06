using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.OrthophotoSources //#MOD
{
    public class OSMMapsOrthomapSource : GenericOrthophotoSource
    {
        //public static string  DefaultUrlTemplate = "https://tiles.wmflabs.org/osm-no-labels/{zoom}/{x}/{y}.png"; // XYZ-Tile-server don't works properly
        public static string DefaultUrlTemplate = "http://tile.openstreetmap.org/{zoom}/{x}/{y}.png";

        public OSMMapsOrthomapSource()
        {
            this.urlTemplate = DefaultUrlTemplate;
            Initialize();
        }

        public OSMMapsOrthomapSource(string urlTemplate)
        {
            this.urlTemplate = urlTemplate;
            Initialize();
        }

        private void Initialize()
        {
            this.width = 256;
            this.height = 256;
            this.imageExtension = "png";
            this.source = OrthophotoSourceDirectoryName.OSMMaps;
            this.tiledWebMapType = TiledWebMapType.Google;
        }

    }
}
