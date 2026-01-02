using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.OrthophotoSources
{
    public class MapboxOrthophotoSource : GenericOrthophotoSource
    {
        //#MOD_e
        public static string DefaultUrlTemplate = "https://api.mapbox.com/v4/mapbox.satellite/{zoom}/{x}/{y}.jpg?access_token={apikey}";

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

        public MapboxOrthophotoSource()
        {
            this.urlTemplate = DefaultUrlTemplate;
            Initialize();
        }

        public MapboxOrthophotoSource(string urlTemplate)
        {
            this.urlTemplate = urlTemplate;
            Initialize();
        }

        private void Initialize()
        {
            this.width = 256;
            this.height = 256;
            //#MOD_e
            this.imageExtension = "jpg";
            this.source = OrthophotoSourceDirectoryName.Mapbox;
            this.tiledWebMapType = TiledWebMapType.Google;
            //#MOD_e
            this.AdditionalUrlParams = new Dictionary<string, string>();
        }

    }
}
