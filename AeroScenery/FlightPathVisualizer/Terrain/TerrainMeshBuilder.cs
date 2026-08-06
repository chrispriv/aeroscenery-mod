using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelixToolkit.Wpf;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace AeroScenery.FlightPathVisualizer.Terrain
{
    /*
    public class TerrainMeshBuilder
    {

        public MeshGeometry3D BuildTerrainMesh(float[,] heightData, double scaleX = 1.0, double scaleY = 1.0, double heightScale = 1.0)
        {
            int rows = heightData.GetLength(0); // y-Achse
            int cols = heightData.GetLength(1); // x-Achse

            var builder = new MeshBuilder(false, false);

            // Ziel: Zentrum (Flugzeugposition) auf (0,0)
            double offsetX = cols / 2.0 * scaleX;
            double offsetY = rows / 2.0 * scaleY;

            for (int y = 0; y < rows - 1; y++)
            {
                for (int x = 0; x < cols - 1; x++)
                {
                    double px0 = x * scaleX - offsetX;
                    double px1 = (x + 1) * scaleX - offsetX;
                    double py0 = y * scaleY - offsetY;
                    double py1 = (y + 1) * scaleY - offsetY;

                    // 🔁 Spiegelung durch Zugriff auf umgekehrte X-Indizes
                    int sx = cols - 1 - x;
                    int sx1 = cols - 2 - x;

                    Point3D p00 = new Point3D(px0, py0, heightData[y, sx] * heightScale);
                    Point3D p10 = new Point3D(px1, py0, heightData[y, sx1] * heightScale);
                    Point3D p01 = new Point3D(px0, py1, heightData[y + 1, sx] * heightScale);
                    Point3D p11 = new Point3D(px1, py1, heightData[y + 1, sx1] * heightScale);

                    //builder.AddQuad(p00, p10, p11, p01);
                    builder.AddTriangle(p00, p10, p11);
                    builder.AddTriangle(p00, p11, p01);
                }
            }

            return builder.ToMesh();
        }
                
    }
    */
    public class TerrainMeshBuilder
    {
        public MeshGeometry3D BuildTerrainMesh(float[,] heightData, double scaleX = 1.0, double scaleY = 1.0, double heightScale = 1.0)
        {
            int rows = heightData.GetLength(0); // y-Achse
            int cols = heightData.GetLength(1); // x-Achse

            var builder = new MeshBuilder(false, false);

            // Ziel: Zentrum (Flugzeugposition) auf (0,0)
            double offsetX = cols / 2.0 * scaleX;
            double offsetY = rows / 2.0 * scaleY;

            for (int y = 0; y < rows - 1; y++)
            {
                for (int x = 0; x < cols - 1; x++)
                {
                    // 🔁 Spiegelung durch Zugriff auf umgekehrte X-Indizes
                    int sx = cols - 1 - x;
                    int sx1 = cols - 2 - x;

                    float h00 = heightData[y, sx];
                    float h10 = heightData[y, sx1];
                    float h01 = heightData[y + 1, sx];
                    float h11 = heightData[y + 1, sx1];

                    // ⛔ Falls einer der vier Werte NaN ist, überspringen
                    if (float.IsNaN(h00) || float.IsNaN(h10) || float.IsNaN(h01) || float.IsNaN(h11))
                        continue;

                    double px0 = x * scaleX - offsetX;
                    double px1 = (x + 1) * scaleX - offsetX;
                    double py0 = y * scaleY - offsetY;
                    double py1 = (y + 1) * scaleY - offsetY;

                    Point3D p00 = new Point3D(px0, py0, h00 * heightScale);
                    Point3D p10 = new Point3D(px1, py0, h10 * heightScale);
                    Point3D p01 = new Point3D(px0, py1, h01 * heightScale);
                    Point3D p11 = new Point3D(px1, py1, h11 * heightScale);

                    builder.AddTriangle(p00, p10, p11);
                    builder.AddTriangle(p00, p11, p01);
                }
            }

            return builder.ToMesh();
        }
    }


}
