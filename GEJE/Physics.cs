using System;
using System.Collections.Generic;
using System.Linq;

namespace GEJE
{

    /// <summary>Small 3D vector used by the physics code.</summary>
    public struct Vec3
    {
        public double X, Y, Z;
        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }

        public static readonly Vec3 Zero = new Vec3(0, 0, 0);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator -(Vec3 a) => new Vec3(-a.X, -a.Y, -a.Z);
        public static Vec3 operator *(Vec3 a, double s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(double s, Vec3 a) => a * s;
        public static Vec3 operator /(Vec3 a, double s) => new Vec3(a.X / s, a.Y / s, a.Z / s);

        public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vec3 Cross(Vec3 a, Vec3 b) =>
            new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public static Vec3 Min(Vec3 a, Vec3 b) => new Vec3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        public static Vec3 Max(Vec3 a, Vec3 b) => new Vec3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        public double LengthSquared => X * X + Y * Y + Z * Z;
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public bool Equals(Vec3 o) => X == o.X && Y == o.Y && Z == o.Z;
        public Vec3 Normalized { get { double l = Length; return l > 1e-12 ? this / l : Zero; } }

        public override string ToString() => $"({X:0.##}, {Y:0.##}, {Z:0.##})";
    }

    /// <summary>
    /// The physics shape of a body: its mesh's vertices and triangles, already rotated
    /// by the item's rotation, relative to the item's position.
    /// </summary>
    internal class CollisionShape
    {
        public Vec3[] verts;              // unique vertices (used as a convex hull by moving bodies)
        public Vec3[] triA, triB, triC;   // every triangle (used exactly by static bodies)
        public Vec3 min, max;             // local bounding box
        public Vec3 Center => (min + max) / 2;
        public bool IsEmpty => verts.Length == 0;
        public int version;
    }

    /// <summary>A static triangle placed in the world, with its bounding box and normal.</summary>
    internal struct WorldTri
    {
        public Vec3 a, b, c, normal, min, max;
    }

    /// <summary>
    /// Gives an Item physical behaviour. Creating one attaches it to the item
    /// and registers it with the world, so a single line is enough.
    ///
    /// The collision shape is the item's actual mesh:
    ///  * Static bodies collide with every triangle exactly, so any shape works:
    ///    ramps, stairs, bowls, terrain, pyramids, hollow rooms.
    ///  * Moving bodies use the convex hull of their mesh (the shape you'd get
    ///    by shrink-wrapping it). Exact for boxes, pyramids, spheres, ramps; a
    ///    hollow or C-shaped moving object behaves as if its gaps were filled in.
    /// </summary>
    public class PhysicsBody : Proportie
    {
        public readonly Item owner;
        public readonly PhysicsWorld world;

        public Vec3 velocity = Vec3.Zero;
        public double mass = 1.0;

        /// <summary>Static bodies never move (floors, walls). Dynamic bodies collide with them.</summary>
        public bool isStatic = false;
        public bool useGravity = true;

        /// <summary>0 = no bounce, 1 = perfectly bouncy.</summary>
        public double bounciness = 0.2;
        /// <summary>How much sliding is slowed on contact (0 = ice, ~1 = very grippy).</summary>
        public double friction = 0.5;
        /// <summary>Air resistance; fraction of velocity lost per second.</summary>
        public double drag = 0.05;

        /// <summary>True if the body is resting on something this step (touching from the gravity side).</summary>
        public bool isGrounded { get; internal set; }

        private Mesh _colliderMesh;
        /// <summary>
        /// Optional invisible mesh used ONLY for physics. When set, collisions use this
        /// mesh's shape instead of the item's visible meshes. It is never added to the
        /// item's properties, so the camera never draws it. Its x/y/z offset and rotation
        /// work just like a visible mesh on the item. Set to null to use the visible meshes.
        /// </summary>
        public Mesh colliderMesh
        {
            get => _colliderMesh;
            set { _colliderMesh = value; shapeDirty = true; }
        }

        /// <summary>Called with the other body whenever this body touches something (once per step per pair).</summary>
        public event Action<PhysicsBody, PhysicsBody> OnCollision;

        private Vec3 accumulatedForce = Vec3.Zero;
        internal Vec3 prevPosition;

        private CollisionShape shape;
        private bool shapeDirty = true;
        private double lastXr, lastYr, lastZr;
        private int lastSignature;

