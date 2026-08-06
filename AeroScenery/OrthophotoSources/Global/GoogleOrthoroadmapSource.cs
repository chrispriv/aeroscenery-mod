using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.OrthophotoSources //#MOD_c
{
    public class GoogleOrthoroadmapSource : GenericOrthophotoSource
    {

        //#MOD_b
        //public static string  DefaultUrlTemplate = "http://mt1.google.com/vt/lyrs=r&x={x}&y={y}&z={zoom}"; //lyrs=t for Terrain & lyrs=r for Roads
        //TRY_h
        public static string DefaultUrlTemplate = "https://wprd02.is.autonavi.com/appmaptile?style=6&x={x}&y={y}&z={zoom}";

        public GoogleOrthoroadmapSource()
        {
            this.urlTemplate = DefaultUrlTemplate;
            Initialize();
        }

        public GoogleOrthoroadmapSource(string urlTemplate)
        {
            this.urlTemplate = urlTemplate;
            Initialize();
        }

        private void Initialize()
        {
            this.width = 256;
            this.height = 256;
            this.imageExtension = "jpg";   //"MOD
            this.source = OrthophotoSourceDirectoryName.GoogleRoads; //"MOD
            this.tiledWebMapType = TiledWebMapType.Google;
        }

    }
}
