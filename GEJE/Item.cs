using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace GEJE
{
    public class Item
    {
        public double x, y, z, w;
        public static bool rotatei = false;
        public static bool floatingyay = false;
        private float momentumU = 1f;
        public static bool NAH = false;
        public double xrot, yrot, zrot, wrot;
        public List<Proportie> properties = new List<Proportie>();
        public bool state = true;
        public Item(double x, double y, double z, double xrot, double yrot, double zrot)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.xrot = xrot;
            this.yrot = yrot;
            this.zrot = zrot;

        }

        public void add_propertie(Proportie propertie)
        {
            properties.Add(propertie);
        }
        public void move(double x, double y, double z, double w) // Add w parameter
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w; // Set w
        }

        public void rotate(double x, double y, double z, double w) // Add w parameter
        {
            this.xrot = x;
            this.yrot = y;
            this.zrot = z;
            this.wrot = w; // Set wrot
        }
        public void Start()
        {
            foreach (Proportie propertie in properties)
            {
                propertie.Start();
            }
        }
        bool dddd = true;
        public void Update()
        {
            this.xrot = Rotation.WrapAngle(this.xrot);
            this.yrot = Rotation.WrapAngle(this.yrot);
            this.zrot = Rotation.WrapAngle(this.zrot);
            double xRotRad = this.xrot * (Math.PI / 180);
            double yRotRad = this.yrot * (Math.PI / 180);
            double zRotRad = this.zrot * (Math.PI / 180);
            double[,] rotationMatrixX = Rotation.GetRotationMatrixX(xRotRad);
            double[,] rotationMatrixY = Rotation.GetRotationMatrixY(yRotRad);
            double[,] rotationMatrixZ = Rotation.GetRotationMatrixZ(zRotRad);
            // Combine rotation matrices
            double[,] combinedRotationMatrix = Rotation.CombineMatrices(rotationMatrixZ, Rotation.CombineMatrices(rotationMatrixY, rotationMatrixX));

            foreach (Proportie propertie in properties)
            {
                if (propertie is Movement)
                {
                    propertie.Update();
                    continue;
                }
                if (propertie is Mesh)
                {
                    if (NAH) continue;
                    if (rotatei)
                    {
                        //propertie.zrot += 5;
                        propertie.yrot += 2.4;
                        //propertie.xrot += 3;
                    }
                    else if (floatingyay)
                    {
                        propertie.yrot += 1.2;
                        momentumU += (float)Math.Asin((((double)(System.DateTime.Now.Millisecond)) - 599.5) / 10000);
                        propertie.y += momentumU;
                    }



                    //Console.WriteLine(propertie.nxrot);
                    //propertie.nxrot += 1;
                }
                if (propertie is Camera)
                {
                    //propertie.y += .1;
                    //propertie.yrot += .005;
                }

                double x = propertie.x;
                double y = propertie.y;
                double z = propertie.z;
                double[] rotatedPoint = Rotation.ApplyRotation(combinedRotationMatrix, x, y, z);
                double newX = rotatedPoint[0] + this.x;
                double newY = rotatedPoint[1] + this.y;
                double newZ = rotatedPoint[2] + this.z;
                double newW = rotatedPoint[3] + this.w;
                /*newX += this.x;
                newY += this.y;
                newZ += this.z;*/
                propertie.move(newX, newY, newZ, 1);
                propertie.nxrot = Rotation.WrapAngle(this.xrot + propertie.xrot);
                propertie.nyrot = Rotation.WrapAngle(this.yrot + propertie.yrot);
                propertie.nzrot = Rotation.WrapAngle(this.zrot + propertie.zrot);
                //if(propertie is Camera)
                //{
                //    var watch = new Stopwatch();
                //    watch.Start();
                //    watch.Stop();
                //    Console.WriteLine(watch.ElapsedMilliseconds);
                //    continue;

                //}
                //if(propertie is Camera)
                //{
                //    var watch = new Stopwatch();
                //    watch.Start();
                //    propertie.Update();
                //    watch.Stop();
                //    Console.WriteLine(watch.ElapsedMilliseconds);
                //    continue;
                //}
                //else
                propertie.Update();
                if (propertie is Mesh)
                {
                    if (dddd)
                    {
                        foreach (Polygon polygon in ((Mesh)propertie).points)
                        {
                            Console.WriteLine(polygon.ToString());
                        }

                    }
                }
            }
            dddd = false;
            //Thread.Sleep(3);
        }

    }



    public class SwordDemo : Proportie
    {
        public Item sword;
        public Window win;
        public SwordDemo(double x, double y, double z, double xrot, double yrot, double zrot) : base(x, y, z, xrot, yrot, zrot)
        {

        }
        public override void Start()
        {
            
        }
        public override void Update()
        {
            sword.yrot += 2.3;
            
            if(win.pressed.Contains(80))
            {
                foreach(Proportie propertie in sword.properties)
                {
                    if (propertie is Mesh)
                    {
                        ((Mesh)propertie).hueit(1, 0, 0);
                    }
                }
            }
        }
    }

}