        private WorldTri[] worldTris = new WorldTri[0];
        private Vec3 worldTrisPos;
        private int worldTrisVersion = -1;

        public PhysicsBody(Item owner, PhysicsWorld world, double mass = 1.0) : base(0, 0, 0, 0, 0, 0)
        {
            this.owner = owner;
            this.world = world;
            this.mass = mass;
            owner.add_propertie(this);
            world.Add(this);
            prevPosition = Position;
        }

        public double InverseMass => (isStatic || mass <= 0) ? 0.0 : 1.0 / mass;

        public Vec3 Position
        {
            get => new Vec3(owner.x, owner.y, owner.z);
            set => owner.move(value.X, value.Y, value.Z, owner.w);
        }

        /// <summary>Half the width/height/depth of the shape's bounding box (read only).</summary>
        public Vec3 halfSize { get { var s = Shape; return (s.max - s.min) / 2; } }
        /// <summary>Where the shape's bounding box center is, relative to the item (read only).</summary>
        public Vec3 centerOffset => Shape.Center;

        /// <summary>Continuous push (e.g. thrusters, wind). Units: mass * distance / s^2.</summary>
        public void ApplyForce(Vec3 force) { accumulatedForce = accumulatedForce + force; }

        /// <summary>Instant kick (e.g. a jump or a hit). Changes velocity by impulse / mass.</summary>
        public void ApplyImpulse(Vec3 impulse)
        {
            if (isStatic) return;
            velocity = velocity + impulse * InverseMass;
        }

        /// <summary>Use an invisible mesh loaded from a model file as the collision shape.</summary>
        public void SetColliderMesh(string modelFile, double scaleX = 1, double scaleY = 1, double scaleZ = 1,
                                    double offsetX = 0, double offsetY = 0, double offsetZ = 0)
        {
            Mesh m = new Mesh(modelFile, offsetX, offsetY, offsetZ, 0, 0, 0);
            m.scale(scaleX, scaleY, scaleZ);
            colliderMesh = m;
        }

        /// <summary>
        /// Use an invisible box as the collision shape: size is the full width/height/depth,
        /// offset moves it relative to the item. It rotates with the item like a mesh would.
        /// </summary>
        public void SetColliderBox(Vec3 size, Vec3 offset)
        {
            colliderMesh = MakeBoxMesh(size, offset);
        }
        public void SetColliderBox(Vec3 size) => SetColliderBox(size, Vec3.Zero);

        /// <summary>Go back to using the item's visible meshes as the collision shape.</summary>
        public void UseVisibleMeshes() { colliderMesh = null; }

        /// <summary>
        /// Rebuild the collision shape. Rotation and adding/removing meshes are picked up
        /// automatically; call this if you scale or edit a mesh after the game has started.
        /// </summary>
        public void RebuildShape() { shapeDirty = true; }
        /// <summary>Same as RebuildShape (kept for older code).</summary>
        public void RecalculateBounds() => RebuildShape();

        internal Vec3 ConsumeForce() { Vec3 f = accumulatedForce; accumulatedForce = Vec3.Zero; return f; }
        internal void RaiseCollision(PhysicsBody other) => OnCollision?.Invoke(this, other);

        /// <summary>World-space bounding box of the collision shape.</summary>
        public void GetBounds(out Vec3 min, out Vec3 max)
        {
            var s = Shape;
            Vec3 p = Position;
            min = s.min + p;
            max = s.max + p;
        }

        internal CollisionShape Shape
        {
            get
            {
                int sig = MeshSignature();
                if (shape == null || shapeDirty || sig != lastSignature
                    || owner.xrot != lastXr || owner.yrot != lastYr || owner.zrot != lastZr)
                {
                    BuildShape();
                    lastSignature = sig;
                }
                return shape;
            }
        }

