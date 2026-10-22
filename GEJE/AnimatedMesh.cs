using System;
using System.Collections.Generic;

namespace GEJE
{
    /// <summary>
    /// One animation: a list of frame names and how long each one shows.
    /// Get one from AnimatedMesh.AddAnimation and change its settings if you like.
    /// </summary>
    public class MeshAnimation
    {
        public readonly string name;
        public readonly List<string> frames = new List<string>();
        /// <summary>Seconds each frame shows, one per frame.</summary>
        public readonly List<double> durations = new List<double>();
        /// <summary>Start over at the end (true) or stop on the last frame (false).</summary>
        public bool loop;
        /// <summary>
        /// Blend smoothly from each frame into the next instead of snapping. Only works
        /// between frames with the same triangle count and corner order (poses of one model);
        /// other pairs just snap.
        /// </summary>
        public bool smooth;

        public MeshAnimation(string name, bool loop)
        {
            this.name = name;
            this.loop = loop;
        }

        public double Length
        {
            get { double t = 0; foreach (double d in durations) t += d; return t; }
        }

        internal MeshAnimation Clone()
        {
            var a = new MeshAnimation(name, loop) { smooth = smooth };
            a.frames.AddRange(frames);
            a.durations.AddRange(durations);
            return a;
        }
    }

    /// <summary>
    /// A mesh that flips between different meshes ("frames") to animate. It holds any number
    /// of frames and any number of animations built from them.
    ///
    ///     var anim = new AnimatedMesh(0, 0, 0, 0, 0, 0);
    ///     anim.AddFrame("stand", "Models\\Guy_stand.json");
    ///     anim.AddFrame("step1", "Models\\Guy_step1.json");
    ///     anim.AddFrame("step2", "Models\\Guy_step2.json");
    ///     anim.scale(5, 5, 5);                              // scales every frame
    ///     anim.AddAnimation("idle", 1, true, "stand");
    ///     anim.AddAnimation("walk", 8, true, "step1", "stand", "step2", "stand").smooth = true;
    ///     anim.Play("idle");
    ///     item.add_propertie(anim);
    ///     ...
    ///     anim.Play("walk");
    ///
    /// Animations made in the Mesh Workshop (.anim.json files) add all their frames at once:
    ///
    ///     anim.AddAnimationFile("wave", "Models\\Guy_wave.anim.json");
    ///
    /// This one is only for looks: physics ignores it. Use PhysicsAnimatedMesh if the
    /// collision shape should follow the animation. A PhysicsBody on an item whose only
    /// mesh is an AnimatedMesh has nothing to collide with, so give it a collider
    /// (SetColliderBox) or a normal Mesh.
    /// </summary>
    public class AnimatedMesh : Mesh
    {
        /// <summary>Frames and animations. Copies made with Duplicate share it until one of them changes it.</summary>
        private class Library
        {
            public Dictionary<string, MeshData> frames = new Dictionary<string, MeshData>();
            public Dictionary<string, MeshAnimation> animations = new Dictionary<string, MeshAnimation>();
        }

        private Library lib = new Library();
        private bool libShared = false;

        // Scale applied so far, so frames added later get it too
        private double scaleX = 1, scaleY = 1, scaleZ = 1;

        /// <summary>Playback speed: 1 = normal, 2 = double speed, 0.5 = half speed.</summary>
        public double speed = 1;

        /// <summary>Called when a non-looping animation reaches its end (mesh, animation name).</summary>
        public event Action<AnimatedMesh, string> OnAnimationFinished;

