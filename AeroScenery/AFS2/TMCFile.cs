using AeroScenery.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//#DEVL_k
using System.IO;
using System.Text.RegularExpressions;

namespace AeroScenery.AFS2
{
    public class TMCRegion
    {
        public int Level { get; set; }
        public double LonMin { get; set; }
        public double LatMin { get; set; }
        public double LonMax { get; set; }
        public double LatMax { get; set; }
        public bool WriteImagesWithMask { get; set; }

        public string NorthWestLonLatStr
        {
            get
            {
                var westLon = LonMin;
                var northLat = LatMin;

                var westLonShrink = GeoCoordinatesHelper.CalculateOffset(westLon, AeroSceneryManager.Instance.Settings.ShrinkTMCGridSquareCoords.Value, Direction.East);
                var northLatShrink = GeoCoordinatesHelper.CalculateOffset(northLat, AeroSceneryManager.Instance.Settings.ShrinkTMCGridSquareCoords.Value, Direction.South);

                return String.Format("{0} {1}", westLonShrink.ToString("0.00######", CultureInfo.InvariantCulture), northLatShrink.ToString("0.00######", CultureInfo.InvariantCulture));
            }
        }

        public string SouthEastLonLatStr
        {
            get
            {
                var eastLon = LonMax;
                var southLat = LatMax;

                var eastLonShrink = GeoCoordinatesHelper.CalculateOffset(eastLon, AeroSceneryManager.Instance.Settings.ShrinkTMCGridSquareCoords.Value, Direction.West);
                var southLatShrink = GeoCoordinatesHelper.CalculateOffset(southLat, AeroSceneryManager.Instance.Settings.ShrinkTMCGridSquareCoords.Value, Direction.North);

                return String.Format("{0} {1}", eastLonShrink.ToString("0.00######", CultureInfo.InvariantCulture), southLatShrink.ToString("0.00######", CultureInfo.InvariantCulture));
            }
        }
    }

    public class TMCFile
    {
        public bool WriteImagesWithMask { get; set; }
        public bool WriteRawFiles { get; set; }
        public bool WriteTTCFiles { get; set; }
        public bool AlwaysOverwrite { get; set; }
        public bool DoHeightmaps { get; set; }
        public string FolderDestinationTTC { get; set; }
        public string FolderDestinationRaw { get; set; }
        public string FolderSourceFiles { get; set; }

        public List<TMCRegion> Regions { get; set; }

        public TMCFile()
        {
            Regions = new List<TMCRegion>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<[file]" + "[]" + "[" + "]");
            sb.AppendLine("\t" + "<[tmcolormap_regions]" + "[]" + "[]");

            if (!String.IsNullOrEmpty(this.FolderSourceFiles))
            {
                sb.AppendLine("\t\t" + "<[string8]" + "[folder_source_files]" + "[" + FolderSourceFiles + "]>");
            }

            sb.AppendLine("\t\t" + "<[bool]" + "[write_ttc_files]" + "[" + WriteTTCFiles.ToString().ToLower() + "]>");
            sb.AppendLine("\t\t" + "<[string8]" + "[folder_destination_ttc]" + "[" + FolderDestinationTTC + "]>");
            sb.AppendLine("\t\t" + "<[bool]" + "[write_raw_files]" + "[" + WriteRawFiles.ToString().ToLower() + "]>");
            sb.AppendLine("\t\t" + "<[string8]" + "[folder_destination_raw]" + "[" + FolderDestinationRaw + "]>");
            sb.AppendLine("\t\t" + "<[bool]" + "[do_heightmaps]" + "[" + DoHeightmaps.ToString().ToLower() + "]>");
            sb.AppendLine("\t\t" + "<[bool]" + "[always_overwrite]" + "[" + AlwaysOverwrite.ToString().ToLower() + "]>");
            sb.AppendLine("\t\t" + "<[bool]" + "[write_images_with_mask]" + "[" + WriteImagesWithMask.ToString().ToLower() + "]>");
            sb.AppendLine("");
            sb.AppendLine("\t\t" + "<[list]" + "[region_list]" + "[]");
            sb.AppendLine("");

            if (this.Regions != null)
            {
                foreach (var region in this.Regions)
                {
                    sb.AppendLine("\t\t\t<[tmcolormap_region][element][0]");
                    sb.AppendLine(String.Format("\t\t\t\t<[uint32] [level] [{0}]>", region.Level));
                    sb.AppendLine(String.Format("\t\t\t\t<[vector2_float64] [lonlat_min] [{0}]>", region.NorthWestLonLatStr));
                    sb.AppendLine(String.Format("\t\t\t\t<[vector2_float64] [lonlat_max] [{0}]>", region.SouthEastLonLatStr));
                    sb.AppendLine(String.Format("\t\t\t\t<[bool] [write_images_with_mask] [{0}]>", region.WriteImagesWithMask.ToString().ToLower()));
                    sb.AppendLine("\t\t\t" + ">");
                    sb.AppendLine("");
                }
            }


            sb.AppendLine("");
            sb.AppendLine("\t\t" + ">");
            sb.AppendLine("\t" + ">");
            sb.AppendLine(">");

            return sb.ToString();

        }

    }