        /// <summary>Static bodies: their triangles in world space (cached until they move or rotate).</summary>
        internal WorldTri[] WorldTriangles
        {
            get
            {
                var s = Shape;
                Vec3 p = Position;
                if (worldTrisVersion == s.version && worldTrisPos.X == p.X && worldTrisPos.Y == p.Y && worldTrisPos.Z == p.Z)
                    return worldTris;

                var list = new List<WorldTri>(s.triA.Length);
                for (int i = 0; i < s.triA.Length; i++)
                {
                    Vec3 a = s.triA[i] + p, b = s.triB[i] + p, c = s.triC[i] + p;
                    Vec3 n = Vec3.Cross(b - a, c - a).Normalized;
                    if (n.LengthSquared == 0) continue;   // zero-area triangle, nothing to hit
                    list.Add(new WorldTri
                    {
                        a = a,
                        b = b,
                        c = c,
                        normal = n,
                        min = Vec3.Min(a, Vec3.Min(b, c)),
                        max = Vec3.Max(a, Vec3.Max(b, c)),
                    });
                }
                worldTris = list.ToArray();
                worldTrisPos = p;
                worldTrisVersion = s.version;
                return worldTris;
            }
        }

        private int MeshSignature()
        {
            if (_colliderMesh != null) return -1;
            int sig = 17;
            foreach (Proportie p in owner.properties)
                if (p is Mesh m) sig = sig * 31 + m.oldpoints.Count + 1;
            return sig;
        }

        /// <summary>
        /// Builds the collision shape from the collider mesh (if set) or the item's visible
        /// meshes, using the same rotation math as Item.Update / Mesh.Update so it matches
        /// what is drawn.
        /// </summary>
        private void BuildShape()
        {
            lastXr = owner.xrot; lastYr = owner.yrot; lastZr = owner.zrot;
            shapeDirty = false;

            double[,] itemRot = BuildRotation(owner.xrot, owner.yrot, owner.zrot);
            IEnumerable<Mesh> meshes = _colliderMesh != null
                ? new[] { _colliderMesh }
                : owner.properties.OfType<Mesh>();

            var tA = new List<Vec3>(); var tB = new List<Vec3>(); var tC = new List<Vec3>();
            var unique = new Dictionary<(long, long, long), Vec3>();

            foreach (Mesh mesh in meshes)
            {
                double[] off = Rotation.ApplyRotation(itemRot, mesh.x, mesh.y, mesh.z);
                double[,] meshRot = BuildRotation(owner.xrot + mesh.xrot, owner.yrot + mesh.yrot, owner.zrot + mesh.zrot);
                Vec3 offset = new Vec3(off[0], off[1], off[2]);

                foreach (Polygon poly in mesh.oldpoints)
                {
                    Vec3 a = Transform(meshRot, poly.p1) + offset;
                    Vec3 b = Transform(meshRot, poly.p2) + offset;
                    Vec3 c = Transform(meshRot, poly.p3) + offset;
                    tA.Add(a); tB.Add(b); tC.Add(c);
                    foreach (Vec3 v in new[] { a, b, c })
                        unique[(Key(v.X), Key(v.Y), Key(v.Z))] = v;
                }
            }

            var verts = unique.Values.ToArray();
            Vec3 min = Vec3.Zero, max = Vec3.Zero;
            if (verts.Length > 0)
            {
                min = max = verts[0];
                foreach (var v in verts) { min = Vec3.Min(min, v); max = Vec3.Max(max, v); }
            }

            shape = new CollisionShape
            {
                verts = verts,
                triA = tA.ToArray(),
                triB = tB.ToArray(),
                triC = tC.ToArray(),
                min = min,
                max = max,
                version = (shape?.version ?? 0) + 1,
            };
        }

        private static long Key(double v) => (long)Math.Round(v * 1000);

        private static Vec3 Transform(double[,] rot, Point p)
        {
            double[] r = Rotation.ApplyRotation(rot, p.x, p.y, p.z);
            return new Vec3(r[0], r[1], r[2]);
        }

        private static double[,] BuildRotation(double xDeg, double yDeg, double zDeg)
        {
            double toRad = Math.PI / 180.0;
            double[,] rx = Rotation.GetRotationMatrixX(Rotation.WrapAngle(xDeg) * toRad);
            double[,] ry = Rotation.GetRotationMatrixY(Rotation.WrapAngle(yDeg) * toRad);
            double[,] rz = Rotation.GetRotationMatrixZ(Rotation.WrapAngle(zDeg) * toRad);
            return Rotation.CombineMatrices(rz, Rotation.CombineMatrices(ry, rx));
        }

