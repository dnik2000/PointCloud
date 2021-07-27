using System.Numerics;
using System.IO;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PointCloudTools
{
    public static class StreamReaderEx
    {
        public static int GetPosition(this StreamReader stream)
        {
            var charpos = (int)stream.GetType().InvokeMember("charPos",
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.GetField
                 , null, stream, null);

            var charlen = (int)stream.GetType().InvokeMember("charLen",
            BindingFlags.DeclaredOnly |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.GetField
             , null, stream, null);

            return charpos;
            //return (int)stream.BaseStream.Position - charlen + charpos;
        }
    }
    static class MathEx
    {
        public const float fPi = (float)Math.PI;
        public const float ToRadiansMultiplier = (float)(Math.PI / 180.0);
        public static float ToRadians(this float degree)
        {
            return degree * ToRadiansMultiplier;
        }
    }

    public class PointCloud
    {
        public Vector3[] Points = null;
        //public int Width { get; private set; }
        //public int Height { get; private set; }

        public PointCloud()
        {

        }

        public PointCloud(Vector3[] points)
        {
            Points = points;
        }

        public PointCloud(PointCloud other)
        {
            Points = new Vector3[other.Count];
            other.Points.CopyTo(Points, 0);
        }

        public Vector3 this[int x]
        {
            get { return Points[x]; }
            set { Points[x] = value; }
        }

        public void Update(Vector3[] points)
        {
            Points = points;
        }
        public int Count
        {
            get
            {
                return (Points == null) ? 0 : Points.Length;
            }
        }

        public static void test()
        {
            var p = new Plane(0, 0, 1, 0);
            var v1 = new Vector3(10, 1, 10);
            var v2 = new Vector3(1, 1, -10);
            var v3 = new Vector3(5, 5, 0);
            var r1 = Plane.DotCoordinate(p, v1);
            var r2 = Plane.DotCoordinate(p, v2);
            var r3 = Plane.DotCoordinate(p, v3);
        }


        public void SuppressTo(Plane plane, float level, bool invert = false)
        {
            if (Points == null)
                return;
            for (var i = 0; i < Points.Length; i++)
            {
                var p = Points[i];
                var dotp = Plane.DotCoordinate(plane, p);
                if (invert ^ (Plane.DotCoordinate(plane, Points[i]) < 0))
                    Points[i].Z = level;
            }
        }
        public void Transform(Matrix4x4 transformMatrix)
        {
            if (Points == null)
                return;
            for (var i = 0; i < Points.Length; i++)
            {
                Points[i] = Vector3.Transform(Points[i], transformMatrix);
            }
        }

        public void Scale(float x, float y, float z)
        {
            if (Points == null)
                return;

            Transform(GetRotate(x, y, z));
        }
        public void FlatternZ()
        {
            if (Points == null)
                return;

            for (var i = 0; i < Points.Length; i++)
            {
                Points[i].Z = 0;
            }
        }

        public void Rotate(float x, float y, float z)
        {
            if (Points == null)
                return;

            Transform(GetRotate(x, y, z));
        }

        public void Shift(float x, float y, float z)
        {
            if (Points == null)
                return;

            Transform(GetShift(x, y, z));
        }

        public static Matrix4x4 GetRotate(float x, float y, float z)
        {

            return Matrix4x4.CreateFromYawPitchRoll(y.ToRadians(), x.ToRadians(), z.ToRadians());

        }

        public static Matrix4x4 GetScale(float x, float y, float z)
        {

            return Matrix4x4.CreateScale(y, x, z);

        }

        public static Matrix4x4 GetShift(float x, float y, float z)
        {
            return Matrix4x4.CreateTranslation(x, y, z);
        }

        //public static PointCloud operator +(PointCloud p1, PointCloud p2)
        //{
        //    var result = new PointCloud();
        //    result.Points = new Vector3[p1.Count + p2.Count];
        //    p1.Points.CopyTo(result.Points, 0);
        //    p2.Points.CopyTo(result.Points, p1.Count);
        //    //Array.Copy(p1.Points, 0, result.Points, 0, p1.Count);
        //    //Array.Copy(p2.Points, 0, result.Points, p1.Count, p2.Count);
        //    return result;
        //}

        //public void Append(PointCloud other)
        //{
        //    Points = Points.Concat(other.Points).ToArray();
        //}

        public void SaveToBin(string file, int Width = 0, int Height = 0)
        {
            using (var s = File.Create(file))
            using (var bw = new BinaryWriter(s))
            {
                if (Width > 0 && Height > 0)
                {
                    bw.Write(Width);
                    bw.Write(Height);
                }
                foreach (var p in Points)
                {
                    bw.Write(p.X);
                    bw.Write(p.Y);
                    bw.Write(p.Z);
                }
            }
        }
        public void SaveToPly(string file, bool asciiFormat = true, int Width = 0, int Height = 0)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var sw = File.CreateText(file);


            sw.WriteLine("ply");
            if (asciiFormat)
                sw.WriteLine("format ascii 1.0");
            else
                sw.WriteLine("format binary_little_endian 1.0");
            if (Width > 0 && Height > 0)
            {
                sw.WriteLine($"obj_info num_cols {Width}");
                sw.WriteLine($"obj_info num_rows {Height}");
            }
            sw.WriteLine($"element vertex {Count}");
            sw.WriteLine("property float x");
            sw.WriteLine("property float y");
            sw.WriteLine("property float z");
            sw.WriteLine("end_header");

            if (asciiFormat)
            {
                foreach (var p in Points)
                    sw.WriteLine($"{p.X} {p.Y} {p.Z}");
                sw.Dispose();
            }
            else
            {
                sw.Flush();
                using (var bw = new BinaryWriter(sw.BaseStream))
                {
                    foreach (var p in Points)
                    {
                        bw.Write(p.X);
                        bw.Write(p.Y);
                        bw.Write(p.Z);
                    }
                }
            }

        }

        public static PointCloud LoadFromPly(string file, out int width, out int height)
        {
            string line = string.Empty;
            List<Vector3> vectors = null;
            Vector3[] v_result = null;
            width = 0;
            height = 0;


            using (StreamReader streamReader = new StreamReader(file))
            {
                string format = null;
                int vertexCount = 0;
                int rangeGridCount = 0;
                // parse header

                line = streamReader.ReadLine()?.Trim();
                if (line != "ply")
                    throw new FormatException("Ply header not found");

                // find format
                line = streamReader.ReadLine()?.Trim();
                var parts = line.Split(' ');
                if (parts[0] != "format")
                    throw new FormatException("Format not found");
                format = parts[1];

                //find element vertex
                while (!streamReader.EndOfStream)
                {
                    line = streamReader.ReadLine();
                    if (line == null || line == "end_header")
                        break;
                    line = line.Trim();
                    parts = line.Split(' ');

                    if (parts[0] == "obj_info" && parts[1] == "num_cols")
                    {
                        width = int.Parse(parts[2]);
                        continue;
                    }
                    if (parts[0] == "obj_info" && parts[1] == "num_rows")
                    {
                        height = int.Parse(parts[2]);
                        continue;
                    }

                    if (parts[0] != "element")
                        continue;
                    switch (parts[1])
                    {
                        case "vertex":
                            vertexCount = int.Parse(parts[2]);
                            vectors = new List<Vector3>(vertexCount);
                            break;
                        case "range_grid":
                            rangeGridCount = int.Parse(parts[2]);
                            break;
                        default:
                            throw new FormatException("Can't found vretex count");
                    }
                }

                //while (!streamReader.EndOfStream)
                //{
                //    line = streamReader.ReadLine();
                //    if (line == null)
                //        break;
                //    line = line.Trim();
                //    if (line == "end_header")
                //        break;
                //}

                if (format == "ascii")
                {
                    for (var vCnt = 0; vCnt < vertexCount; vCnt++)
                    {
                        line = streamReader.ReadLine()?.Trim();
                        parts = line.Split(' ');
                        var x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                        var y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        var z = float.Parse(parts[2], CultureInfo.InvariantCulture);

                        vectors.Add(new Vector3(x, y, z));
                    }

                    v_result = vectors.ToArray();

                    /// TODO: 
                    /// range_grid in ascii  mode not implemented yet!!!
                }
                else
                {
                    //streamReader.DiscardBufferedData();
                    var currentPos = streamReader.GetPosition();
                    //streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(currentPos, SeekOrigin.Begin);
                    using (var br = new BinaryReader(streamReader.BaseStream))
                    {
                        //br.ReadString()
                        for (var vCnt = 0; vCnt < vertexCount; vCnt++)
                        {
                            var x = br.ReadSingle();
                            var y = br.ReadSingle();
                            var z = br.ReadSingle();
                            var r = br.ReadByte();
                            var g = br.ReadByte();
                            var b = br.ReadByte();

                            vectors.Add(new Vector3(x, y, -z));
                        }
                        v_result = vectors.ToArray();

                        // read range_grid if defined
                        if (rangeGridCount > 0)
                        {
                            var array = new Vector3[rangeGridCount];
                            for (var rgCnt = 0; rgCnt < rangeGridCount; rgCnt++)
                            {
                                var cnt = br.ReadByte();
                                if (cnt == 0)
                                    array[rgCnt] = new Vector3(float.NaN, float.NaN, float.NaN);
                                else
                                {
                                    var vIdx = br.ReadInt32();
                                    array[rgCnt] = v_result[vIdx];
                                }
                            }
                            v_result = array;
                        }
                    }
                }
                streamReader.Close();
            }

            if (v_result.Length != width * height)
            {
                width = 0;
                height = 0;
            }
            var pc = new PointCloud(v_result);
            vectors.Clear();
            return pc;
        }
    }
}