        public string CurrentAnimation { get; private set; }
        /// <summary>The frame showing now (when blending, the one blending from).</summary>
        public string CurrentFrame { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        /// <summary>Seconds into the current animation.</summary>
        public double Time { get; private set; }

        private string playAfter;

        // What's in oldpoints right now, so nothing is rewritten when it hasn't changed
        private MeshData shownA, shownB;
        private double shownT = -1;

        public AnimatedMesh(double x, double y, double z, double xrot, double yrot, double zrot)
            : base(new List<Polygon>(), x, y, z, xrot, yrot, zrot)
        {
            collides = false;
        }

        public IEnumerable<string> FrameNames => lib.frames.Keys;
        public IEnumerable<string> AnimationNames => lib.animations.Keys;
        public bool HasFrame(string name) => lib.frames.ContainsKey(name);
        public bool HasAnimation(string name) => lib.animations.ContainsKey(name);
        public MeshAnimation GetAnimation(string name) { OwnLibrary(); return lib.animations[name]; }

        // ---------------- building ----------------

        /// <summary>Add a frame from a model file (read through MeshCache, so repeats are free).</summary>
        public void AddFrame(string name, string modelFile) => AddFrameData(name, MeshCache.GetData(modelFile).Clone());

        /// <summary>Add a frame copied from a mesh's current shape and colors.</summary>
        public void AddFrame(string name, Mesh mesh) => AddFrameData(name, MeshData.FromPolygons(mesh.oldpoints));

        /// <summary>Add a frame from mesh data (copied, so the original isn't changed).</summary>
        public void AddFrame(string name, MeshData data) => AddFrameData(name, data.Clone());

        private void AddFrameData(string name, MeshData data)
        {
            OwnLibrary();
            if (scaleX != 1 || scaleY != 1 || scaleZ != 1) data.Scale(scaleX, scaleY, scaleZ);
            lib.frames[name] = data;
            if (CurrentFrame == null) ShowFrame(name);       // the first frame shows right away
            else if (CurrentFrame == name) Refresh();       // replaced the frame on screen
        }

        /// <summary>
        /// Adds an animation made in the GEJE Mesh Workshop (a .anim.json file). Each frame in the
        /// file becomes a frame called name_0, name_1, ... and the animation plays them with the
        /// FPS, Loop and Smooth settings from the workshop. Returns the animation so you can
        /// change those settings here.
        ///
        ///     guyMesh.AddAnimationFile("walk", "Models\\Guy_walk.anim.json");
        ///     guyMesh.AddAnimationFile("wave", "Models\\Guy_wave.anim.json").loop = false;
        ///     guyMesh.Play("walk");
        /// </summary>
        public MeshAnimation AddAnimationFile(string name, string animFile)
        {
            MeshAnimationFile file = MeshCache.GetAnimationFile(animFile);
            var names = new string[file.frames.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = name + "_" + i;
                AddFrameData(names[i], file.frames[i].Clone());
            }
            MeshAnimation anim = AddAnimation(name, file.fps, file.loop, names);
            anim.smooth = file.smooth;
            return anim;
        }

        public bool RemoveFrame(string name)
        {
            OwnLibrary();
            return lib.frames.Remove(name);
        }

        /// <summary>Add an animation where every frame shows for the same time (fps frames per second).</summary>
        public MeshAnimation AddAnimation(string name, double fps, bool loop, params string[] frameNames)
        {
            if (fps <= 0) throw new ArgumentException("fps must be more than 0", nameof(fps));
            var durations = new double[frameNames.Length];
            for (int i = 0; i < durations.Length; i++) durations[i] = 1.0 / fps;
            return AddAnimation(name, frameNames, durations, loop);
        }

        /// <summary>Add an animation with its own time (in seconds) for each frame.</summary>
        public MeshAnimation AddAnimation(string name, string[] frameNames, double[] secondsPerFrame, bool loop)
        {
            if (frameNames.Length == 0) throw new ArgumentException("An animation needs at least one frame.");
            if (frameNames.Length != secondsPerFrame.Length) throw new ArgumentException("Need one duration per frame.");
            OwnLibrary();
            var anim = new MeshAnimation(name, loop);
            for (int i = 0; i < frameNames.Length; i++)
            {
                if (!lib.frames.ContainsKey(frameNames[i]))
                    throw new ArgumentException($"No frame called \"{frameNames[i]}\". Add it with AddFrame first.");
                anim.frames.Add(frameNames[i]);
                anim.durations.Add(Math.Max(1e-6, secondsPerFrame[i]));
            }
            lib.animations[name] = anim;
            if (CurrentAnimation == name) Time = Math.Min(Time, anim.Length);
            return anim;
        }

        public bool RemoveAnimation(string name)
        {
            OwnLibrary();
            if (CurrentAnimation == name) Stop();
            return lib.animations.Remove(name);
        }

        // ---------------- playing ----------------

        /// <summary>
        /// Play an animation. Calling it again with the animation that's already playing does
        /// nothing unless restart is true, so it's safe to call every frame (e.g. while W is held).
        /// thenPlay: for a non-looping animation, what to play when it ends (e.g. "attack" then "idle").
        /// </summary>
        public void Play(string name, bool restart = false, string thenPlay = null)
        {
            if (!lib.animations.ContainsKey(name))
                throw new ArgumentException($"No animation called \"{name}\".");
            playAfter = thenPlay;
            if (!restart && CurrentAnimation == name && IsPlaying) { IsPaused = false; return; }
            CurrentAnimation = name;
            Time = 0;
            IsPlaying = true;
            IsPaused = false;
            ShowAnimationTime();
        }

        /// <summary>Stop playing and keep showing the current frame.</summary>
        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            playAfter = null;
        }

