using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.OrthophotoSources //#MOD_b
{
    public class CartoDBLightOrthomapSource : GenericOrthophotoSource
    {
        //#MOD_b
        //public static string DefaultUrlTemplate = "http://basemaps.cartocdn.com/light_nolabels/{zoom}/{x}/{y}.png"; // Carto DB Light (no labels)
        //#MOD_h
        public static string DefaultUrlTemplate = "https://cartodb-basemaps-a.global.ssl.fastly.net/light_nolabels/{zoom}/{x}/{y}.png"; // Carto DB Light (no labels)

        public CartoDBLightOrthomapSource()
        {
            this.urlTemplate = DefaultUrlTemplate;
            Initialize();
        }

        public CartoDBLightOrthomapSource(string urlTemplate)
        {
            this.urlTemplate = urlTemplate;
            Initialize();
        }

        private void Initialize()
        {
            this.width = 256;
            this.height = 256;
            this.imageExtension = "png"; //#MOD_b
            this.source = OrthophotoSourceDirectoryName.CartoDBLight; //#MOD_b
            this.tiledWebMapType = TiledWebMapType.Google;
        }

    }
}
