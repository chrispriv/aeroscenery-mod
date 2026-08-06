using AeroScenery.Controls;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;



namespace AeroScenery.AFS2
{
    public class AIDFile
    {
        public string ImageFile { get; set; }
        public bool FlipVertical { get; set; }

        public double StepsPerPixelX { get; set; }
        public double StepsPerPixelY { get; set; }
        public double X { get; set; }
        public double Y { get; set; }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<[file][][]");
            sb.AppendLine("\t<[tm_aerial_image_definition][][]");
            sb.AppendLine(String.Format("\t\t<[string8][image][{0}]>", ImageFile));
            sb.AppendLine("\t\t<[string8][mask][]>");
            sb.AppendLine(String.Format("\t\t<[vector2_float64][steps_per_pixel][{0} {1}]>", 
                StepsPerPixelX.ToString("0.###################################################################################################################################################################################################################################################################################################################################################e-00", CultureInfo.InvariantCulture), 
                StepsPerPixelY.ToString("0.###################################################################################################################################################################################################################################################################################################################################################e-00", CultureInfo.InvariantCulture)));
            sb.AppendLine(String.Format("\t\t<[vector2_float64][top_left][{0} {1}]>", X.ToString(CultureInfo.InvariantCulture), Y.ToString(CultureInfo.InvariantCulture)));
            sb.AppendLine("\t\t<[string8][coordinate_system][lonlat]>");
            sb.AppendLine(String.Format("\t\t<[bool][flip_vertical][{0}]>", FlipVertical.ToString().ToLower()));
            sb.AppendLine("\t>");
            sb.AppendLine(">");

            return sb.ToString();
        }

    }

    public class AIDElevationFile
    {
        public double WestLng { get; set; }
        public double NorthLat { get; set; }

        public double StepsPerPixelX { get; set; }
        public double StepsPerPixelY { get; set; }

        public string MeshResolution { get; set; }

        public string GeneratedContent { get; private set; }

        public AIDElevationFile(double westLng, double northLat, double stepsPerPixelX, double stepsPerPixelY, string meshResolution)
        {
            WestLng = westLng;
            NorthLat = northLat;
            StepsPerPixelX = stepsPerPixelX;
            StepsPerPixelY = stepsPerPixelY;

            MeshResolution = meshResolution;

            // Text is generated and saved in the constructor
            GeneratedContent = GenerateContent();
        }

        // Generation of the text
        public string GenerateContent()
        {
            int meshResolutionMeter = Convert.ToInt16(MeshResolution);

            string westLngText = WestLng.ToString("#.#########", CultureInfo.InvariantCulture);
            string northLatText = NorthLat.ToString("#.#########", CultureInfo.InvariantCulture);
            string stepsPerPixelXText = StepsPerPixelX.ToString("0.###########", CultureInfo.InvariantCulture);
            string stepsPerPixelYText = StepsPerPixelY.ToString("0.###########", CultureInfo.InvariantCulture);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<[file][][]");
            sb.AppendLine("    <[tm_aerial_image_definition][][]");
            sb.AppendLine($"        <[string8][image][dem_area_{MeshResolution}m.tif]>");
            sb.AppendLine("        <[string8][mask][]>");
            sb.AppendLine($"        <[vector2_float64][steps_per_pixel][{stepsPerPixelXText} {stepsPerPixelYText}]> // [<Horizontal> -<Vertical(minus!)>] ");
            /*
            switch (meshResolutionMeter)
            {
                case 10:
                    sb.AppendLine("        <[vector2_float64][steps_per_pixel][0.0000925926 -0.0000925926]> // [<Horizontal> -<Vertical(minus!)>] ");
                    break;
                case 30: //Default
                    sb.AppendLine("        <[vector2_float64][steps_per_pixel][0.000277778 -0.000277778]> // [<Horizontal> -<Vertical(minus!)>] ");
                    break;
                case 90:
                    sb.AppendLine("        <[vector2_float64][steps_per_pixel][0.000833333 -0.000833333]> // [<Horizontal> -<Vertical(minus!)>] ");
                    break;
                default:
                    sb.AppendLine("        <[vector2_float64][steps_per_pixel][0 -0]> // ERROR: Mesh Resolution not detected resp. unkonwn!");
                    break;
            }
            */
            sb.AppendLine($@"        <[vector2_float64][top_left][{westLngText} {northLatText}]> // [<West> <Nord>]");
            sb.AppendLine("        <[string8][coordinate_system][lonlat]>");
            sb.AppendLine("        <[bool][flip_vertical][false]>");
            sb.AppendLine("    >");
            sb.AppendLine(">");

            return sb.ToString();

        }
    }

}