        public void Pause() { if (IsPlaying) IsPaused = true; }
        public void Resume() { IsPaused = false; }

        /// <summary>Stop any animation and show one frame.</summary>
        public void ShowFrame(string name)
        {
            if (!lib.frames.TryGetValue(name, out MeshData data))
                throw new ArgumentException($"No frame called \"{name}\".");
            Stop();
            CurrentAnimation = null;
            CurrentFrame = name;
            Show(data, null, 0);
        }

        /// <summary>Jump to a time (seconds) in the current animation.</summary>
        public void Seek(double seconds)
        {
            if (CurrentAnimation == null) return;
            Time = Math.Max(0, seconds);
            ShowAnimationTime();
        }

        public override void Update()
        {
            if (IsPlaying && !IsPaused && CurrentAnimation != null)
                Advance(ThreeDSceen.deltaTime * Math.Max(0, speed));
            base.Update();   // moves and rotates the current frame like any other mesh
        }

        private void Advance(double dt)
        {
            if (!lib.animations.TryGetValue(CurrentAnimation, out MeshAnimation anim)) { Stop(); return; }
            double length = anim.Length;
            Time += dt;

            if (Time >= length)
            {
                if (anim.loop)
                {
                    Time = length > 0 ? Time % length : 0;
                }
                else
                {
                    Time = length;
                    ShowAnimationTime();
                    string finished = CurrentAnimation, next = playAfter;
                    Stop();
                    OnAnimationFinished?.Invoke(this, finished);
                    if (next != null && !IsPlaying && lib.animations.ContainsKey(next)) Play(next);
                    return;
                }
            }
            ShowAnimationTime();
        }

        /// <summary>Show the frame (or blend of two frames) for Time in the current animation.</summary>
        private void ShowAnimationTime()
        {
            MeshAnimation anim = lib.animations[CurrentAnimation];
            int n = anim.frames.Count;
            int i = 0;
            double start = 0;
            while (i < n - 1 && Time >= start + anim.durations[i]) { start += anim.durations[i]; i++; }

            MeshData a = lib.frames[anim.frames[i]];
            CurrentFrame = anim.frames[i];
            if (!anim.smooth) { Show(a, null, 0); return; }

            int next = i + 1 < n ? i + 1 : (anim.loop ? 0 : i);
            double t = Math.Max(0, Math.Min(1, (Time - start) / anim.durations[i]));
            Show(a, lib.frames[anim.frames[next]], t);
        }

