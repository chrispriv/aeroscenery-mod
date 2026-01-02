using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.OrthophotoSources
{
    public class HereWeGoOrthophotoSource : GenericOrthophotoSource
    {
        //public static string DefaultUrlTemplate = "https://1.aerial.maps.ls.hereapi.com/maptile/2.1/maptile/newest/satellite.day/{zoom}/{x}/{y}/256/jpg?apiKey=RBx63mkCvx-pB_w9dWzVj2tnCTDTozT3C-O14S_H2Ls";
        public static string DefaultUrlTemplate = "https://1.aerial.maps.ls.hereapi.com/maptile/2.1/maptile/newest/satellite.day/{zoom}/{x}/{y}/256/jpg?apiKey={apikey}";

        // Initial HTTP URL
        //public static string DefaultUrlTemplate = "http://1.aerial.maps.api.here.com/maptile/2.1/maptile/4c6170d81c/satellite.day/{zoom}/{x}/{y}/256/jpg?app_id=VgTVFr1a0ft1qGcLCVJ6&app_code=LJXqQ8ErW71UsRUK3R33Ow";

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

        public HereWeGoOrthophotoSource()
        {
            this.urlTemplate = DefaultUrlTemplate;
            Initialize();
        }

        public HereWeGoOrthophotoSource(string urlTemplate)
        {
            this.urlTemplate = urlTemplate;
            Initialize();
        }

        private void Initialize()
        {
            this.width = 256;
            this.height = 256;
            this.imageExtension = "jpg";
            this.source = OrthophotoSourceDirectoryName.HereWeGo;
            this.tiledWebMapType = TiledWebMapType.Google;
            //#MOD_h
            this.AdditionalUrlParams = new Dictionary<string, string>();
        }

    }
}
