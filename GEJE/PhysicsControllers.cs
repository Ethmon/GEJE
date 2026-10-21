using System;
using System.Runtime.Intrinsics.X86;

namespace GEJE
{
    public class Physics_Mover : Proportie
    {
        /// <summary>
        /// Rotation of the movement around the y axis
        /// </summary>
        public float rotation = 0;

        /// <summary>
        /// Mulltiplys the Movement Vector
        /// </summary>
        public double multiplyer;

        /// <summary>
        /// How fast velocity changes toward the target on the ground (units/s²).
        /// Keep under ~3300 or walls can hold you up in the air again.
        /// </summary>
        public double acceleration = 3000;

        /// <summary>
        /// How fast velocity changes toward the target in the air (units/s²).
        /// </summary>
        public double airAcceleration = 1500;


        /// <summary>
        /// where to grab the key strokes
        /// </summary>
        public Window window;

        private PhysicsBody Controlled;

        public Physics_Mover(double x, double y, double z, double xrot, double yrot, double zrot, PhysicsBody Controlled) : base(x, y, z, xrot, yrot, zrot)
        {
            this.Controlled = Controlled;
        }
        Vec3 Movement = new Vec3();
        Vec3 Temp = new Vec3();
        bool aa = false;
        public override void Update()
        {

            double time = ThreeDSceen.deltaTime;   // seconds since last frame
            Temp = Temp * 0;  
            if (window.pressed.Count != 0)
            {
                //Console.WriteLine("notfrozen");
                float yRotRad = (float)(Math.PI * rotation / 180.0);
                //Console.WriteLine(yRotRad);
                // Calculate movement direction based on current rotation
                Movement.X = Math.Sin(-yRotRad);
                Movement.Z = Math.Cos(-yRotRad);
                //Console.WriteLine(moved.yrot);
                //Console.WriteLine(yRotRad);
                //Console.WriteLine((Math.PI * moved.yrot / 180.0 == yRotRad) ? "true" : "false");

                if (rotation % 90 == 0)
                {
                    if (rotation % 360 == 0)
                    {
                        Movement.X = 0.0;
                        Movement.Z = 1.0;
                    }
                    else if (rotation % 180 == 0)
                    {
                        Movement.X = -0.0;
                        Movement.Z = -1.0;
                    }
                    else
                    {
                        if (rotation == 90 || rotation == -270)
                        {
                            Movement.X = -1.0;
                            Movement.Z = 0.0;
                        }
                        else if (rotation == 270 || rotation == -90)
                        {
                            Movement.X = 1.0;
                            Movement.Z = 0.0;
                        }
                    }
                }


                if (window.pressed.Contains(87) ^ window.pressed.Contains(83))
                {
                    if (window.pressed.Contains(83))
                    {
                        Temp.X += Movement.X * multiplyer;
                        Temp.Z -= Movement.Z * multiplyer;
                    }
                    else
                    {
                        Temp.X -= Movement.X * multiplyer;
                        Temp.Z += Movement.Z * multiplyer;
                    }
                }

                if (window.pressed.Contains(65) ^ window.pressed.Contains(68))
                {
                    if (window.pressed.Contains(65))
                    {
                        Temp.X -= Movement.Z * multiplyer;
                        Temp.Z -= Movement.X * multiplyer;
                    }
                    else
                    {
                        Temp.X += Movement.Z * multiplyer;
                        Temp.Z += Movement.X * multiplyer;
                    }
                }

                if (window.pressed.Contains(32))
                {
                    if (!aa)
                    {
                        if (Controlled.isGrounded)
                        {
                            Controlled.ApplyImpulse(new Vec3(0, -multiplyer * 1.96, 0)); aa = true;
                        }

                    }
                }
                else aa = false;
            }
            else
            {

            }

            double accel = Controlled.isGrounded ? acceleration : airAcceleration;
            double dx = Temp.X - Controlled.velocity.X;
            double dz = Temp.Z - Controlled.velocity.Z;
            double dist = Math.Sqrt(dx * dx + dz * dz);
            double maxStep = accel * time;
            if (dist > maxStep)
            {
                dx *= maxStep / dist;
                dz *= maxStep / dist;
            }
            Controlled.velocity.X += dx;
            Controlled.velocity.Z += dz;
        }
    }
}