        private static Mesh MakeBoxMesh(Vec3 size, Vec3 offset)
        {
            double x = size.X / 2, y = size.Y / 2, z = size.Z / 2;
            Point P(double px, double py, double pz) => new Point(px, py, pz, 1, 0, 0, 0);
            Point[] c =
            {
                P(-x, -y, -z), P(x, -y, -z), P(x, y, -z), P(-x, y, -z),
                P(-x, -y,  z), P(x, -y,  z), P(x, y,  z), P(-x, y,  z),
            };
            int[] idx =
            {
                0,1,2, 0,2,3,   4,6,5, 4,7,6,   0,4,5, 0,5,1,
                3,2,6, 3,6,7,   0,3,7, 0,7,4,   1,5,6, 1,6,2,
            };
            var polys = new List<Polygon>();
            for (int i = 0; i < idx.Length; i += 3)
                polys.Add(new Polygon(c[idx[i]], c[idx[i + 1]], c[idx[i + 2]]));
            return new Mesh(polys, offset.X, offset.Y, offset.Z, 0, 0, 0);
        }

        // The world drives the simulation; nothing to do per item update.
        public override void Update() { }
    }

    /// <summary>
    /// Owns all physics bodies and steps them. ThreeDSceen calls Step(deltaTime) once per frame.
    /// The frame's delta time is split into fixed-size steps, so a slow machine runs more
    /// steps per frame and a fast one fewer: objects fall and bounce the same either way.
    /// </summary>
    public class PhysicsWorld
    {
        public Vec3 gravity = new Vec3(0, 2000, 0);   // +Y is down in this engine (see note at top)

        /// <summary>Simulation rate. 1/120 s is smooth and stable for small scenes.</summary>
        public double fixedTimeStep = 1.0 / 120.0;
        /// <summary>Caps catch-up work after a long frame so the game can't spiral.</summary>
        public int maxSubSteps = 8;
        /// <summary>Collision passes per step. More = steadier stacks, slower.</summary>
        public int solverIterations = 4;
        public bool paused = false;

        private readonly List<PhysicsBody> bodies = new List<PhysicsBody>();
        private readonly List<PhysicsBody> dynamicBodies = new List<PhysicsBody>();
        private readonly List<PhysicsBody> staticBodies = new List<PhysicsBody>();
        private readonly HashSet<(PhysicsBody, PhysicsBody)> touching = new HashSet<(PhysicsBody, PhysicsBody)>();
        private readonly HullSupport hullA = new HullSupport();
        private readonly HullSupport hullB = new HullSupport();
        private readonly TriSupport tri = new TriSupport();
        private double accumulator = 0;
        private int contactsResolved = 0;
        private readonly object sync = new object();

        public IReadOnlyList<PhysicsBody> Bodies => bodies;

        public void Add(PhysicsBody body) { lock (sync) { if (!bodies.Contains(body)) bodies.Add(body); } }
        public void Remove(PhysicsBody body) { lock (sync) { bodies.Remove(body); } }

        /// <summary>Advance by deltaSeconds (the scene's delta time) using fixed sub-steps.</summary>
        public void Step(double deltaSeconds)
        {
            if (paused || deltaSeconds <= 0) return;

            accumulator += deltaSeconds;
            double maxAccum = fixedTimeStep * maxSubSteps;
            if (accumulator > maxAccum) accumulator = maxAccum;

            while (accumulator >= fixedTimeStep)
            {
                Simulate(fixedTimeStep);
                accumulator -= fixedTimeStep;
            }
        }

        /// <summary>Advance exactly dt seconds (useful for tests or manual stepping).</summary>
        public void Simulate(double dt)
        {
            lock (sync)
            {
                dynamicBodies.Clear();
                staticBodies.Clear();
                foreach (var b in bodies)
                {
                    if (!b.owner.state) continue;      // disabled items don't take part
                    if (b.Shape.IsEmpty) { if (!b.isStatic) dynamicBodies.Add(b); continue; }
                    (b.isStatic ? staticBodies : dynamicBodies).Add(b);
                }

                // 1. Integrate: forces -> velocity -> position
                foreach (var b in dynamicBodies)
                {
                    b.prevPosition = b.Position;
                    Vec3 accel = b.ConsumeForce() * b.InverseMass;
                    if (b.useGravity) accel = accel + gravity;

                    b.velocity = b.velocity + accel * dt;
                    b.velocity = b.velocity * Math.Max(0.0, 1.0 - b.drag * dt);
                    b.Position = b.Position + b.velocity * dt;
                    b.isGrounded = false;
                }

                // 2. Collisions, a few passes so stacks and corners settle
                touching.Clear();
                for (int pass = 0; pass < solverIterations; pass++)
                {
                    int before = contactsResolved;
                    for (int i = 0; i < dynamicBodies.Count; i++)
                    {
                        var a = dynamicBodies[i];
                        if (a.Shape.IsEmpty) continue;

                        foreach (var s in staticBodies)
                            CollideWithStatic(a, s);

                        for (int j = i + 1; j < dynamicBodies.Count; j++)
                        {
                            var b = dynamicBodies[j];
                            if (!b.Shape.IsEmpty) CollideHulls(a, b);
                        }
                    }
                    if (contactsResolved == before) break;   // everything separated: no more passes needed
                }

                foreach (var (a, b) in touching)
                {
                    a.RaiseCollision(b);
                    b.RaiseCollision(a);
                }
            }
        }

