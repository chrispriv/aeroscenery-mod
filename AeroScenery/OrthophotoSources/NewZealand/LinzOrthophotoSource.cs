using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.OrthophotoSources.NewZealand
{
    public class LinzOrthophotoSource : GenericOrthophotoSource
    {
        //public static string DefaultUrlTemplate = "http://koordinates-tiles-d.global.ssl.fastly.net/services;key=50721244e42045c58b2bbe2ee5487a9a/tiles/v4/layer=51769,style=auto;layer=88131,style=auto;layer=95497,style=auto/{zoom}/{x}/{y}.png"; // initial url

        //#MOD_h
        //public static string DefaultUrlTemplate = "http://tiles-a.data-cdn.linz.govt.nz/services;key={apikey}/tiles/v4/set=4702/EPSG:3857/{zoom}/{x}/{y}.png"; // before change to new secured https://
        public static string DefaultUrlTemplate = "https://basemaps.linz.govt.nz/v1/tiles/aerial/3857/{zoom}/{x}/{y}.png?api={apikey}";


        private string apiKey;

        public string ApiKey {
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

        public LinzOrthophotoSource()
        {
            this.urlTemplate = DefaultUrlTemplate;
            Initialize();
        }

        public LinzOrthophotoSource(string urlTemplate)
        {
            this.urlTemplate = urlTemplate;
            Initialize();
        }

        private void Initialize()
        {
            this.width = 256;
            this.height = 256;
            this.imageExtension = "png";
            this.source = OrthophotoSourceDirectoryName.NZ_Linz;
            this.tiledWebMapType = TiledWebMapType.Google;
            this.AdditionalUrlParams = new Dictionary<string, string>();
        }
    }
}
