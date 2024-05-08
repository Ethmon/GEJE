using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GEJE
{
    internal class Item
    {
        public double x, y, z, w;
        public double xrot, yrot, zrot , wrot;
        public List<Proportie> properties = new List<Proportie>();
        public Item(double x, double y, double z,double xrot,double yrot, double zrot)
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
                if(propertie is Movement)
                {
                    propertie.Update();
                    continue;
                }
                if(propertie is Mesh)
                {
                    //propertie.zrot+=.005;
                    //propertie.yrot+=.0024;
                    //propertie.xrot+=.003;
                    //Console.WriteLine(propertie.nxrot);
                    //propertie.nxrot += 1;
                }
                if(propertie is Camera)
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
                propertie.nxrot = Rotation.WrapAngle(this.xrot+propertie.xrot);
                propertie.nyrot = Rotation.WrapAngle(this.yrot+propertie.yrot);
                propertie.nzrot = Rotation.WrapAngle(this.zrot+propertie.zrot);
                
                propertie.Update();
            }
            //Thread.Sleep(3);
        }

    }
}