        private static bool Overlaps(Vec3 aMin, Vec3 aMax, Vec3 bMin, Vec3 bMax) =>
            aMin.X <= bMax.X && aMax.X >= bMin.X &&
            aMin.Y <= bMax.Y && aMax.Y >= bMin.Y &&
            aMin.Z <= bMax.Z && aMax.Z >= bMin.Z;

        /// <summary>Moving body vs every triangle of a static mesh.</summary>
        private void CollideWithStatic(PhysicsBody a, PhysicsBody s)
        {
            s.GetBounds(out Vec3 sMin, out Vec3 sMax);
            a.GetBounds(out Vec3 aMin, out Vec3 aMax);
            if (!Overlaps(aMin, aMax, sMin, sMax)) return;

            var shape = a.Shape;
            WorldTri[] tris = s.WorldTriangles;
            for (int t = 0; t < tris.Length; t++)
            {
                a.GetBounds(out aMin, out aMax);   // a may have been pushed by the previous triangle
                if (!Overlaps(aMin, aMax, tris[t].min, tris[t].max)) continue;

                hullA.Set(shape.verts, a.Position);
                tri.Set(tris[t]);
                Vec3 center = shape.Center + a.Position;
                Vec3 triCenter = (tris[t].a + tris[t].b + tris[t].c) / 3;
                if (!Gjk.Intersect(hullA, tri, triCenter - center, out Vec3[] simplex)) continue;

                // Treat the triangle as a surface facing the side the body came from.
                // This stops bodies catching on the seams between flat triangles
                // and stops fast bodies being pushed through to the far side.
                Vec3 fn = tris[t].normal;
                double planeD = Vec3.Dot(fn, tris[t].a);
                Vec3 prevCenter = shape.Center + a.prevPosition;
                if (Vec3.Dot(fn, prevCenter) < planeD) { fn = -fn; planeD = -planeD; }

                double curLow = LowestAlong(shape, a.Position, fn);
                double prevLow = LowestAlong(shape, a.prevPosition, fn);
                double slop = 0.01 * Math.Max(1.0, (shape.max - shape.min).Length * 0.01);

                Vec3 normal; double depth;
                if (prevLow >= planeD - slop)
                {
                    // Came from the front: push straight back out of the surface (or just touching)
                    if (curLow >= planeD) continue;
                    normal = -fn;                       // points from a into the surface
                    depth = planeD - curLow;
                }
                else if (!Gjk.Penetration(hullA, tri, simplex, out normal, out depth))
                {
                    continue;   // already beside/below this triangle: use the smallest way out
                }
                Respond(a, s, normal, depth);
            }
        }

        /// <summary>Moving body vs moving body: convex hull vs convex hull.</summary>
        private void CollideHulls(PhysicsBody a, PhysicsBody b)
        {
            a.GetBounds(out Vec3 aMin, out Vec3 aMax);
            b.GetBounds(out Vec3 bMin, out Vec3 bMax);
            if (!Overlaps(aMin, aMax, bMin, bMax)) return;

            hullA.Set(a.Shape.verts, a.Position);
            hullB.Set(b.Shape.verts, b.Position);
            Vec3 dir = (b.Shape.Center + b.Position) - (a.Shape.Center + a.Position);
            if (!Gjk.Intersect(hullA, hullB, dir, out Vec3[] simplex)) return;
            if (!Gjk.Penetration(hullA, hullB, simplex, out Vec3 normal, out double depth)) return;
            Respond(a, b, normal, depth);
        }

