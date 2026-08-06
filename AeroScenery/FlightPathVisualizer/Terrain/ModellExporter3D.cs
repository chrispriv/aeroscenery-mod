using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Media.Media3D;

namespace AeroScenery.FlightPathVisualizer.Terrain
{
    public static class ModellExporter3D
    {
        public static void ObjExporter(MeshGeometry3D mesh, string path)
        {
            using (var writer = new StreamWriter(path))
            {
                // 1. Schreibe die Vertices
                foreach (var p in mesh.Positions)
                    writer.WriteLine($"v {p.X} {p.Y} {p.Z}");

                // 2. Schreibe die Normals (optional)
                if (mesh.Normals != null && mesh.Normals.Count == mesh.Positions.Count)
                {
                    foreach (var n in mesh.Normals)
                        writer.WriteLine($"vn {n.X} {n.Y} {n.Z}");
                }

                // 3. Schreibe die Faces (3 Indices pro Dreieck)
                for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
                {
                    int i1 = mesh.TriangleIndices[i] + 1;
                    int i2 = mesh.TriangleIndices[i + 1] + 1;
                    int i3 = mesh.TriangleIndices[i + 2] + 1;
                    writer.WriteLine($"f {i1}//{i1} {i2}//{i2} {i3}//{i3}");
                }
            }
        }


        public static void ColladaExporter(MeshGeometry3D mesh, string filename)
        {
            var sb = new StringBuilder();
            var culture = CultureInfo.InvariantCulture;

            sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            sb.AppendLine(@"<COLLADA xmlns=""http://www.collada.org/2005/11/COLLADASchema"" version=""1.4.1"">");

            // Asset
            sb.AppendLine(@"  <asset><unit name=""meter"" meter=""1""/><up_axis>Z_UP</up_axis></asset>");

            // Geometry data
            sb.AppendLine(@"  <library_geometries>");
            sb.AppendLine(@"    <geometry id=""terrainMesh"" name=""terrainMesh"">");
            sb.AppendLine(@"      <mesh>");

            // Positions
            sb.AppendLine(@"        <source id=""terrainMesh-positions"">");
            sb.AppendLine(@"          <float_array id=""terrainMesh-positions-array"" count=""" + (mesh.Positions.Count * 3) + @""">");

            foreach (var pos in mesh.Positions)
                sb.AppendFormat(culture, "{0} {1} {2} ", pos.X, pos.Y, pos.Z);
            sb.AppendLine(@"</float_array>");
            sb.AppendLine(@"          <technique_common>");
            sb.AppendLine(@"            <accessor source=""#terrainMesh-positions-array"" count=""" + mesh.Positions.Count + @""" stride=""3"">");
            sb.AppendLine(@"              <param name=""X"" type=""float""/>");
            sb.AppendLine(@"              <param name=""Y"" type=""float""/>");
            sb.AppendLine(@"              <param name=""Z"" type=""float""/>");
            sb.AppendLine(@"            </accessor>");
            sb.AppendLine(@"          </technique_common>");
            sb.AppendLine(@"        </source>");

            // Vertices
            sb.AppendLine(@"        <vertices id=""terrainMesh-vertices"">");
            sb.AppendLine(@"          <input semantic=""POSITION"" source=""#terrainMesh-positions""/>");
            sb.AppendLine(@"        </vertices>");

            // Triangles
            sb.AppendLine(@"        <triangles count=""" + (mesh.TriangleIndices.Count / 3) + @""">");
            sb.AppendLine(@"          <input semantic=""VERTEX"" source=""#terrainMesh-vertices"" offset=""0""/>");
            sb.AppendLine(@"          <p>");
            foreach (int index in mesh.TriangleIndices)
                sb.Append(index + " ");
            sb.AppendLine(@"</p>");
            sb.AppendLine(@"        </triangles>");

            sb.AppendLine(@"      </mesh>");
            sb.AppendLine(@"    </geometry>");
            sb.AppendLine(@"  </library_geometries>");

            // Scene
            sb.AppendLine(@"  <library_visual_scenes>");
            sb.AppendLine(@"    <visual_scene id=""Scene"" name=""Scene"">");
            sb.AppendLine(@"      <node id=""terrainNode"" name=""terrainNode"">");
            sb.AppendLine(@"        <instance_geometry url=""#terrainMesh""/>");
            sb.AppendLine(@"      </node>");
            sb.AppendLine(@"    </visual_scene>");
            sb.AppendLine(@"  </library_visual_scenes>");
            sb.AppendLine(@"  <scene><instance_visual_scene url=""#Scene""/></scene>");
            sb.AppendLine(@"</COLLADA>");

            File.WriteAllText(filename, sb.ToString());
        }

    }
}