    //#DEVL_k
    public class TMCElevationFile
    {
        public string InputFolderImages { get; set; }
        public string OutputFolderTTH { get; set; }
        public string BoundingBox { get; set; }
        public string MeshResolution { get; set; }
        public string GridSquareLevel { get; set; }

        public string GeneratedContent { get; private set; }

        public TMCElevationFile(string inputFolder, string outputFolder, string boundingBox, string meshResolution, string gridSquareLevel)
        {
            InputFolderImages = inputFolder;
            OutputFolderTTH = outputFolder;
            BoundingBox = boundingBox;
            MeshResolution = meshResolution;
            GridSquareLevel = gridSquareLevel;

            // Text is generated and saved in the constructor
            GeneratedContent = GenerateContent();
        }

        // Generation of the text
        public string GenerateContent()
        {
            string southLat = BoundingBox.Split('&')[0];
            string northLat = BoundingBox.Split('&')[1];
            string westLng = BoundingBox.Split('&')[2];
            string eastLng = BoundingBox.Split('&')[3];

            string[] values = BoundingBox.Split('&')
                               .Select(part => part.Split('=')[1].TrimEnd('&'))
                               .ToArray();
            southLat = values[0];
            northLat = values[1];
            westLng = values[2];
            eastLng = values[3];

            int meshResolutionMeter = Convert.ToInt16(MeshResolution); 
            int gridSpuareLevel = Convert.ToInt16(GridSquareLevel);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<[file][][]");
            sb.AppendLine("    <[tmcolormap_regions][][]");
            sb.AppendLine($"        <[string] [folder_source_files][{InputFolderImages}]>");
            sb.AppendLine("        <[bool]   [write_images_with_mask][false]>");
            sb.AppendLine("        <[bool]   [write_ttc_files][false]>");
            sb.AppendLine("        <[bool]   [do_heightmaps][true]>");
            sb.AppendLine($"        <[string8][folder_destination_heightmaps][{OutputFolderTTH}]>");
            sb.AppendLine("        <[bool]   [always_overwrite][true]>");
            sb.AppendLine();
            sb.AppendLine("        <[list][region_list][] //Note: After the GeoConvert process, keep only the non-masked TTH files resp. delete all masked files for use with FS4");
            sb.AppendLine();

            if (gridSpuareLevel <=8)
            {
                sb.AppendLine("            <[tmheightmap_region][element][0]    //Note: only Level 7 & 10 needed for FS2023 Mobile(Android)");
                sb.AppendLine("                <[uint32]              [level]  [8]>");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_min]   [{westLng} {southLat}]>// [<West> <Süd>]");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_max]   [{eastLng} {northLat}]>// [<Ost> <Nord>]");
                sb.AppendLine("                <[bool]                [write_images_with_mask][false]>");
                sb.AppendLine("            >");
                sb.AppendLine();
            }
            if (gridSpuareLevel <= 9) 
            {
                sb.AppendLine("            <[tmheightmap_region][element][1]");
                sb.AppendLine("                <[uint32]              [level]  [9]>");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_min]   [{westLng} {southLat}]>// [<West> <Süd>]");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_max]   [{eastLng} {northLat}]>// [<Ost> <Nord>]");
                sb.AppendLine("                <[bool]                [write_images_with_mask][false]>");
                sb.AppendLine("            >");
                sb.AppendLine();
            }

