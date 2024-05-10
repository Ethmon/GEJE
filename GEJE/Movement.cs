using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GEJE
{
    internal class Movement : Proportie
    {
        private Item moved;
        float speed = 0.1f;
        public Movement(double x, double y, double z, double xrot, double yrot, double zrot,Item moved, float speed) : base(x, y, z, xrot, yrot, zrot)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.xrot = xrot;
            this.yrot = yrot;
            this.zrot = zrot;
            this.moved = moved;
            this.speed = speed;
        }
        public override void Update()
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                double yRotRad = Math.PI * moved.yrot / 180.0;
                //Console.WriteLine(yRotRad);
                // Calculate movement direction based on current rotation
                double movementX = Math.Sin(-yRotRad);
                double movementY = Math.Cos(-yRotRad);
                //Console.WriteLine(moved.yrot);
                //Console.WriteLine(yRotRad);
                //Console.WriteLine((Math.PI * moved.yrot / 180.0 == yRotRad) ? "true" : "false");
                if (moved.yrot == 0 || moved.yrot == 360 || moved.yrot == -360)
                {
                    movementX = 0.0;
                    movementY = 1.0;
                }
                else if (moved.yrot == 90 || moved.yrot == -270)
                {
                    movementX = 1.0;
                    movementY = 0.0;
                }
                else if (moved.yrot == 180 || moved.yrot == -180)
                {
                    movementX = -0.0;
                    movementY = -1.0;
                }
                else if (moved.yrot == 270 || moved.yrot == -90)
                {
                    movementX = -1.0;
                    movementY = 0.0;
                }


                if (keyInfo.Key == ConsoleKey.S)
                {
                    moved.move(moved.x - movementX* speed, moved.y , moved.z - movementY* speed, moved.w);
                }
                if (keyInfo.Key == ConsoleKey.W)
                {
                    moved.move(moved.x + movementX* speed, moved.y , moved.z + movementY* speed, moved.w);
                }
                if (keyInfo.Key == ConsoleKey.A)
                {
                    moved.move(moved.x - movementY* speed, moved.y , moved.z - movementX* speed, moved.w);
                }
                if (keyInfo.Key == ConsoleKey.D)
                {
                    moved.move(moved.x + movementY* speed, moved.y , moved.z + movementX* speed, moved.w);
                }
                if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    moved.rotate(moved.xrot-.25, moved.yrot, moved.zrot, moved.w);
                }
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    moved.rotate(moved.xrot+.25, moved.yrot, moved.zrot, moved.w);
                }
                if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    moved.rotate(moved.xrot, moved.yrot+.25, moved.zrot, moved.w);
                }
                if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    moved.rotate(moved.xrot, moved.yrot-.25, moved.zrot, moved.w);
                }
                //.WriteLine("x: " + moved.x + " y: " + moved.y + " z: " + moved.z + " xrot: " + moved.xrot + " yrot: " + moved.yrot + " zrot: " + moved.zrot);
            }
        }
    }
}
