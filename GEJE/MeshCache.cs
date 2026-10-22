using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GEJE
{
    /// <summary>
    /// Mesh geometry stored as two flat arrays, so it is quick to copy and to keep in memory.
    /// Every triangle takes 9 numbers in pos (x, y, z of its 3 corners, in Polygon p1, p2, p3
    /// order) and 9 bytes in col (r, g, b of those corners).
    /// </summary>
    public class MeshData
    {
        public readonly double[] pos;
        public readonly byte[] col;

        public int TriangleCount => pos.Length / 9;

        public MeshData(double[] pos, byte[] col)
        {
            if (pos.Length % 9 != 0 || col.Length != pos.Length)
                throw new ArgumentException("pos and col need 9 entries per triangle.");
            this.pos = pos;
            this.col = col;
        }

        /// <summary>Snapshot of a list of polygons (later changes to the polygons don't affect it).</summary>
        public static MeshData FromPolygons(List<Polygon> polys)
        {
            var pos = new double[polys.Count * 9];
            var col = new byte[polys.Count * 9];
            int i = 0;
            foreach (Polygon p in polys)
            {
                Put(pos, col, ref i, p.p1);
                Put(pos, col, ref i, p.p2);
                Put(pos, col, ref i, p.p3);
            }
            return new MeshData(pos, col);
        }

        private static void Put(double[] pos, byte[] col, ref int i, Point p)
        {
            pos[i] = p.x; pos[i + 1] = p.y; pos[i + 2] = p.z;
            col[i] = p.r; col[i + 1] = p.g; col[i + 2] = p.b;
            i += 3;
        }

        /// <summary>New polygons with their own points: changing them never touches this data.</summary>
        public List<Polygon> ToPolygons()
        {
            int n = TriangleCount;
            var list = new List<Polygon>(n);
            for (int t = 0, i = 0; t < n; t++, i += 9)
                list.Add(new Polygon(MakePoint(i), MakePoint(i + 3), MakePoint(i + 6)));
            return list;
        }

        private Point MakePoint(int i) => new Point(pos[i], pos[i + 1], pos[i + 2], 1, col[i], col[i + 1], col[i + 2]);

        /// <summary>
        /// Writes this geometry into polygons that already exist (same triangle count),
        /// reusing their points instead of making new ones.
        /// </summary>
        public void WriteInto(List<Polygon> polys)
        {
            for (int t = 0, i = 0; t < polys.Count; t++, i += 9)
            {
                Polygon p = polys[t];
                Set(p.p1, i); Set(p.p2, i + 3); Set(p.p3, i + 6);
            }
        }

        private void Set(Point p, int i)
        {
            p.x = pos[i]; p.y = pos[i + 1]; p.z = pos[i + 2];
            p.r = col[i]; p.g = col[i + 1]; p.b = col[i + 2];
        }

        /// <summary>
        /// Writes a blend between a (t = 0) and b (t = 1) into existing polygons. Both must have
        /// the same triangle count and the same corner order (e.g. poses of one model).
        /// </summary>
        public static void WriteBlend(MeshData a, MeshData b, double t, List<Polygon> polys)
        {
            double s = 1 - t;
            for (int k = 0, i = 0; k < polys.Count; k++, i += 9)
            {
                Polygon p = polys[k];
                Blend(a, b, s, t, p.p1, i); Blend(a, b, s, t, p.p2, i + 3); Blend(a, b, s, t, p.p3, i + 6);
            }
        }

        private static void Blend(MeshData a, MeshData b, double s, double t, Point p, int i)
        {
            p.x = a.pos[i] * s + b.pos[i] * t;
            p.y = a.pos[i + 1] * s + b.pos[i + 1] * t;
            p.z = a.pos[i + 2] * s + b.pos[i + 2] * t;
            p.r = (byte)Math.Round(a.col[i] * s + b.col[i] * t);
            p.g = (byte)Math.Round(a.col[i + 1] * s + b.col[i + 1] * t);
            p.b = (byte)Math.Round(a.col[i + 2] * s + b.col[i + 2] * t);
        }

        public MeshData Clone() => new MeshData((double[])pos.Clone(), (byte[])col.Clone());

        /// <summary>Same as Mesh.scale, on this data (changes it in place).</summary>
        public void Scale(double x, double y, double z)
        {
            for (int i = 0; i < pos.Length; i += 3) { pos[i] *= x; pos[i + 1] *= y; pos[i + 2] *= z; }
        }

        /// <summary>Same as Mesh.hueit, on this data (changes it in place).</summary>
        public void Hue(int r, int g, int b)
        {
            for (int i = 0; i < col.Length; i += 3)
            {
                col[i] = (byte)(col[i] + r % 256);
                col[i + 1] = (byte)(col[i + 1] + g % 256);
                col[i + 2] = (byte)(col[i + 2] + b % 256);
            }
        }

        /// <summary>Same as Mesh.hardsetcolor, on this data (changes it in place).</summary>
        public void SetColor(int r, int g, int b)
        {
            for (int i = 0; i < col.Length; i += 3)
            {
                col[i] = (byte)(r % 256); col[i + 1] = (byte)(g % 256); col[i + 2] = (byte)(b % 256);
            }
        }

        /// <summary>
        /// Reads a GEJE model file: a JSON list of [x, y, z, r, g, b], three per triangle.
        /// Corners are stored in the same order the original Mesh loader used.
        /// </summary>
        public static MeshData LoadFile(string fullPath)
        {
            string json = File.ReadAllText(fullPath);
            return FromRows(JsonSerializer.Deserialize<List<List<double>>>(json));
        }

        /// <summary>
        /// Mesh data from [x, y, z, r, g, b] rows, three per triangle (the model file format).
        /// Corners are stored in the same order the original Mesh loader used.
        /// </summary>
        public static MeshData FromRows(List<List<double>> data)
        {
            int tris = data.Count / 3;
            var pos = new double[tris * 9];
            var col = new byte[tris * 9];
            for (int t = 0; t < tris; t++)
            {
                // The original loader builds Polygon(third, first, second)
                List<double> a = data[t * 3], b = data[t * 3 + 1], c = data[t * 3 + 2];
                int i = t * 9;
                Put(pos, col, i, c); Put(pos, col, i + 3, a); Put(pos, col, i + 6, b);
            }
            return new MeshData(pos, col);
        }

        private static void Put(double[] pos, byte[] col, int i, List<double> v)
        {
            pos[i] = v[0]; pos[i + 1] = v[1]; pos[i + 2] = v[2];
            col[i] = (byte)v[3]; col[i + 1] = (byte)v[4]; col[i + 2] = (byte)v[5];
        }
    }

    /// <summary>
    /// An animation file made by the GEJE Mesh Workshop (.anim.json): every frame as mesh data,
    /// plus the frames per second, loop and smooth settings chosen in the workshop.
    /// Load it with AnimatedMesh.AddAnimationFile (or MeshCache.GetAnimationFile).
    /// </summary>
    public class MeshAnimationFile
    {
        public readonly MeshData[] frames;
        public readonly double fps;
        public readonly bool loop;
        public readonly bool smooth;

        public MeshAnimationFile(MeshData[] frames, double fps, bool loop, bool smooth)
        {
            this.frames = frames;
            this.fps = fps;
            this.loop = loop;
            this.smooth = smooth;
        }

        private class Json
        {
            public double Fps { get; set; } = 8;
            public bool Loop { get; set; } = true;
            public bool Smooth { get; set; } = false;
            public List<List<List<double>>> Frames { get; set; }
        }

        public static MeshAnimationFile LoadFile(string fullPath)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            Json file = JsonSerializer.Deserialize<Json>(File.ReadAllText(fullPath), options);
            if (file == null || file.Frames == null || file.Frames.Count == 0)
                throw new InvalidDataException(Path.GetFileName(fullPath) +
                    " has no frames. Export it from the Mesh Workshop with \"Whole animation\" selected.");
            var frames = new MeshData[file.Frames.Count];
            for (int i = 0; i < frames.Length; i++) frames[i] = MeshData.FromRows(file.Frames[i]);
            return new MeshAnimationFile(frames, file.Fps > 0 ? file.Fps : 8, file.Loop, file.Smooth);
        }
    }

    /// <summary>
    /// Keeps mesh data in memory so each model file is read from disk only once.
    ///
    ///     MeshCache.Preload("Models\\Crate.json", "Models\\Tree.json");   // optional, at startup
    ///     Mesh m = MeshCache.Create("Models\\Crate.json");                 // fast, no disk
    ///
    /// new Mesh("Models\\Crate.json", ...) goes through the cache automatically too.
    /// You can also save a mesh you've already scaled / colored under any name:
    ///
    ///     MeshCache.Store("BigRedCrate", someMesh);
    ///     Mesh copy = MeshCache.Create("BigRedCrate", x, y, z, 0, 0, 0);
    ///
    /// Every mesh you get back has its own points, so changing one never changes another.
    /// </summary>
    public static class MeshCache
    {
        private static readonly Dictionary<string, MeshData> cache =
            new Dictionary<string, MeshData>(StringComparer.OrdinalIgnoreCase);
        private static readonly object sync = new object();
        private static string basePath;

        /// <summary>
        /// Folder model paths are relative to. Defaults to the project's GEJE folder (found from
        /// the working directory, like Mesh.getPath). Set it yourself if your files live elsewhere.
        /// </summary>
        public static string BasePath
        {
            get
            {
                if (basePath == null)
                {
                    string dir = Directory.GetCurrentDirectory();
                    int at = dir.LastIndexOf("GEJE", StringComparison.Ordinal);
                    basePath = at >= 0 ? dir.Substring(0, at + 4) : dir;
                }
                return basePath;
            }
            set { basePath = value; }
        }

        /// <summary>Full path for a model file name like "Models\\Crate.json".</summary>
        public static string FullPath(string file)
        {
            if (Path.IsPathRooted(file)) return file;
            string rel = file.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(BasePath, rel);
        }

        private static string Key(string name) => name.Replace('/', '\\');

        /// <summary>
        /// The cached data for a stored name or model file (loads the file the first time).
        /// This is the shared copy: don't change it, use .Clone() if you need to.
        /// </summary>
        public static MeshData GetData(string fileOrName)
        {
            string key = Key(fileOrName);
            lock (sync)
            {
                if (cache.TryGetValue(key, out MeshData data)) return data;
            }
            MeshData loaded = MeshData.LoadFile(FullPath(fileOrName));   // read outside the lock
            lock (sync)
            {
                if (!cache.TryGetValue(key, out MeshData existing)) cache[key] = existing = loaded;
                return existing;
            }
        }

        private static readonly Dictionary<string, MeshAnimationFile> animCache =
            new Dictionary<string, MeshAnimationFile>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The cached contents of a Mesh Workshop animation file (.anim.json), read from disk the
        /// first time. Shared: don't change its frames, Clone() them (AddAnimationFile does).
        /// </summary>
        public static MeshAnimationFile GetAnimationFile(string file)
        {
            string key = Key(file);
            lock (sync)
            {
                if (animCache.TryGetValue(key, out MeshAnimationFile a)) return a;
            }
            MeshAnimationFile loaded = MeshAnimationFile.LoadFile(FullPath(file));   // read outside the lock
            lock (sync)
            {
                if (!animCache.TryGetValue(key, out MeshAnimationFile existing)) animCache[key] = existing = loaded;
                return existing;
            }
        }

        /// <summary>Read model files into memory now, so the first use later doesn't stall.</summary>
        public static void Preload(params string[] files)
        {
            foreach (string f in files) GetData(f);
        }

        /// <summary>
        /// Save the current shape and colors of a mesh under a name (a snapshot: changing the
        /// mesh afterwards doesn't change what was saved). Replaces anything with that name.
        /// </summary>
        public static void Store(string name, Mesh mesh) => Store(name, MeshData.FromPolygons(mesh.oldpoints));

        public static void Store(string name, MeshData data)
        {
            lock (sync) cache[Key(name)] = data;
        }

        /// <summary>A new Mesh from a stored name or model file, with its own copy of the points.</summary>
        public static Mesh Create(string fileOrName, double x = 0, double y = 0, double z = 0,
                                  double xrot = 0, double yrot = 0, double zrot = 0)
            => new Mesh(GetData(fileOrName), x, y, z, xrot, yrot, zrot);

        public static bool Contains(string fileOrName) { lock (sync) return cache.ContainsKey(Key(fileOrName)); }

        /// <summary>Forget one entry (e.g. after editing a model file while the game runs).</summary>
        public static bool Remove(string fileOrName)
        {
            lock (sync) return cache.Remove(Key(fileOrName)) | animCache.Remove(Key(fileOrName));
        }

        public static void Clear() { lock (sync) { cache.Clear(); animCache.Clear(); } }
    }
}