        private static double LowestAlong(CollisionShape shape, Vec3 pos, Vec3 n)
        {
            double best = double.MaxValue;
            foreach (var v in shape.verts) best = Math.Min(best, Vec3.Dot(n, v));
            return best + Vec3.Dot(n, pos);
        }

        /// <summary>Push apart, bounce and apply friction. normal points from a towards b.</summary>
        private void Respond(PhysicsBody a, PhysicsBody b, Vec3 normal, double depth)
        {
            double invA = a.InverseMass, invB = b.InverseMass;
            double invSum = invA + invB;
            if (invSum <= 0 || depth <= 0) return;

            // Positional correction, split by mass
            a.Position = a.Position - normal * (depth * invA / invSum);
            if (invB > 0) b.Position = b.Position + normal * (depth * invB / invSum);

            // Velocity response
            Vec3 relVel = b.velocity - a.velocity;
            double velAlongNormal = Vec3.Dot(relVel, normal);
            if (velAlongNormal < 0)
            {
                double e = Math.Min(a.bounciness, b.bounciness);
                double j = -(1 + e) * velAlongNormal / invSum;
                Vec3 impulse = normal * j;
                a.velocity = a.velocity - impulse * invA;
                b.velocity = b.velocity + impulse * invB;

                // Friction (Coulomb): oppose sliding, limited by how hard they press together
                relVel = b.velocity - a.velocity;
                Vec3 tangent = relVel - normal * Vec3.Dot(relVel, normal);
                double tLen = tangent.Length;
                if (tLen > 1e-9)
                {
                    tangent = tangent / tLen;
                    double mu = Math.Sqrt(a.friction * b.friction);
                    double stopAll = tLen / invSum;
                    double jt = Math.Min(stopAll, mu * j);
                    Vec3 fImpulse = tangent * jt;
                    a.velocity = a.velocity + fImpulse * invA;
                    b.velocity = b.velocity - fImpulse * invB;

                    // Friction held completely against something fixed: also undo the little
                    // slide this step already made, or objects slowly creep down slopes.
                    if (invB == 0 && jt >= stopAll)
                    {
                        Vec3 moved = a.Position - a.prevPosition;
                        a.Position = a.Position - (moved - normal * Vec3.Dot(moved, normal));
                    }
                }
            }

            // Grounded = something under us relative to gravity (slopes up to ~60 degrees)
            Vec3 down = gravity.Normalized;
            if (Vec3.Dot(normal, down) > 0.5) a.isGrounded = true;   // b is below a
            if (Vec3.Dot(-normal, down) > 0.5) b.isGrounded = true;  // a is below b

            touching.Add((a, b));
            contactsResolved++;
        }
    }

    // ------------------------------------------------------------------
    //  Shape tests: GJK (do two convex shapes overlap?) and EPA (by how
    //  much, and in which direction?). Works on any convex shape that can
    //  answer "which of your points is furthest in this direction?".
    // ------------------------------------------------------------------

    internal interface ISupport
    {
        Vec3 Support(Vec3 dir);
    }

    internal class HullSupport : ISupport
    {
        private Vec3[] verts;
        private Vec3 offset;
        public void Set(Vec3[] verts, Vec3 offset) { this.verts = verts; this.offset = offset; }
        public Vec3 Support(Vec3 d)
        {
            Vec3 best = verts[0];
            double bestDot = Vec3.Dot(best, d);
            for (int i = 1; i < verts.Length; i++)
            {
                double dd = Vec3.Dot(verts[i], d);
                if (dd > bestDot) { bestDot = dd; best = verts[i]; }
            }
            return best + offset;
        }
    }

    internal class TriSupport : ISupport
    {
        private Vec3 a, b, c;
        public void Set(WorldTri t) { a = t.a; b = t.b; c = t.c; }
        public Vec3 Support(Vec3 d)
        {
            double da = Vec3.Dot(a, d), db = Vec3.Dot(b, d), dc = Vec3.Dot(c, d);
            return da >= db ? (da >= dc ? a : c) : (db >= dc ? b : c);
        }
    }

    internal static class Gjk
    {
        private const int MaxIterations = 64;
        private const double Eps = 1e-12;

        // Point on the Minkowski difference A - B furthest along d
        private static Vec3 S(ISupport A, ISupport B, Vec3 d) => A.Support(d) - B.Support(-d);

