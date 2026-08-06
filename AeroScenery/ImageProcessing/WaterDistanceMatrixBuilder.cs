using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

//#MOD_k
namespace AeroScenery.ImageProcessing
{
    public class WaterDistanceMatrixBuilder
    {
        private int[,] distances;
        private bool[,] visited;
        private int width;
        private int height;

        private readonly int[] dx = { -1, 1, 0, 0 };
        private readonly int[] dy = { 0, 0, -1, 1 };

        public int[,] BuildWaterDistanceMatrix(OsmFeatureMatrix matrix)
        {
            width = matrix.Width;
            height = matrix.Height;
            distances = new int[width, height];
            visited = new bool[width, height];

            Queue<Point> queue = new Queue<Point>();

            // 1. Vorinitialisierung
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var feature = matrix.GetFeature(x, y);
                    distances[x, y] = int.MaxValue;

                    if (IsWater(feature) && IsEdgeOfWater(matrix, x, y))
                    {
                        distances[x, y] = 1;
                        visited[x, y] = true;
                        queue.Enqueue(new Point(x, y));
                    }
                }
            }

            // 2. BFS-Schleife ab Wasserrändern
            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                int x = p.X;
                int y = p.Y;
                int d = distances[x, y];

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                        IsWater(matrix.GetFeature(nx, ny)) &&
                        !visited[nx, ny])
                    {
                        distances[nx, ny] = d + 1;
                        visited[nx, ny] = true;
                        queue.Enqueue(new Point(nx, ny));
                    }
                }
            }

            return distances;
        }

        private bool IsWater(OsmFeatureType type)
        {
            return type == OsmFeatureType.Water1 || type == OsmFeatureType.Water2 || type == OsmFeatureType.Water3;
        }

        private bool IsEdgeOfWater(OsmFeatureMatrix matrix, int x, int y)
        {
            var feature = matrix.GetFeature(x, y);
            if (!IsWater(feature)) return false;

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && nx < matrix.Width && ny >= 0 && ny < matrix.Height)
                {
                    if (!IsWater(matrix.GetFeature(nx, ny)))
                        return true;
                }
            }

            return false;
        }
    }

}
