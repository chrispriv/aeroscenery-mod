using AeroScenery.OrthoPhotoSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScenery.Common
{
    public enum ActionSet
    {
        Default,
        Custom
    }

    public class Settings
    {
        public Settings()
        {
            this.ElevationSettings = new ElevationSettings();
            this.OrthophotoSourceSettings = new OrthophotoSourceSettings();
        }

        public string AFS2SDKDirectory { get; set; }

        public string AFS2Directory { get; set; }

        public string WorkingDirectory { get; set; }

        public string AeroSceneryDBDirectory { get; set; }

        public OrthophotoSource? OrthophotoSource { get; set; }

        public int? ZoomLevel { get; set; }

        public bool? DownloadImageTiles { get; set; }

        public bool? FixMissingTiles { get; set; }

        public bool? StitchImageTiles { get; set; }

        //#DEVL_k
        public bool? WaterMaskingEnable { get; set; }

        public bool? AllowShiftCorrectionEnable { get; set; }


        public bool? GenerateAIDAndTMCFiles { get; set; }

        public bool? RunGeoConvert { get; set; }

        //#MOD
        public bool? DownloadOSMDataEnable { get; set; }
        public bool? DownloadOsmData { get; set; }
        public bool? DownloadElevationData { get; set; }

        public bool? RunTreesDetection { get; set; }
        public bool? RunTreesDetectionMask { get; set; }
        public bool? RunTreesDetectionDetection { get; set; }

        public bool? DeleteStitchedImageTiles { get; set; }

        public bool? InstallScenery { get; set; }

        public ActionSet? ActionSet { get; set; }

        public List<int> AFSLevelsToGenerate { get; set; }

        public string UserAgent { get; set; }


        public int? DownloadWaitMs { get; set; }

        public int? DownloadWaitRandomMs { get; set; }

        public int? SimultaneousDownloads { get; set; }

        public int? MaximumStitchedImageSize { get; set; }

        public bool? GeoConvertWriteImagesWithMask { get; set; }

        public bool? GeoConvertWriteRawFiles { get; set; }

        public bool? GeoConvertDoMultipleSmallerRuns { get; set; }

        public bool? GeoConvertUseWrapper { get; set; }

        public bool? ShowMultipleConcurrentSquaresWarning { get; set; }

        public string USGSUsername { get; set; }
        public string USGSPassword { get; set; }

        public string LinzApiKey { get; set; }

        //#MOD
        public string MapboxApiKey { get; set; }
        public string OpenTopographyApiKey { get; set; }
        public string OpenTopographyDataSet { get; set; }
        public string HereWeGoApiKey { get; set; }

        public int? MapControlLastZoomLevel { get; set;}
        public double? MapControlLastX { get; set; }
        public double? MapControlLastY { get; set; }
        public string MapControlLastMapType { get; set; }
        public bool? ShowAirports { get; set; }
        public double? ShrinkTMCGridSquareCoords { get; set; }
        public string AFS2UserDirectory { get; set; }

        //#MOD
        public string QGISDirectory { get; set; }
        public string GeoTiffElevationMapFilename { get; set; }
        public string AFSSceneryFolder { get; set; }

        // Image procesing
        public bool? EnableImageProcessing { get; set; }
        public int? BrightnessAdjustment { get; set; }
        public int? ContrastAdjustment { get; set; }
        public int? SaturationAdjustment { get; set; }
        public int? SharpnessAdjustment { get; set; }
        public int? RedAdjustment { get; set; }
        public int? GreenAdjustment { get; set; }
        public int? BlueAdjustment { get; set; }

        //#MOD
        public bool? RemoveAlphaChannelAdjustment { get; set; }

        //#DEVL_k
        public bool? WaterMaskingProcessing { get; set; }
        public int? WaterFadeThresholdDistance { get; set; }
        public int? WaterReplaceThresholdDistance { get; set; }
        
        public bool? AllowShiftCorrectionProcessing { get; set; }

        public int? AllowShiftCorrectionLevel { get; set; }    


        public bool? GridSquareNamesFixed { get; set; }

        public OrthophotoSourceSettings OrthophotoSourceSettings { get; set; }

        public ElevationSettings ElevationSettings { get; set; }

        //#MOD
        public string TreesDetectionDirectory { get; set; }
        public int? TreesDetectionDensity { get; set; }
        public bool? TreesDetectionQuit { get; set; }
        public int? TreesDetectionAltitudeMax { get; set; }
        public bool? TreesDetectionAltitudeCheck { get; set; }
        public int? TreesPresetIndex { get; set; }
        public bool? TreesPresetHighTrees { get; set; }
        public bool? TreesPresetBigShrubs { get; set; }

        //#MOD
        public bool? CreateAddForMobile { get; set; }

        //#DEVL_k
        public bool? MovingMapElevationDataEnable { get; set; }
        public bool? MovingMapElevationData { get; set; }
        public string MovingMapElevationFileName { get; set; }
        public int? MovingMapElevationDataRendering { get; set; }
        public bool? MovingMapElevationEnable3DCapture { get; set; }


    }
}