        /// <summary>True if A and B overlap; simplex is then a tetrahedron around the origin for EPA.</summary>
        public static bool Intersect(ISupport A, ISupport B, Vec3 dir, out Vec3[] simplex)
        {
            simplex = null;
            if (dir.LengthSquared < Eps) dir = new Vec3(1, 0, 0);

            Vec3 a = Vec3.Zero, d = Vec3.Zero;
            Vec3 c = S(A, B, dir);
            Vec3 search = -c;
            if (search.LengthSquared < Eps) return false;      // only just touching

            Vec3 b = S(A, B, search);
            if (Vec3.Dot(b, search) < 0) return false;

            search = Vec3.Cross(Vec3.Cross(c - b, -b), c - b);
            if (search.LengthSquared < Eps)
            {
                search = Vec3.Cross(c - b, new Vec3(1, 0, 0));
                if (search.LengthSquared < Eps) search = Vec3.Cross(c - b, new Vec3(0, 0, -1));
            }

            int dim = 2;
            for (int i = 0; i < MaxIterations; i++)
            {
                a = S(A, B, search);
                if (Vec3.Dot(a, search) < 0) return false;
                dim++;
                if (dim == 3)
                {
                    UpdateTriangle(ref a, ref b, ref c, ref d, ref dim, ref search);
                }
                else if (UpdateTetrahedron(ref a, ref b, ref c, ref d, ref dim, ref search))
                {
                    simplex = new[] { a, b, c, d };
                    return true;
                }
            }
            return false;
        }

        private static void UpdateTriangle(ref Vec3 a, ref Vec3 b, ref Vec3 c, ref Vec3 d, ref int dim, ref Vec3 search)
        {
            Vec3 n = Vec3.Cross(b - a, c - a);
            Vec3 ao = -a;
            dim = 2;
            if (Vec3.Dot(Vec3.Cross(b - a, n), ao) > 0) { c = a; search = Vec3.Cross(Vec3.Cross(b - a, ao), b - a); return; }
            if (Vec3.Dot(Vec3.Cross(n, c - a), ao) > 0) { b = a; search = Vec3.Cross(Vec3.Cross(c - a, ao), c - a); return; }
            dim = 3;
            if (Vec3.Dot(n, ao) > 0) { d = c; c = b; b = a; search = n; return; }
            d = b; b = a; search = -n;
        }

        private static bool UpdateTetrahedron(ref Vec3 a, ref Vec3 b, ref Vec3 c, ref Vec3 d, ref int dim, ref Vec3 search)
        {
            Vec3 abc = Vec3.Cross(b - a, c - a);
            Vec3 acd = Vec3.Cross(c - a, d - a);
            Vec3 adb = Vec3.Cross(d - a, b - a);
            Vec3 ao = -a;
            dim = 3;
            if (Vec3.Dot(abc, ao) > 0) { d = c; c = b; b = a; search = abc; return false; }
            if (Vec3.Dot(acd, ao) > 0) { b = a; search = acd; return false; }
            if (Vec3.Dot(adb, ao) > 0) { c = d; d = b; b = a; search = adb; return false; }
            return true;
        }

        private struct Face { public Vec3 a, b, c, n; public double dist; }

