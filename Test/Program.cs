using PointCloudTools;
using System;
using System.Numerics;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            const int width = 100;
            const int height = 100;
            var points = new Vector3[width*height];

            var halfHeight = (float)height / 2.0f;
            var halfWidth = (float)width / 2.0f;

            for (var h = 0; h < height; h++)
            {
                var tmpY = MathF.Sin(2f * MathF.PI * ((float)h) / halfHeight);
                for (var w = 0; w < width; w++)
                {
                    var tmpX = MathF.Sin(2f * MathF.PI * ((float)w) / halfWidth);
                    //tmpX *= tmpX;
                    var p = new Vector3
                    {
                        X = w,
                        Y = h,
                        Z = 10f * tmpX * tmpY
                    };
                    points[w + width * h] = p;
                }


                var cloud = new PointCloud(points);
                cloud.SaveToPly("test.ply");
            }
        }
    }
}

