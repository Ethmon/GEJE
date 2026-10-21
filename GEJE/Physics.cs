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
    /// 3x3 rotation matrix used for body orientation. Built and read back with the same
    /// Rz * Ry * Rx order as Rotation.cs, so physics and rendering always agree.
    /// </summary>
    internal struct Mat3
    {
        public double m00, m01, m02, m10, m11, m12, m20, m21, m22;

        public static readonly Mat3 Identity = new Mat3 { m00 = 1, m11 = 1, m22 = 1 };

        public static Mat3 operator *(Mat3 a, Mat3 b) => new Mat3
        {
            m00 = a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20,
            m01 = a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21,
            m02 = a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22,
            m10 = a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20,
            m11 = a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21,
            m12 = a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22,
            m20 = a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20,
            m21 = a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21,
            m22 = a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22,
        };

        public static Vec3 operator *(Mat3 m, Vec3 v) => new Vec3(
            m.m00 * v.X + m.m01 * v.Y + m.m02 * v.Z,
            m.m10 * v.X + m.m11 * v.Y + m.m12 * v.Z,
            m.m20 * v.X + m.m21 * v.Y + m.m22 * v.Z);

        public Mat3 Transposed => new Mat3
        {
            m00 = m00,
            m01 = m10,
            m02 = m20,
            m10 = m01,
            m11 = m11,
            m12 = m21,
            m20 = m02,
            m21 = m12,
            m22 = m22,
        };

        /// <summary>Same matrix Item/Mesh/BuildShape make from xrot, yrot, zrot (degrees).</summary>
        public static Mat3 FromEulerDegrees(double xDeg, double yDeg, double zDeg)
        {
            double toRad = Math.PI / 180.0;
            double cx = Math.Cos(xDeg * toRad), sx = Math.Sin(xDeg * toRad);
            double cy = Math.Cos(yDeg * toRad), sy = Math.Sin(yDeg * toRad);
            double cz = Math.Cos(zDeg * toRad), sz = Math.Sin(zDeg * toRad);
            var rx = new Mat3 { m00 = 1, m11 = cx, m12 = -sx, m21 = sx, m22 = cx };
            var ry = new Mat3 { m00 = cy, m02 = sy, m11 = 1, m20 = -sy, m22 = cy };
            var rz = new Mat3 { m00 = cz, m01 = -sz, m10 = sz, m11 = cz, m22 = 1 };
            return rz * (ry * rx);
        }

        /// <summary>Back to xrot, yrot, zrot in degrees (inverse of FromEulerDegrees).</summary>
        public void ToEulerDegrees(out double xDeg, out double yDeg, out double zDeg)
        {
            double toDeg = 180.0 / Math.PI;
            double sy = Math.Max(-1.0, Math.Min(1.0, -m20));
            yDeg = Math.Asin(sy) * toDeg;
            if (Math.Abs(sy) < 0.999999)
            {
                xDeg = Math.Atan2(m21, m22) * toDeg;
                zDeg = Math.Atan2(m10, m00) * toDeg;
            }
            else
            {
                // Gimbal lock: x and z spin about the same axis, so put it all in z
                xDeg = 0;
                zDeg = Math.Atan2(-m01, m11) * toDeg;
            }
        }

        /// <summary>Rotation by angle (radians) around a unit axis.</summary>
        public static Mat3 AxisAngle(Vec3 k, double angle)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle), t = 1 - c;
            return new Mat3
            {
                m00 = c + k.X * k.X * t,
                m01 = k.X * k.Y * t - k.Z * s,
                m02 = k.X * k.Z * t + k.Y * s,
                m10 = k.Y * k.X * t + k.Z * s,
                m11 = c + k.Y * k.Y * t,
                m12 = k.Y * k.Z * t - k.X * s,
                m20 = k.Z * k.X * t - k.Y * s,
                m21 = k.Z * k.Y * t + k.X * s,
                m22 = c + k.Z * k.Z * t,
            };
        }

        /// <summary>Removes the tiny drift that builds up from multiplying rotations every step.</summary>
        public Mat3 Orthonormalized()
        {
            Vec3 c0 = new Vec3(m00, m10, m20).Normalized;
            Vec3 c1 = new Vec3(m01, m11, m21);
            c1 = (c1 - c0 * Vec3.Dot(c0, c1)).Normalized;
            Vec3 c2 = Vec3.Cross(c0, c1);
            return new Mat3
            {
                m00 = c0.X,
                m01 = c1.X,
                m02 = c2.X,
                m10 = c0.Y,
                m11 = c1.Y,
                m12 = c2.Y,
                m20 = c0.Z,
                m21 = c1.Z,
                m22 = c2.Z,
            };
        }
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
        public double friction = 0.75;
        /// <summary>Air resistance; fraction of velocity lost per second.</summary>
        public double drag = 0.05;

        /// <summary>
        /// Spin in radians per second, around world axes. The direction is the spin axis
        /// and the length is how fast it turns. Written back to the item's xrot/yrot/zrot.
        /// </summary>
        public Vec3 angularVelocity = Vec3.Zero;
        /// <summary>Fraction of spin lost per second (keeps things from spinning forever).</summary>
        public double angularDrag = 0.5;
        /// <summary>
        /// True = never rotate from collisions or torque (players, characters, anything a
        /// script turns itself). The body then behaves exactly like before rotation existed.
        /// </summary>
        public bool freezeRotation = false;

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
        private Vec3 accumulatedTorque = Vec3.Zero;
        internal Vec3 prevPosition;

        // Orientation (kept as a matrix so it never drifts, then written back as Euler angles)
        private Mat3 orientation = Mat3.Identity;
        private bool orientationValid = false;
        private double writtenX, writtenY, writtenZ;

        // Mass properties, measured once in a reference orientation and rotated from there
        private bool massPropsDirty = true;
        private Mat3 refOrientation = Mat3.Identity;
        private Vec3 refComOffset = Vec3.Zero;     // item origin -> center of mass, in the reference orientation
        private Vec3 refInvInertia = Vec3.Zero;    // 1 / moment of inertia around each reference axis

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

        /// <summary>
        /// A body belongs to one Item, so it can't be copied on its own. Use Item.Duplicate()
        /// (copies the item and its body together) or DuplicateFor(otherItem).
        /// </summary>
        public override Proportie Duplicate()
        {
            throw new InvalidOperationException(
                "A PhysicsBody belongs to one Item. Use item.Duplicate() or body.DuplicateFor(newItem).");
        }

        /// <summary>
        /// A new body on newOwner, in the same world, with all the same settings and motion.
        /// It adds itself to newOwner. Collision event handlers are not copied.
        /// </summary>
        public override Proportie DuplicateFor(Item newOwner)
        {
            var copy = new PhysicsBody(newOwner, world, mass)
            {
                velocity = velocity,
                isStatic = isStatic,
                useGravity = useGravity,
                bounciness = bounciness,
                friction = friction,
                drag = drag,
                angularVelocity = angularVelocity,
                angularDrag = angularDrag,
                freezeRotation = freezeRotation,
            };
            if (_colliderMesh != null) copy.colliderMesh = (Mesh)_colliderMesh.Duplicate();
            return copy;
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

        // ---------------- Rotation ----------------

        /// <summary>True if this body can currently spin.</summary>
        public bool CanRotate => !isStatic && !freezeRotation && mass > 0 && !Shape.IsEmpty;

        /// <summary>World position of the center of mass (the point the body spins around).</summary>
        public Vec3 CenterOfMass => Position + ComOffset;

        /// <summary>Continuous twist (e.g. a motor or a wheel). Units: mass * distance^2 / s^2.</summary>
        public void ApplyTorque(Vec3 torque) { accumulatedTorque = accumulatedTorque + torque; }

        /// <summary>Instant twist: changes spin by the inverse inertia times the impulse.</summary>
        public void ApplyAngularImpulse(Vec3 angularImpulse)
        {
            if (!CanRotate) return;
            angularVelocity = angularVelocity + InverseInertiaTimes(angularImpulse);
        }

        /// <summary>
        /// Push at a point in the world (e.g. thruster on a wing tip). Pushing off-center
        /// both moves the body and makes it spin.
        /// </summary>
        public void ApplyForceAtPoint(Vec3 force, Vec3 worldPoint)
        {
            ApplyForce(force);
            if (CanRotate) ApplyTorque(Vec3.Cross(worldPoint - CenterOfMass, force));
        }

        /// <summary>Instant kick at a point in the world (e.g. a bullet hit). Off-center hits spin the body.</summary>
        public void ApplyImpulseAtPoint(Vec3 impulse, Vec3 worldPoint)
        {
            if (isStatic) return;
            ApplyContactImpulse(impulse, worldPoint - CenterOfMass);
        }

        /// <summary>Velocity of a point on the body (moving plus spinning).</summary>
        public Vec3 VelocityAtPoint(Vec3 worldPoint) => velocity + Vec3.Cross(angularVelocity, worldPoint - CenterOfMass);

        internal Vec3 ConsumeTorque() { Vec3 t = accumulatedTorque; accumulatedTorque = Vec3.Zero; return t; }

        /// <summary>Center of mass offset from the item origin, in world axes.</summary>
        internal Vec3 ComOffset
        {
            get
            {
                if (isStatic || Shape.IsEmpty) return Shape.IsEmpty ? Vec3.Zero : Shape.Center;
                SyncOrientation();
                EnsureMassProps();
                return (orientation * refOrientation.Transposed) * refComOffset;
            }
        }

        /// <summary>Inverse inertia (in world axes) applied to v. Zero if the body can't rotate.</summary>
        internal Vec3 InverseInertiaTimes(Vec3 v)
        {
            if (!CanRotate) return Vec3.Zero;
            SyncOrientation();
            EnsureMassProps();
            Mat3 q = orientation * refOrientation.Transposed;   // rotation since the reference
            Vec3 local = q.Transposed * v;
            local = new Vec3(local.X * refInvInertia.X, local.Y * refInvInertia.Y, local.Z * refInvInertia.Z);
            return q * local;
        }

        /// <summary>Linear + angular response to an impulse at offset r from the center of mass.</summary>
        internal void ApplyContactImpulse(Vec3 impulse, Vec3 r)
        {
            if (isStatic) return;
            velocity = velocity + impulse * InverseMass;
            if (CanRotate) angularVelocity = angularVelocity + InverseInertiaTimes(Vec3.Cross(r, impulse));
        }

        /// <summary>
        /// How hard it is to change the velocity along direction n at offset r through spinning.
        /// Goes into the bottom of the impulse formula next to the inverse mass.
        /// </summary>
        internal double AngularEffect(Vec3 r, Vec3 n)
        {
            if (!CanRotate) return 0;
            return Vec3.Dot(n, Vec3.Cross(InverseInertiaTimes(Vec3.Cross(r, n)), r));
        }

        /// <summary>Re-read the orientation if a script (or the constructor) changed xrot/yrot/zrot.</summary>
        private void SyncOrientation()
        {
            if (orientationValid && owner.xrot == writtenX && owner.yrot == writtenY && owner.zrot == writtenZ) return;
            orientation = Mat3.FromEulerDegrees(owner.xrot, owner.yrot, owner.zrot);
            writtenX = owner.xrot; writtenY = owner.yrot; writtenZ = owner.zrot;
            orientationValid = true;
        }

        /// <summary>
        /// Measures center of mass and inertia from the collision shape's bounding box
        /// (treated as a solid box of this body's mass). Redone when the meshes change.
        /// </summary>
        private void EnsureMassProps()
        {
            if (!massPropsDirty) return;
            var s = Shape;
            if (s.IsEmpty) return;
            SyncOrientation();
            massPropsDirty = false;
            refOrientation = orientation;
            refComOffset = s.Center;

            Vec3 size = s.max - s.min;
            double m = Math.Max(mass, 1e-9);
            double ix = m * (size.Y * size.Y + size.Z * size.Z) / 12.0;
            double iy = m * (size.X * size.X + size.Z * size.Z) / 12.0;
            double iz = m * (size.X * size.X + size.Y * size.Y) / 12.0;
            refInvInertia = new Vec3(ix > 1e-12 ? 1 / ix : 0, iy > 1e-12 ? 1 / iy : 0, iz > 1e-12 ? 1 / iz : 0);
        }

        /// <summary>Called by the world each step: torque -> spin -> new orientation.</summary>
        internal void IntegrateRotation(double dt, bool wasGrounded)
        {
            Vec3 torque = ConsumeTorque();
            if (!CanRotate) { angularVelocity = Vec3.Zero; return; }
            SyncOrientation();
            EnsureMassProps();

            angularVelocity = angularVelocity + InverseInertiaTimes(torque) * dt;
            angularVelocity = angularVelocity * Math.Max(0.0, 1.0 - angularDrag * dt);

            // Let resting objects settle instead of trembling forever
            if (wasGrounded && angularVelocity.LengthSquared < 0.0025 && velocity.LengthSquared < 25)
                angularVelocity = Vec3.Zero;

            double w = angularVelocity.Length;
            if (w * dt < 1e-9) return;

            // Spin around the center of mass, not the item origin
            Vec3 oldOffset = ComOffset;
            orientation = (Mat3.AxisAngle(angularVelocity / w, w * dt) * orientation).Orthonormalized();
            orientation.ToEulerDegrees(out double xr, out double yr, out double zr);
            owner.xrot = xr; owner.yrot = yr; owner.zrot = zr;
            writtenX = xr; writtenY = yr; writtenZ = zr;
            Position = Position + oldOffset - ComOffset;
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
                if (shape == null || shapeDirty || sig != lastSignature)
                    massPropsDirty = true;    // the meshes changed, not just the rotation
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
            // Changes whenever a mesh is added/removed, switched on/off for collision, or reshaped
            // (scale, animation), so the collision shape rebuilds by itself
            if (_colliderMesh != null) return -1 - _colliderMesh.ShapeVersion;
            int sig = 17;
            foreach (Proportie p in owner.properties)
                if (p is Mesh m && m.collides)
                    sig = (sig * 31 + m.oldpoints.Count + 1) * 31 + m.ShapeVersion;
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
                : owner.properties.OfType<Mesh>().Where(m => m.collides);

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
                    b.IntegrateRotation(dt, b.isGrounded);
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
            if (handledTris.Length < tris.Length) handledTris = new bool[tris.Length * 2];
            Array.Clear(handledTris, 0, tris.Length);
            for (int t = 0; t < tris.Length; t++)
            {
                if (handledTris[t]) continue;     // already solved together with a flat neighbour
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

                    // A flat surface is usually several triangles (a box top is 2). Solve the
                    // corners on all of them together, or the first triangle takes the whole
                    // landing and twists the body.
                    mergedContacts.Clear(); mergedGaps.Clear();
                    for (int u = t; u < tris.Length; u++)
                    {
                        if (u != t)
                        {
                            if (handledTris[u] || Math.Abs(Vec3.Dot(tris[u].normal, fn)) < 0.9999) continue;
                            if (Math.Abs(Vec3.Dot(fn, tris[u].a) - planeD) > slop) continue;
                            if (!Overlaps(aMin, aMax, tris[u].min, tris[u].max)) continue;
                        }
                        handledTris[u] = true;
                        triVerts[0] = tris[u].a; triVerts[1] = tris[u].b; triVerts[2] = tris[u].c;
                        ContactPoints(normal, depth, shape, a.Position, triVerts, Vec3.Zero, true);
                        for (int k = 0; k < contacts.Count; k++)
                        {
                            bool dup = false;
                            foreach (Vec3 q in mergedContacts) if ((q - contacts[k]).LengthSquared < slop * slop) { dup = true; break; }
                            if (!dup) { mergedContacts.Add(contacts[k]); mergedGaps.Add(contactGaps[k]); }
                        }
                    }
                    Respond(a, s, normal, depth, mergedContacts, mergedGaps);
                    continue;
                }
                else if (!Gjk.Penetration(hullA, tri, simplex, out normal, out depth))
                {
                    continue;   // already beside/below this triangle: use the smallest way out
                }
                triVerts[0] = tris[t].a; triVerts[1] = tris[t].b; triVerts[2] = tris[t].c;
                ContactPoints(normal, depth, shape, a.Position, triVerts, Vec3.Zero, true);
                Respond(a, s, normal, depth, contacts, contactGaps);
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
            ContactPoints(normal, depth, a.Shape, a.Position, b.Shape.verts, b.Position, false);
            Respond(a, b, normal, depth, contacts, contactGaps);
        }

        private readonly Vec3[] triVerts = new Vec3[3];
        private readonly List<Vec3> deepA = new List<Vec3>();
        private readonly List<Vec3> deepB = new List<Vec3>();
        private readonly List<Vec3> contacts = new List<Vec3>();
        private readonly List<double> contactGaps = new List<double>();
        private readonly List<Vec3> mergedContacts = new List<Vec3>();
        private readonly List<double> mergedGaps = new List<double>();
        private bool[] handledTris = new bool[64];
        private readonly List<Vec3> polyA = new List<Vec3>();
        private readonly List<Vec3> polyB = new List<Vec3>();
        private readonly List<Vec3> clipIn = new List<Vec3>();
        private readonly List<Vec3> clipOut = new List<Vec3>();

        /// <summary>
        /// Fills 'contacts' with the corners of the patch where A and B touch, and
        /// 'contactGaps' with how far each corner still is from actually touching.
        ///
        /// Takes the face of A that faces B and the face of B that faces A, and cuts one
        /// to the outline of the other (seen along the normal). A box lying flat gets its 4
        /// bottom corners (so it sits still), a box on an edge gets that edge (so it tips),
        /// and a box hanging over a ledge gets the part still on the ledge, including points
        /// along the ledge's edge (so it only falls when its center is past the edge).
        /// </summary>
        private void ContactPoints(Vec3 normal, double depth, CollisionShape shapeA, Vec3 offsetA,
                                   Vec3[] vertsB, Vec3 offsetB, bool bIsTriangle)
        {
            contacts.Clear(); contactGaps.Clear();
            double tol = 0.01 * (shapeA.max - shapeA.min).Length + 1e-6;
            double maxA = Deepest(shapeA.verts, offsetA, normal, depth + tol, deepA);
            double maxB = Deepest(vertsB, offsetB, -normal, depth + tol, deepB);

            // Two directions along the contact surface
            Vec3 helper = Math.Abs(normal.X) < 0.9 ? new Vec3(1, 0, 0) : new Vec3(0, 1, 0);
            Vec3 t1 = Vec3.Cross(normal, helper).Normalized;
            Vec3 t2 = Vec3.Cross(normal, t1);

            Outline(deepA, t1, t2, tol * 0.1, polyA);
            Outline(deepB, t1, t2, tol * 0.1, polyB);

            // Cut the smaller feature (point / edge / face) to the outline of the face it touches
            bool fromA;
            if (polyB.Count >= 3) { Clip(polyA, polyB, t1, t2); fromA = true; }
            else if (polyA.Count >= 3) { Clip(polyB, polyA, t1, t2); fromA = false; }
            else { clipOut.Clear(); fromA = true; }

            foreach (Vec3 p in clipOut)
            {
                // Gap = how much further this point must travel along the normal to touch
                double gap = fromA ? maxA - Vec3.Dot(p, normal) : maxB - Vec3.Dot(p, -normal);
                bool duplicate = false;
                foreach (Vec3 q in contacts) if ((q - p).LengthSquared < tol * tol * 0.01) { duplicate = true; break; }
                if (duplicate) continue;
                contacts.Add(p);
                contactGaps.Add(Math.Max(0, gap));
            }

            if (contacts.Count == 0)   // edge crossing edge, or numerical trouble: one point in the middle
            {
                contacts.Add(ContactPoint(normal, depth, shapeA, shapeA.verts, offsetA, vertsB, offsetB, deepA, deepB));
                contactGaps.Add(0);
            }
        }

        /// <summary>Convex outline of points seen along the normal, counter-clockwise in (t1, t2).</summary>
        private static void Outline(List<Vec3> pts, Vec3 t1, Vec3 t2, double eps, List<Vec3> into)
        {
            into.Clear();
            var sorted = new List<Vec3>(pts);
            sorted.Sort((p, q) =>
            {
                int c = Vec3.Dot(p, t1).CompareTo(Vec3.Dot(q, t1));
                return c != 0 ? c : Vec3.Dot(p, t2).CompareTo(Vec3.Dot(q, t2));
            });
            // Drop points that sit on top of each other (seen along the normal)
            var uniq = new List<Vec3>();
            foreach (var p in sorted)
            {
                bool same = false;
                foreach (var q in uniq)
                    if (Math.Abs(Vec3.Dot(p - q, t1)) < eps && Math.Abs(Vec3.Dot(p - q, t2)) < eps) { same = true; break; }
                if (!same) uniq.Add(p);
            }
            if (uniq.Count < 3) { into.AddRange(uniq); return; }

            // Monotone chain convex hull
            double Turn(Vec3 o, Vec3 a, Vec3 b) =>
                Vec3.Dot(a - o, t1) * Vec3.Dot(b - o, t2) - Vec3.Dot(a - o, t2) * Vec3.Dot(b - o, t1);
            var hull = new List<Vec3>();
            foreach (var p in uniq)
            {
                while (hull.Count >= 2 && Turn(hull[hull.Count - 2], hull[hull.Count - 1], p) <= eps * eps) hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            int lower = hull.Count + 1;
            for (int i = uniq.Count - 2; i >= 0; i--)
            {
                var p = uniq[i];
                while (hull.Count >= lower && Turn(hull[hull.Count - 2], hull[hull.Count - 1], p) <= eps * eps) hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            hull.RemoveAt(hull.Count - 1);
            into.AddRange(hull);
        }

        /// <summary>
        /// Sutherland-Hodgman: cut 'subject' (a point, an edge or a polygon) to the inside of
        /// the convex counter-clockwise outline 'clip', working in the (t1, t2) plane. New
        /// points along cut edges keep the subject's height. Result goes in clipOut.
        /// </summary>
        private void Clip(List<Vec3> subject, List<Vec3> clip, Vec3 t1, Vec3 t2)
        {
            clipOut.Clear();
            clipOut.AddRange(subject);
            for (int e = 0; e < clip.Count && clipOut.Count > 0; e++)
            {
                Vec3 c0 = clip[e], c1 = clip[(e + 1) % clip.Count];
                double ex = Vec3.Dot(c1 - c0, t1), ey = Vec3.Dot(c1 - c0, t2);
                double len = Math.Sqrt(ex * ex + ey * ey);
                if (len < 1e-12) continue;
                double Side(Vec3 p) => (ex * Vec3.Dot(p - c0, t2) - ey * Vec3.Dot(p - c0, t1)) / len;   // >= 0 inside

                clipIn.Clear(); clipIn.AddRange(clipOut);
                clipOut.Clear();
                const double eps = 1e-7;
                if (clipIn.Count == 1)
                {
                    if (Side(clipIn[0]) >= -eps) clipOut.Add(clipIn[0]);
                    continue;
                }
                if (clipIn.Count == 2)
                {
                    Vec3 s = clipIn[0], f = clipIn[1];
                    double ds = Side(s), df = Side(f);
                    if (ds >= -eps) clipOut.Add(s);
                    if ((ds >= -eps) != (df >= -eps)) clipOut.Add(s + (f - s) * (ds / (ds - df)));
                    if (df >= -eps) clipOut.Add(f);
                    continue;
                }
                for (int i = 0; i < clipIn.Count; i++)
                {
                    Vec3 s = clipIn[i], f = clipIn[(i + 1) % clipIn.Count];
                    double ds = Side(s), df = Side(f);
                    bool sIn = ds >= -eps, fIn = df >= -eps;
                    if (sIn) clipOut.Add(s);
                    if (sIn != fIn) clipOut.Add(s + (f - s) * (ds / (ds - df)));
                }
            }
        }

        /// <summary>
        /// Single fallback contact: the middle of where A's deepest points and B's deepest
        /// points overlap (used when no corner clearly sits over the other shape).
        /// </summary>
        private static Vec3 ContactPoint(Vec3 normal, double depth, CollisionShape shapeA,
                                         Vec3[] vertsA, Vec3 offsetA, Vec3[] vertsB, Vec3 offsetB,
                                         List<Vec3> setA = null, List<Vec3> setB = null)
        {
            setA = setA ?? new List<Vec3>(); setB = setB ?? new List<Vec3>();
            double tol = Math.Max(depth, 0.02 * (shapeA.max - shapeA.min).Length) + 1e-6;
            Deepest(vertsA, offsetA, normal, tol, setA);
            Deepest(vertsB, offsetB, -normal, tol, setB);

            // Two directions along the contact surface
            Vec3 helper = Math.Abs(normal.X) < 0.9 ? new Vec3(1, 0, 0) : new Vec3(0, 1, 0);
            Vec3 t1 = Vec3.Cross(normal, helper).Normalized;
            Vec3 t2 = Vec3.Cross(normal, t1);

            Extent(setA, t1, t2, normal, out double aMin1, out double aMax1, out double aMin2, out double aMax2, out double aN);
            Extent(setB, t1, t2, normal, out double bMin1, out double bMax1, out double bMin2, out double bMax2, out double bN);

            double lo1 = Math.Max(aMin1, bMin1), hi1 = Math.Min(aMax1, bMax1);
            double lo2 = Math.Max(aMin2, bMin2), hi2 = Math.Min(aMax2, bMax2);
            double c1, c2;
            if (lo1 <= hi1 && lo2 <= hi2) { c1 = (lo1 + hi1) / 2; c2 = (lo2 + hi2) / 2; }
            else
            {
                // Patches don't overlap (edge against edge etc.): split the difference
                c1 = ((aMin1 + aMax1) / 2 + (bMin1 + bMax1) / 2) / 2;
                c2 = ((aMin2 + aMax2) / 2 + (bMin2 + bMax2) / 2) / 2;
            }
            return t1 * c1 + t2 * c2 + normal * ((aN + bN) / 2);
        }

        private static double Deepest(Vec3[] verts, Vec3 offset, Vec3 dir, double tol, List<Vec3> into)
        {
            into.Clear();
            double best = double.MinValue;
            foreach (var v in verts) best = Math.Max(best, Vec3.Dot(v + offset, dir));
            foreach (var v in verts)
                if (Vec3.Dot(v + offset, dir) >= best - tol) into.Add(v + offset);
            return best;
        }

        private static void Extent(List<Vec3> pts, Vec3 t1, Vec3 t2, Vec3 n,
                                   out double min1, out double max1, out double min2, out double max2, out double avgN)
        {
            min1 = min2 = double.MaxValue; max1 = max2 = double.MinValue; avgN = 0;
            foreach (var p in pts)
            {
                double d1 = Vec3.Dot(p, t1), d2 = Vec3.Dot(p, t2);
                min1 = Math.Min(min1, d1); max1 = Math.Max(max1, d1);
                min2 = Math.Min(min2, d2); max2 = Math.Max(max2, d2);
                avgN += Vec3.Dot(p, n);
            }
            if (pts.Count > 0) avgN /= pts.Count;
        }

        private static double LowestAlong(CollisionShape shape, Vec3 pos, Vec3 n)
        {
            double best = double.MaxValue;
            foreach (var v in shape.verts) best = Math.Min(best, Vec3.Dot(n, v));
            return best + Vec3.Dot(n, pos);
        }

        private static Vec3 PointVelocity(PhysicsBody body, Vec3 r) =>
            body.velocity + Vec3.Cross(body.angularVelocity, r);

        /// <summary>
        /// Push apart, then bounce and apply friction at each contact point. normal points
        /// from a towards b. Off-center contacts make bodies spin; bodies with freezeRotation
        /// (or static ones) take no spin, which gives the same result as the old version.
        /// </summary>
        private void Respond(PhysicsBody a, PhysicsBody b, Vec3 normal, double depth, List<Vec3> points, List<double> gaps)
        {
            double invA = a.InverseMass, invB = b.InverseMass;
            double invSum = invA + invB;
            if (invSum <= 0 || depth <= 0) return;

            // Centers of mass before pushing apart (the contact points were measured then too)
            Vec3 comA = a.CenterOfMass, comB = b.CenterOfMass;

            // Positional correction, split by mass
            a.Position = a.Position - normal * (depth * invA / invSum);
            if (invB > 0) b.Position = b.Position + normal * (depth * invB / invSum);

            double mu = Math.Sqrt(a.friction * b.friction);
            double e = Math.Min(a.bounciness, b.bounciness);
            double restingSpeed = gravity.Length * fixedTimeStep * 3;
            double touchGap = 0.002 * Math.Max(1.0, (a.Shape.max - a.Shape.min).Length);

            // Per contact point: lever arms, how hard it is to push there, the speed apart we
            // want afterwards (bounce), and the push / friction applied so far.
            int n = points.Count;
            EnsureSolverBuffers(n);
            bool hit = false;
            for (int i = 0; i < n; i++)
            {
                rAs[i] = points[i] - comA;
                rBs[i] = points[i] - comB;
                kNs[i] = invSum + a.AngularEffect(rAs[i], normal) + b.AngularEffect(rBs[i], normal);
                double vn = Vec3.Dot(PointVelocity(b, rBs[i]) - PointVelocity(a, rAs[i]), normal);
                if (gaps[i] > touchGap)
                {
                    // Not touching yet: it may keep closing in, just not past the surface this step
                    targets[i] = -gaps[i] / fixedTimeStep;
                }
                else
                {
                    // Touching: bounce, except for gentle contacts (stops resting objects buzzing)
                    targets[i] = (vn < -restingSpeed) ? -e * vn : 0;
                }
                if (vn < targets[i]) hit = true;
                jNs[i] = 0;
                jTs[i] = Vec3.Zero;
            }
            if (!hit) { MarkTouching(a, b, normal); return; }

            // Solve all corners together a few times, so the push is shared fairly between
            // them instead of the first corner taking it all and twisting the body.
            for (int iter = 0; iter < 6; iter++)
            {
                for (int i = 0; i < n; i++)
                {
                    Vec3 rA = rAs[i], rB = rBs[i];

                    // Normal: push apart until this corner moves apart at the target speed (never pull)
                    double vn = Vec3.Dot(PointVelocity(b, rB) - PointVelocity(a, rA), normal);
                    double newJ = Math.Max(jNs[i] + (targets[i] - vn) / kNs[i], 0);
                    double dj = newJ - jNs[i];
                    jNs[i] = newJ;
                    if (dj != 0)
                    {
                        a.ApplyContactImpulse(normal * -dj, rA);
                        b.ApplyContactImpulse(normal * dj, rB);
                    }

                    // Friction (Coulomb): oppose sliding at this corner, limited by how hard it
                    // presses. Applied at the corner, so a box sliding on the floor can trip over.
                    Vec3 relVel = PointVelocity(b, rB) - PointVelocity(a, rA);
                    Vec3 slide = relVel - normal * Vec3.Dot(relVel, normal);
                    double sLen = slide.Length;
                    if (sLen < 1e-9) continue;
                    Vec3 tangent = slide / sLen;
                    double kt = invSum + a.AngularEffect(rA, tangent) + b.AngularEffect(rB, tangent);
                    Vec3 newT = jTs[i] + tangent * (sLen / kt);   // friction impulse acting on a
                    double maxT = mu * jNs[i];
                    double tLen = newT.Length;
                    if (tLen > maxT) newT = newT * (maxT / tLen);
                    Vec3 dT = newT - jTs[i];
                    jTs[i] = newT;
                    a.ApplyContactImpulse(dT, rA);
                    b.ApplyContactImpulse(-dT, rB);
                }
            }

            // Did friction hold everywhere (nothing slipping)?
            bool frictionHeld = true;
            for (int i = 0; i < n; i++)
                if (jNs[i] > 0 && jTs[i].Length >= mu * jNs[i] * 0.999) frictionHeld = false;

            // Friction held completely against something fixed: also undo the little slide
            // this step already made, or objects slowly creep down slopes.
            // Skipped while spinning, so rolling and tipping over still work.
            if (frictionHeld && invB == 0 && a.angularVelocity.LengthSquared < 0.01)
            {
                Vec3 moved = a.Position - a.prevPosition;
                a.Position = a.Position - (moved - normal * Vec3.Dot(moved, normal));
            }

            MarkTouching(a, b, normal);
        }

        private void MarkTouching(PhysicsBody a, PhysicsBody b, Vec3 normal)
        {
            // Grounded = something under us relative to gravity (slopes up to ~60 degrees)
            Vec3 down = gravity.Normalized;
            if (Vec3.Dot(normal, down) > 0.5) a.isGrounded = true;   // b is below a
            if (Vec3.Dot(-normal, down) > 0.5) b.isGrounded = true;  // a is below b

            touching.Add((a, b));
            contactsResolved++;
        }

        // Reused per-contact scratch space for Respond
        private Vec3[] rAs = new Vec3[8], rBs = new Vec3[8], jTs = new Vec3[8];
        private double[] kNs = new double[8], targets = new double[8], jNs = new double[8];
        private void EnsureSolverBuffers(int n)
        {
            if (rAs.Length >= n) return;
            int size = Math.Max(n, rAs.Length * 2);
            rAs = new Vec3[size]; rBs = new Vec3[size]; jTs = new Vec3[size];
            kNs = new double[size]; targets = new double[size]; jNs = new double[size];
        }
    }


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
        private Queue<Item> thrown = new Queue<Item>();
        private Random rnd = new Random();

        /// <summary>A copy keeps its own list of thrown boxes.</summary>
        public override Proportie Duplicate()
        {
            var copy = (BoxThrower)base.Duplicate();
            copy.thrown = new Queue<Item>();
            copy.rnd = new Random();
            return copy;
        }

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
            Mesh mesh = new Mesh("Models\\Crate.json", 0, 0, 0, 0, 0, 0);
            mesh.scale(5, 5, 5);
            box.add_propertie(mesh);

            var body = new PhysicsBody(box, scene.physics, mass: 1) { bounciness = 0.35 };
            body.velocity = forward * throwSpeed + up * (throwSpeed * 0.3);
            // A little random tumble so throws look natural (radians per second)
            body.angularVelocity = new Vec3(rnd.NextDouble() - 0.5, rnd.NextDouble() - 0.5, rnd.NextDouble() - 0.5) * 8;

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