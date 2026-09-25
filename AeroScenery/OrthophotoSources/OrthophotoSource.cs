namespace AeroScenery.OrthoPhotoSources
{
    public enum OrthophotoSource
    {
        Bing,
        Google,
        ArcGIS,
        US_USGS,
        NZ_Linz,
        ES_IDEIB,
        CH_Geoportal,
        NO_NorgeBilder,
        SE_Lantmateriet,
        ES_IGN,
        JP_GSI,
        USGS, // For backwards compatibility
        SE_Hitta,
        HereWeGo,
        NO_GuleSider,
        //#MOD
        Mapbox,
        GoogleMaps,
        GoogleRoads,
        OSMMaps,
        CartoDBLight,
    }

    public abstract class OrthophotoSourceDirectoryName
    {
        public static readonly string Bing = "b";
        public static readonly string Google = "g";
        public static readonly string ArcGIS = "arcg";
        public static readonly string US_USGS = "us_usgs";
        public static readonly string NZ_Linz = "nz_linz";
        public static readonly string ES_IDEIB = "es_ideib";
        public static readonly string CH_Geoportal = "ch_geo";
        public static readonly string NO_NorgeBilder = "no_nb";
        public static readonly string SE_Lantmateriet = "se_lant";
        public static readonly string ES_IGN = "es_ign";
        public static readonly string JP_GSI = "jp_gsi";
        public static readonly string SE_Hitta = "se_hitta";
        public static readonly string HereWeGo = "hwg";
        public static readonly string NO_GuleSider = "no_gus";
        //#MOD
        public static readonly string Mapbox = "mapb";
        public static readonly string GoogleMaps = "g-mask";
        public static readonly string GoogleRoads = "r-mask";
        public static readonly string OSMMaps = "o-mask";
        public static readonly string CartoDBLight = "c-mask";

        public static string GetDirectoryName(OrthophotoSource source)
        {
            switch (source)
            {
                case OrthophotoSource.Google:
                    return Google;
                case OrthophotoSource.ArcGIS:
                    return ArcGIS;
                case OrthophotoSource.USGS:
                case OrthophotoSource.US_USGS:
                    return US_USGS;
                case OrthophotoSource.NZ_Linz:
                    return NZ_Linz;
                case OrthophotoSource.ES_IDEIB:
                    return ES_IDEIB;
                case OrthophotoSource.CH_Geoportal:
                    return CH_Geoportal;
                case OrthophotoSource.NO_NorgeBilder:
                    return NO_NorgeBilder;
                case OrthophotoSource.SE_Lantmateriet:
                    return SE_Lantmateriet;
                case OrthophotoSource.ES_IGN:
                    return ES_IGN;
                case OrthophotoSource.JP_GSI:
                    return JP_GSI;
                case OrthophotoSource.SE_Hitta:
                    return SE_Hitta;
                case OrthophotoSource.HereWeGo:
                    return HereWeGo;
                case OrthophotoSource.NO_GuleSider:
                    return NO_GuleSider;
                case OrthophotoSource.Mapbox:
                    return Mapbox;
                case OrthophotoSource.GoogleMaps:
                    return GoogleMaps;
                case OrthophotoSource.GoogleRoads:
                    return GoogleRoads;
                case OrthophotoSource.OSMMaps:
                    return OSMMaps;
                case OrthophotoSource.CartoDBLight:
                    return CartoDBLight;
                default:
                    return Bing;
            }
        }
    }
}