        /// <summary>Put frame a (blended toward b by t) into oldpoints.</summary>
        private void Show(MeshData a, MeshData b, double t)
        {
            if (b != null && (b == a || b.TriangleCount != a.TriangleCount || t <= 0)) b = null;
            if (b == null) t = 0;
            if (a == shownA && b == shownB && t == shownT) return;   // already showing this

            if (oldpoints.Count != a.TriangleCount)
                oldpoints = a.ToPolygons();             // different model: new points
            else if (b == null)
                a.WriteInto(oldpoints);                 // same size: reuse the points
            if (b != null)
                MeshData.WriteBlend(a, b, t, oldpoints);

            shownA = a; shownB = b; shownT = t;
            GeometryChanged();
        }

        /// <summary>Show whatever should be showing again (after frames were changed).</summary>
        private void Refresh()
        {
            shownA = shownB = null; shownT = -1;
            if (CurrentAnimation != null && lib.animations.ContainsKey(CurrentAnimation)) ShowAnimationTime();
            else if (CurrentFrame != null && lib.frames.TryGetValue(CurrentFrame, out MeshData d)) Show(d, null, 0);
        }

        // ---------------- changing every frame at once ----------------

        /// <summary>Scales every frame (and frames you add later).</summary>
        public override void scale(double x, double y, double z)
        {
            OwnLibrary();
            foreach (MeshData d in lib.frames.Values) d.Scale(x, y, z);
            scaleX *= x; scaleY *= y; scaleZ *= z;
            Refresh();
        }

        /// <summary>Shifts the colors of every frame.</summary>
        public override void hueit(int r, int g, int b)
        {
            OwnLibrary();
            foreach (MeshData d in lib.frames.Values) d.Hue(r, g, b);
            Refresh();
            colorchanged = true;
        }

        /// <summary>Sets the color of every frame.</summary>
        public override void hardsetcolor(int r, int g, int b)
        {
            OwnLibrary();
            foreach (MeshData d in lib.frames.Values) d.SetColor(r, g, b);
            Refresh();
            colorchanged = true;
        }

        /// <summary>Before changing frames or animations: if a copy shares them, make our own first.</summary>
        private void OwnLibrary()
        {
            if (!libShared) return;
            var mine = new Library();
            var remap = new Dictionary<MeshData, MeshData>();
            foreach (var kv in lib.frames) remap[kv.Value] = mine.frames[kv.Key] = kv.Value.Clone();
            foreach (var kv in lib.animations) mine.animations[kv.Key] = kv.Value.Clone();
            lib = mine;
            libShared = false;
            // keep pointing at the frames we're showing, now our own copies
            if (shownA != null && remap.TryGetValue(shownA, out MeshData a)) shownA = a;
            if (shownB != null && remap.TryGetValue(shownB, out MeshData b)) shownB = b;
        }

        /// <summary>
        /// An independent copy, playing the same animation at the same time. Frames are shared
        /// behind the scenes until either copy changes them (scale, color, AddFrame...), so
        /// making lots of copies is cheap. Event handlers are not copied.
        /// </summary>
        public override Proportie Duplicate()
        {
            var copy = (AnimatedMesh)base.Duplicate();   // own oldpoints / points (see Mesh.Duplicate)
            libShared = true;
            copy.libShared = true;
            copy.OnAnimationFinished = null;
            copy.shownA = copy.shownB = null; copy.shownT = -1;   // its oldpoints are new objects
            copy.Refresh();
            return copy;
        }
    }

    /// <summary>
    /// An AnimatedMesh whose collision shape follows the animation: a PhysicsBody on the same
    /// item collides with the frame that's showing (a door that swings, a platform that grows,
    /// a character whose arm swings into things).
    ///
    /// The collision shape is rebuilt each time the frame changes, and every tick while a smooth
    /// animation blends, so keep its models simple. If only the looks need to animate, use
    /// AnimatedMesh with SetColliderBox instead, which is much cheaper.
    /// Note: if the PhysicsBody has a colliderMesh set, that is used instead of any mesh.
    /// </summary>
    public class PhysicsAnimatedMesh : AnimatedMesh
    {
        public PhysicsAnimatedMesh(double x, double y, double z, double xrot, double yrot, double zrot)
            : base(x, y, z, xrot, yrot, zrot)
        {
            collides = true;
        }
    }
}