            if ((gridSpuareLevel <= 10) && (meshResolutionMeter <=50))
            {
                sb.AppendLine("            <[tmheightmap_region][element][2]");
                sb.AppendLine("                <[uint32]              [level]  [10]>");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_min]   [{westLng} {southLat}]>// [<West> <Süd>]");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_max]   [{eastLng} {northLat}]>// [<Ost> <Nord>]");
                sb.AppendLine("                <[bool]                [write_images_with_mask][false]>");
                sb.AppendLine("            >");
                sb.AppendLine();
            }
            if ((gridSpuareLevel <= 11) && (meshResolutionMeter <= 20))
            {
                sb.AppendLine("            <[tmheightmap_region][element][3]");
                sb.AppendLine("                <[uint32]              [level]  [11]>");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_min]   [{westLng} {southLat}]>// [<West> <Süd>]");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_max]   [{eastLng} {northLat}]>// [<Ost> <Nord>]");
                sb.AppendLine("                <[bool]                [write_images_with_mask][false]>");
                sb.AppendLine("            >");
                sb.AppendLine();
            }
            else if (gridSpuareLevel >= 12)
            {
                sb.AppendLine("            <[tmheightmap_region][element][3]    //WARNING: Selected Grid Square Level to small for compile (masking not supportet by FS4)");
                sb.AppendLine("                <[uint32]              [level]  [10]>");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_min]   [{westLng} {southLat}]>// [<West> <Süd>]");
                sb.AppendLine($"                <[vector2_float64]     [lonlat_max]   [{eastLng} {northLat}]>// [<Ost> <Nord>]");
                sb.AppendLine("                <[bool]                [write_images_with_mask][false]>");
                sb.AppendLine("            >");
                sb.AppendLine();
            }

            sb.AppendLine("        >");
            sb.AppendLine("    >");
            sb.AppendLine(">");

            return sb.ToString();
        }
    }

    //#DEVL_k
    public class TMCImagesFile 
    {
        public string InputFolderImages { get; set; }
        public string OutputFolderTTC { get; set; }

        public string GeneratedContent { get; private set; }

        public TMCImagesFile(string inputFolder, string outputFolder)
        {
            InputFolderImages = inputFolder;
            OutputFolderTTC = outputFolder;

            // Text is generated and saved in the constructor
            GeneratedContent = GenerateContent();
        }

        // Generation of the text
        public string GenerateContent()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<[file][][]");
            sb.AppendLine("    <[tm_config][][]");
            sb.AppendLine();
            sb.AppendLine("        <[string8][base_output_folder][./]>");
            sb.AppendLine("        <[string8][texture_base_type][ttc_etc2]>");
            sb.AppendLine();
            sb.AppendLine("        <[list_tm_config_folderpair][folder_pairs][]");
            sb.AppendLine("            <[tm_config_folderpair][element][1]>");
            sb.AppendLine($"                <[string8][input_folder][{InputFolderImages}]>");
            sb.AppendLine($"                <[string8][output_folder][{OutputFolderTTC}]>");
            sb.AppendLine("                <[string8][type][place]>");
            sb.AppendLine("                <[uint32][recurse_level][0]>");
            sb.AppendLine("                <[list_string8][file_types][tsc tgi jpg bmp tif png toc]>");
            sb.AppendLine("                <[list_tm_texture_settings][texture_settings][]");
            sb.AppendLine("                    <[tm_config_folderpair][element][0]");
            sb.AppendLine("                        <[list_string8][regex][.*]>");
            sb.AppendLine("                        <[bool][compressed][true]>");
            sb.AppendLine("                        <[bool][compress_file][true]>");
            sb.AppendLine("                        <[bool][flip_vertical][true]>");
            sb.AppendLine("                        <[bool][mipmaps][true]>");
            sb.AppendLine("                        <[uint][max_size][2048]>");
            sb.AppendLine("                        <[bool][make_square][true]>");
            sb.AppendLine("                    >");
            sb.AppendLine("                >");
            sb.AppendLine("                <[tm_config_geometry_settings][geometry_settings][]");
            sb.AppendLine("                    <[float32][collision_mesh_quality][0]>");
            sb.AppendLine("                >");
            sb.AppendLine("            >");
            sb.AppendLine("        >");
            sb.AppendLine("    >");
            sb.AppendLine(">");

            return sb.ToString();
        }
    }

}
