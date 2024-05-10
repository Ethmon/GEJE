using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace GEJE
{
    public class Mesh : Proportie
    {
        public List<Line> points = new List<Line>();
        public List<Line> oldpoints = new List<Line>();

        public Mesh(string filename, double x, double y, double z, double xrot, double yrot, double zrot) : base(x, y, z, xrot, yrot, zrot)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.xrot = xrot;
            this.yrot = yrot;
            this.zrot = zrot;

            // Read a json file with a list of lists with 3 doubles and 3 ints
            string jsonString = File.ReadAllText(filename);
            List<List<double>> data = JsonSerializer.Deserialize<List<List<double>>>(jsonString);
            Point point1 = null;
            foreach (List<double> point in data)
            {
                if (point1 != null)
                {
                    oldpoints.Add(new Line(new Point((point[0] * 2), (point[1] * 2), (point[2] * 2), 1, (int)(point[3]), (int)(point[4]), (int)(point[5])), point1));
                    point1 = null;
                }
                else 
                    point1 = new Point((point[0] * 2), (point[1] * 2), (point[2] * 2), 1, (int)(point[3]), (int)(point[4]), (int)(point[5]));
            }
        }

        public override void Update()
        {
            this.nxrot = Rotation.WrapAngle(this.nxrot);
            this.nyrot = Rotation.WrapAngle(this.nyrot);
            this.nzrot = Rotation.WrapAngle(this.nzrot);
            double xRotRad = this.nxrot * (Math.PI / 180);
            double yRotRad = this.nyrot * (Math.PI / 180);
            double zRotRad = this.nzrot * (Math.PI / 180);
            double[,] rotationMatrixX = Rotation.GetRotationMatrixX(xRotRad);
            double[,] rotationMatrixY = Rotation.GetRotationMatrixY(yRotRad);
            double[,] rotationMatrixZ = Rotation.GetRotationMatrixZ(zRotRad);
            // Combine rotation matrices
            double[,] combinedRotationMatrix = Rotation.CombineMatrices(rotationMatrixZ, Rotation.CombineMatrices(rotationMatrixY, rotationMatrixX));

            points.ForEach(p => { p = null; });
            points.Clear();

            foreach (Line Lines in oldpoints)
            {
                Point point1 = Lines.p1;
                for (int i = 0; i < 2; i++)
                {
                    Point point = null;
                    if (i == 1) { point = Lines.p2; } else { point = point1; }
                    double x = point.x;
                    double y = point.y;
                    double z = point.z;
                    double w = point.w;

                    double[] rotatedPoint = Rotation.ApplyRotation(combinedRotationMatrix, x, y, z);
                    double newX = rotatedPoint[0] + this.nx;
                    double newY = rotatedPoint[1] + this.ny;
                    double newZ = rotatedPoint[2] + this.nz;

                    // Translate back to the original position
                    /*newX += this.nx;
                    newY += this.ny;
                    newZ += this.nz;*/
                    if(i==0) point1 = new Point(newX, newY, newZ, 1, point.r, point.g, point.b);
                    else
                        points.Add(new Line(point1, new Point(newX, newY, newZ, 1, point.r, point.g, point.b)));

                }

            }
        }
    }
}
