using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.OrthophotoSources //#MOD_b
{
    public class GoogleOrthomapSource : GenericOrthophotoSource
    {
        //#MOD_b
        public static string  DefaultUrlTemplate = "http://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={zoom}"; //lyrs=m for Map, lyrs=r for Roads

        public GoogleOrthomapSource()
        {
            this.urlTemplate = DefaultUrlTemplate;
            Initialize();
        }
        

        public GoogleOrthomapSource(string urlTemplate)
        {
            this.urlTemplate = urlTemplate;
            Initialize();
        }

        private void Initialize()
        {
            this.width = 256;
            this.height = 256;
            this.imageExtension = "jpg";
            this.source = OrthophotoSourceDirectoryName.GoogleMaps; //#MOD_b
            this.tiledWebMapType = TiledWebMapType.Google;
        }

    }
}
