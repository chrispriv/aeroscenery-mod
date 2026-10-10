using AeroScenery.OrthoPhotoSources;

namespace AeroScenery.OrthophotoSources
{
    public class GoogleOrthoroadmapSource : GenericOrthophotoSource
    {

        public static string  DefaultUrlTemplate = "http://mt1.google.com/vt/lyrs=r&x={x}&y={y}&z={zoom}"; //lyrs=t for Terrain & lyrs=r for Roads
        // Just to try out a different Map
        //public static string DefaultUrlTemplate = "https://wprd02.is.autonavi.com/appmaptile?style=6&x={x}&y={y}&z={zoom}";

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
            this.imageExtension = "jpg";
            this.source = OrthophotoSourceDirectoryName.GoogleRoads;
            this.tiledWebMapType = TiledWebMapType.Google;
        }

    }
}
