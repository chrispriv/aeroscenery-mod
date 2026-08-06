using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.OrthophotoSources
{
    public class ArcGISOrthophotoSource : GenericOrthophotoSource
    {
        //#MOD_d
        // New secured HTTPS URL and downlaod as JPG
        public static string DefaultUrlTemplate = "https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{zoom}/{y}/{x}.jpg";

        // Initial HTTP URL
        //public static string DefaultUrlTemplate = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{zoom}/{y}/{x}";

        // Just to try out the Esri Topo World Map
        //public static string DefaultUrlTemplate = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{zoom}/{y}/{x}.jpg";

        public ArcGISOrthophotoSource()
        {
            this.urlTemplate = DefaultUrlTemplate;
            Initialize();
        }

        public ArcGISOrthophotoSource(string urlTemplate)
        {
            this.urlTemplate = urlTemplate;
            Initialize();
        }

        private void Initialize()
        {
            this.width = 256;
            this.height = 256;
            //#MOD_d
            this.imageExtension = "jpg";
            //this.imageExtension = "jfif";
            this.source = OrthophotoSourceDirectoryName.ArcGIS;
            this.tiledWebMapType = TiledWebMapType.Google;
        }

    }
}
