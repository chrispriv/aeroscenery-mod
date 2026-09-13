using AeroScenery.OrthoPhotoSources;
using System.Collections.Generic;

namespace AeroScenery.OrthophotoSources //#MOD
{
    public class CartoDBLightOrthomapSource : GenericOrthophotoSource
    {
        //#MOD_k
        //API key now needed for Carto DB Light (no labels) tiles. You can get one here: https://carto.com/apikey/
        //public static string DefaultUrlTemplate = "https://cartodb-basemaps-a.global.ssl.fastly.net/light_nolabels/{zoom}/{x}/{y}.png"; // Carto DB Light (no labels)
        public static string DefaultUrlTemplate = "https://cartodb-basemaps-a.global.ssl.fastly.net/light_nolabels/{zoom}/{x}/{y}.png?key={apikey}"; // Carto DB Light (no labels)

        //#MOD_k
        private string apiKey;

        public string ApiKey
        {
            get
            {
                return apiKey;
            }
            set
            {
                apiKey = value;

                if (this.AdditionalUrlParams.ContainsKey("apikey"))
                {
                    this.AdditionalUrlParams["apikey"] = apiKey;
                }
                else
                {
                    this.AdditionalUrlParams.Add("apikey", apiKey);
                }
            }
        }

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
            this.imageExtension = "png";
            this.source = OrthophotoSourceDirectoryName.CartoDBLight;
            this.tiledWebMapType = TiledWebMapType.Google;
            //#MOD_k
            this.AdditionalUrlParams = new Dictionary<string, string>();
        }

    }
}
