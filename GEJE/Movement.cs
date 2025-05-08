using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GEJE
{
    public class Movement : Proportie
    {
        private Item moved;
        public Window window;
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
        double yRotRad = 0;
        double movementX = 0;
        double movementY = 0;
        public override void Update()
        {
            if (window.pressed.Count()!=0)
            {
                yRotRad = Math.PI * moved.yrot / 180.0;
                //Console.WriteLine(yRotRad);
                // Calculate movement direction based on current rotation
                movementX = Math.Sin(-yRotRad);
                movementY = Math.Cos(-yRotRad);
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
                    movementX = -1.0;
                    movementY = 0.0;
                }
                else if (moved.yrot == 180 || moved.yrot == -180)
                {
                    movementX = -0.0;
                    movementY = -1.0;
                }
                else if (moved.yrot == 270 || moved.yrot == -90)
                {
                    movementX = 1.0;
                    movementY = 0.0;
                }


                if (window.pressed.Contains(83))
                {
                    moved.move(moved.x - movementX* speed, moved.y , moved.z - movementY* speed, moved.w);
                }
                if (window.pressed.Contains(87))
                {
                    moved.move(moved.x + movementX* speed, moved.y , moved.z + movementY* speed, moved.w);
                }
                if (window.pressed.Contains(68))
                {
                    moved.move(moved.x - movementY* speed, moved.y , moved.z + movementX* speed, moved.w);
                }
                if (window.pressed.Contains(65))
                {
                    moved.move(moved.x + movementY* speed, moved.y , moved.z - movementX* speed, moved.w);
                }
                if(window.pressed.Contains(69))
                {
                    moved.move(moved.x, moved.y + speed, moved.z, moved.w);
                }
                if(window.pressed.Contains(81))
                {
                    moved.move(moved.x, moved.y - speed, moved.z, moved.w);
                }
                if (window.pressed.Contains(38))
                {
                    moved.rotate(moved.xrot-.5, moved.yrot, moved.zrot, moved.w);
                }
                if (window.pressed.Contains(40))
                {
                    moved.rotate(moved.xrot+.5, moved.yrot, moved.zrot, moved.w);
                }
                if (window.pressed.Contains(39))
                {
                    moved.rotate(moved.xrot, moved.yrot+.5, moved.zrot, moved.w);
                }
                if (window.pressed.Contains(37))
                {
                    moved.rotate(moved.xrot, moved.yrot-.5, moved.zrot, moved.w);
                }
                //Console.WriteLine("x: " + moved.x + " y: " + moved.y + " z: " + moved.z + " xrot: " + moved.xrot + " yrot: " + moved.yrot + " zrot: " + moved.zrot);
            }
        }
    }
}