        /// <summary>
        /// Expanding Polytope Algorithm: from an overlapping GJK simplex, find the smallest push
        /// that separates A and B. normal points from A towards B; move A by -normal * depth.
        /// </summary>
        public static bool Penetration(ISupport A, ISupport B, Vec3[] simplex, out Vec3 normal, out double depth)
        {
            normal = Vec3.Zero; depth = 0;
            Vec3 a = simplex[0], b = simplex[1], c = simplex[2], d = simplex[3];

            // A point strictly inside the polytope (it only grows, so this stays inside).
            // Faces are oriented away from it, which is reliable even when the origin
            // sits right on a face (shapes just touching).
            Vec3 inside = (a + b + c + d) / 4;
            var faces = new List<Face>(32);
            if (!AddFace(faces, inside, a, b, c) | !AddFace(faces, inside, a, c, d) |
                !AddFace(faces, inside, a, d, b) | !AddFace(faces, inside, b, d, c))
                return false;   // flat simplex: shapes only graze each other

            var edges = new List<(Vec3, Vec3)>();
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                int closest = 0;
                for (int i = 1; i < faces.Count; i++)
                    if (faces[i].dist < faces[closest].dist) closest = i;

                Face f = faces[closest];
                Vec3 p = S(A, B, f.n);
                double tol = 1e-6 * Math.Max(1.0, f.dist) + 1e-4;
                if (Vec3.Dot(p, f.n) - f.dist < tol || iter == MaxIterations - 1)
                {
                    normal = f.n; depth = Math.Max(0, f.dist);
                    return depth > 0;
                }

                // Remove every face that can see p, keeping the boundary edges of the hole
                edges.Clear();
                for (int i = faces.Count - 1; i >= 0; i--)
                {
                    Face g = faces[i];
                    if (Vec3.Dot(g.n, p - g.a) > 0)
                    {
                        AddEdge(edges, g.a, g.b);
                        AddEdge(edges, g.b, g.c);
                        AddEdge(edges, g.c, g.a);
                        faces.RemoveAt(i);
                    }
                }

                // Patch the hole with new faces joined to p
                foreach (var (e0, e1) in edges) AddFace(faces, inside, e0, e1, p);
                if (faces.Count == 0) return false;
            }
            return false;
        }

        private static bool AddFace(List<Face> faces, Vec3 inside, Vec3 a, Vec3 b, Vec3 c)
        {
            Vec3 n = Vec3.Cross(b - a, c - a);
            double len = n.Length;
            if (len < 1e-12) return false;
            n = n / len;
            if (Vec3.Dot(n, a - inside) < 0) { var t = b; b = c; c = t; n = -n; }  // keep normals facing outward
            double dist = Vec3.Dot(n, a);
            faces.Add(new Face { a = a, b = b, c = c, n = n, dist = dist });
            return true;
        }

        private static void AddEdge(List<(Vec3, Vec3)> edges, Vec3 a, Vec3 b)
        {
            // An edge shared by two removed faces is inside the hole: drop both copies
            for (int i = 0; i < edges.Count; i++)
            {
                var (x, y) = edges[i];
                if (x.Equals(b) && y.Equals(a)) { edges.RemoveAt(i); return; }
            }
            edges.Add((a, b));
        }
    }

    /// <summary>
    /// Demo: press F to throw a box from the camera into the scene.
    /// Attach to the camera Item. Keeps at most maxBoxes alive.
    /// </summary>
    public class BoxThrower : Proportie
    {
        public Item camera;
        public Window window;
        public ThreeDSceen scene;
        public double throwSpeed = 1500;
        public int maxBoxes = 25;
        public int throwKey = 70; // F

        private bool wasDown = false;
        private readonly Queue<Item> thrown = new Queue<Item>();
        private readonly Random rnd = new Random();

        public BoxThrower(Item camera, Window window, ThreeDSceen scene) : base(0, 0, 0, 0, 0, 0)
        {
            this.camera = camera;
            this.window = window;
            this.scene = scene;
        }

        public override void Update()
        {
            bool down = window.pressed.Contains(throwKey);
            if (down && !wasDown) Throw();
            wasDown = down;
        }

        private void Throw()
        {
            // Same forward direction the Movement script uses for W
            double yaw = camera.yrot * Math.PI / 180.0;
            Vec3 forward = new Vec3(Math.Sin(yaw), 0, Math.Cos(yaw));
            Vec3 up = -scene.physics.gravity.Normalized;

            Vec3 spawn = new Vec3(camera.x, camera.y, camera.z) + forward * 150;
            Item box = new Item(spawn.X, spawn.Y, spawn.Z, 90, 0, 0);
            Mesh mesh = new Mesh("Models\\FullBox.json", 0, 0, 0, 0, 0, 0);
            mesh.scale(5, 5, 5);
            mesh.hueit(rnd.Next(0, 120), rnd.Next(0, 120), rnd.Next(0, 120));
            box.add_propertie(mesh);

            var body = new PhysicsBody(box, scene.physics, mass: 1) { bounciness = 0.35 };
            body.velocity = forward * throwSpeed + up * (throwSpeed * 0.3);

            scene.queue_add(box);
            thrown.Enqueue(box);
            if (thrown.Count > maxBoxes)
            {
                Item old = thrown.Dequeue();
                foreach (var p in old.properties)
                    if (p is PhysicsBody pb) scene.physics.Remove(pb);
                scene.queue_remove(old);
            }
        }
    }
}