using System;
using System.Collections.Generic;

namespace GEJE
{

    /// <summary>3D vector used by the physics code.</summary>
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
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vec3 Normalized { get { double l = Length; return l > 1e-12 ? this / l : Zero; } }

        public override string ToString() => $"({X:0.##}, {Y:0.##}, {Z:0.##})";
    }

    /// <summary>
    /// Gives an Item physical behaviour. Creating one attaches it to the item
    /// and registers it with the world, so a single line is enough.
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

        /// <summary>
        /// Collider size. If autoSize is true these are computed from the item's
        /// meshes (and recomputed when the item rotates). Set autoSize = false to
        /// use your own halfSize/centerOffset.
        /// </summary>
        public bool autoSize = true;
        public Vec3 halfSize = new Vec3(10, 10, 10);
        public Vec3 centerOffset = Vec3.Zero;

        /// <summary>Called with the other body whenever this body touches something.</summary>
        public event Action<PhysicsBody, PhysicsBody> OnCollision;

        private Vec3 accumulatedForce = Vec3.Zero;
        private bool boundsReady = false;
        private double lastXr, lastYr, lastZr;

        public PhysicsBody(Item owner, PhysicsWorld world, double mass = 1.0) : base(0, 0, 0, 0, 0, 0)
        {
            this.owner = owner;
            this.world = world;
            this.mass = mass;
            owner.add_propertie(this);
            world.Add(this);
        }

        public double InverseMass => (isStatic || mass <= 0) ? 0.0 : 1.0 / mass;

        public Vec3 Position
        {
            get => new Vec3(owner.x, owner.y, owner.z);
            set => owner.move(value.X, value.Y, value.Z, owner.w);
        }

        /// <summary>Continuous push (e.g. thrusters, wind). Units: mass * distance / s^2.</summary>
        public void ApplyForce(Vec3 force) { accumulatedForce = accumulatedForce + force; }

        /// <summary>Instant kick (e.g. a jump or a hit). Changes velocity by impulse / mass.</summary>
        public void ApplyImpulse(Vec3 impulse)
        {
            if (isStatic) return;
            velocity = velocity + impulse * InverseMass;
        }

        internal Vec3 ConsumeForce() { Vec3 f = accumulatedForce; accumulatedForce = Vec3.Zero; return f; }
        internal void RaiseCollision(PhysicsBody other) => OnCollision?.Invoke(this, other);

        public void GetBounds(out Vec3 min, out Vec3 max)
        {
            EnsureBounds();
            Vec3 c = Position + centerOffset;
            min = c - halfSize;
            max = c + halfSize;
        }

        /// <summary>Recompute the collider from the meshes if the item rotated.</summary>
        internal void EnsureBounds()
        {
            if (!autoSize) return;
            if (boundsReady && owner.xrot == lastXr && owner.yrot == lastYr && owner.zrot == lastZr) return;
            RecalculateBounds();
        }

        /// <summary>
        /// Builds the AABB from the item's meshes using the same rotation math
        /// as Item.Update / Mesh.Update, so it matches what is drawn.
        /// </summary>
        public void RecalculateBounds()
        {
            lastXr = owner.xrot; lastYr = owner.yrot; lastZr = owner.zrot;
            boundsReady = true;

            double[,] itemRot = BuildRotation(owner.xrot, owner.yrot, owner.zrot);
            bool any = false;
            double minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;

            foreach (Proportie p in owner.properties)
            {
                if (!(p is Mesh mesh)) continue;

                double[] off = Rotation.ApplyRotation(itemRot, mesh.x, mesh.y, mesh.z);
                double[,] meshRot = BuildRotation(owner.xrot + mesh.xrot, owner.yrot + mesh.yrot, owner.zrot + mesh.zrot);

                foreach (Polygon poly in mesh.oldpoints)
                {
                    foreach (Point pt in new[] { poly.p1, poly.p2, poly.p3 })
                    {
                        double[] r = Rotation.ApplyRotation(meshRot, pt.x, pt.y, pt.z);
                        double px = r[0] + off[0], py = r[1] + off[1], pz = r[2] + off[2];
                        if (!any)
                        {
                            minX = maxX = px; minY = maxY = py; minZ = maxZ = pz;
                            any = true;
                        }
                        else
                        {
                            minX = Math.Min(minX, px); maxX = Math.Max(maxX, px);
                            minY = Math.Min(minY, py); maxY = Math.Max(maxY, py);
                            minZ = Math.Min(minZ, pz); maxZ = Math.Max(maxZ, pz);
                        }
                    }
                }
            }

            if (!any) return; // no mesh yet: keep the default box

            halfSize = new Vec3((maxX - minX) / 2, (maxY - minY) / 2, (maxZ - minZ) / 2);
            centerOffset = new Vec3((maxX + minX) / 2, (maxY + minY) / 2, (maxZ + minZ) / 2);
        }

        private static double[,] BuildRotation(double xDeg, double yDeg, double zDeg)
        {
            double toRad = Math.PI / 180.0;
            double[,] rx = Rotation.GetRotationMatrixX(Rotation.WrapAngle(xDeg) * toRad);
            double[,] ry = Rotation.GetRotationMatrixY(Rotation.WrapAngle(yDeg) * toRad);
            double[,] rz = Rotation.GetRotationMatrixZ(Rotation.WrapAngle(zDeg) * toRad);
            return Rotation.CombineMatrices(rz, Rotation.CombineMatrices(ry, rx));
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
        public bool paused = false;

        private readonly List<PhysicsBody> bodies = new List<PhysicsBody>();
        private readonly List<PhysicsBody> dynamicBodies = new List<PhysicsBody>();
        private readonly List<PhysicsBody> staticBodies = new List<PhysicsBody>();
        private readonly List<Vec3> staticMins = new List<Vec3>();
        private readonly List<Vec3> staticMaxs = new List<Vec3>();
        private double accumulator = 0;
        private readonly object sync = new object();

        public IReadOnlyList<PhysicsBody> Bodies => bodies;

        public void Add(PhysicsBody body) { lock (sync) { if (!bodies.Contains(body)) bodies.Add(body); } }
        public void Remove(PhysicsBody body) { lock (sync) { bodies.Remove(body); } }

        /// <summary>Advance by deltaSeconds (the scene's delta time) using fixed sub-steps.</summary>
        public void Step(double deltaSeconds)
        {
            if (paused || deltaSeconds <= 0) return;
            double elapsed = deltaSeconds;

            accumulator += elapsed;
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
                    (b.isStatic ? staticBodies : dynamicBodies).Add(b);
                }

                // 1. Integrate: forces -> velocity -> position
                foreach (var b in dynamicBodies)
                {
                    Vec3 accel = b.ConsumeForce() * b.InverseMass;
                    if (b.useGravity) accel = accel + gravity;

                    b.velocity = b.velocity + accel * dt;
                    b.velocity = b.velocity * Math.Max(0.0, 1.0 - b.drag * dt);
                    b.Position = b.Position + b.velocity * dt;
                    b.isGrounded = false;
                }

                // Static bodies don't move during the step, so get their boxes once
                staticMins.Clear(); staticMaxs.Clear();
                foreach (var s in staticBodies)
                {
                    s.GetBounds(out Vec3 mn, out Vec3 mx);
                    staticMins.Add(mn); staticMaxs.Add(mx);
                }

                // 2. Collisions: dynamic vs static, then dynamic vs dynamic
                for (int i = 0; i < dynamicBodies.Count; i++)
                {
                    var a = dynamicBodies[i];
                    for (int k = 0; k < staticBodies.Count; k++)
                    {
                        a.GetBounds(out Vec3 aMin, out Vec3 aMax);
                        Resolve(a, aMin, aMax, staticBodies[k], staticMins[k], staticMaxs[k]);
                    }
                    for (int j = i + 1; j < dynamicBodies.Count; j++)
                    {
                        var b = dynamicBodies[j];
                        a.GetBounds(out Vec3 aMin, out Vec3 aMax);
                        b.GetBounds(out Vec3 bMin, out Vec3 bMax);
                        Resolve(a, aMin, aMax, b, bMin, bMax);
                    }
                }
            }
        }

        private void Resolve(PhysicsBody a, Vec3 aMin, Vec3 aMax, PhysicsBody b, Vec3 bMin, Vec3 bMax)
        {
            // How far the boxes overlap on each axis (<= 0 means no contact)
            double ox = Math.Min(aMax.X, bMax.X) - Math.Max(aMin.X, bMin.X);
            if (ox <= 0) return;
            double oy = Math.Min(aMax.Y, bMax.Y) - Math.Max(aMin.Y, bMin.Y);
            if (oy <= 0) return;
            double oz = Math.Min(aMax.Z, bMax.Z) - Math.Max(aMin.Z, bMin.Z);
            if (oz <= 0) return;

            // Push out along the axis of least overlap. normal points from a to b.
            double aC, bC; Vec3 normal; double depth;
            if (ox <= oy && ox <= oz)
            {
                aC = aMin.X + aMax.X; bC = bMin.X + bMax.X;
                normal = new Vec3(bC > aC ? 1 : -1, 0, 0); depth = ox;
            }
            else if (oy <= oz)
            {
                aC = aMin.Y + aMax.Y; bC = bMin.Y + bMax.Y;
                normal = new Vec3(0, bC > aC ? 1 : -1, 0); depth = oy;
            }
            else
            {
                aC = aMin.Z + aMax.Z; bC = bMin.Z + bMax.Z;
                normal = new Vec3(0, 0, bC > aC ? 1 : -1); depth = oz;
            }

            double invA = a.InverseMass, invB = b.InverseMass;
            double invSum = invA + invB;
            if (invSum <= 0) return;

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
                    double jt = Math.Min(tLen / invSum, mu * j);
                    Vec3 fImpulse = tangent * jt;
                    a.velocity = a.velocity + fImpulse * invA;
                    b.velocity = b.velocity - fImpulse * invB;
                }
            }

            // Grounded = something under us relative to gravity
            Vec3 down = gravity.Normalized;
            if (Vec3.Dot(normal, down) > 0.7) a.isGrounded = true;   // b is below a
            if (Vec3.Dot(-normal, down) > 0.7) b.isGrounded = true;  // a is below b

            a.RaiseCollision(b);
            b.RaiseCollision(a);
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
