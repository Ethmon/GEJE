using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text.Json;

namespace GEJE
{
    public class Mesh : Proportie
    {
        public List<Polygon> points = new List<Polygon>();
        public List<Polygon> oldpoints = new List<Polygon>();
        public string getPath()
        {
            string aa = System.IO.Directory.GetCurrentDirectory();
            aa = aa.Replace("GEJE\\bin\\Debug", "GEJE\\");
            Console.Write(aa);
            return aa;
        }
        public Mesh(string filename, double x, double y, double z, double xrot, double yrot, double zrot) : base(x, y, z, xrot, yrot, zrot)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.xrot = xrot;
            this.yrot = yrot;
            this.zrot = zrot;

            // Read a json file with a list of lists with 3 doubles and 3 ints
            string jsonString = File.ReadAllText(getPath()+"\\"+filename);
            List<List<double>> data = JsonSerializer.Deserialize<List<List<double>>>(jsonString);
            int count = 0;
            Point point1 = null;
            Point point2 = null;
            foreach (List<double> point in data)
            {
                if (count == 2)
                {
                    oldpoints.Add(new Polygon(new Point((point[0] * 1), (point[1] * 1), (point[2] * 1), 1, (byte)(point[3]), (byte)(point[4]), (byte)(point[5])), point1, point2));
                    count = 0;
                }
                else if (count == 0)
                { point1 = new Point((point[0] * 1), (point[1] * 1), (point[2] * 1), 1, (byte)(point[3]), (byte)(point[4]), (byte)(point[5])); count++; }
                else
                {
                    point2 = new Point((point[0] * 1), (point[1] * 1), (point[2] * 1), 1, (byte)(point[3]), (byte)(point[4]), (byte)(point[5]));
                    count++;
                }
            }
            
        }
        public void hueit(int r, int g, int b)
        {
            foreach (Polygon point in oldpoints)
            {
                point.p1.r = (byte)((point.p1.r+r>255)?255:(point.p1.r+r<0)?0:point.p1.r+r);
                point.p1.g = (byte)((point.p1.g + g > 255) ? 255 : (point.p1.g + g < 0) ? 0 : point.p1.g + g);
                point.p1.b = (byte)((point.p1.b + b > 255) ? 255 : (point.p1.b + b < 0) ? 0 : point.p1.b + b);
                point.p2.r = (byte)((point.p2.r + r > 255) ? 255 : (point.p2.r + r < 0) ? 0 : point.p2.r + r);
                point.p2.g = (byte)((point.p2.g + g > 255) ? 255 : (point.p2.g + g < 0) ? 0 : point.p2.g + g);
                point.p2.b = (byte)((point.p2.b + b > 255) ? 255 : (point.p2.b + b < 0) ? 0 : point.p2.b + b);
                point.p3.r = (byte)((point.p3.r + r > 255) ? 255 : (point.p3.r + r < 0) ? 0 : point.p3.r + r);
                point.p3.g = (byte)((point.p3.g + g > 255) ? 255 : (point.p3.g + g < 0) ? 0 : point.p3.g + g);
                point.p3.b = (byte)((point.p3.b + b > 255) ? 255 : (point.p3.b + b < 0) ? 0 : point.p3.b + b);
            }
        }
        public override string ToString()
        {
            string answer = "";
            if (oldpoints != null) { return answer; }
            else
            {
                foreach (Polygon point in oldpoints)
                {
                    answer += point.ToString() + "\n";
                    Console.WriteLine("hello");
                }
            }
            return answer;
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

            foreach (Polygon Lines in oldpoints)
            {
                Point point1 = Lines.p1;
                Point point2 = Lines.p2;
                for (int i = 0; i <= 2; i++)
                {
                    Point point = null;
                    if (i == 0) { point = Lines.p1; } else if(i==1) { point = Lines.p2; } else { point = Lines.p3; }
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
                    else if (i == 1)
                        point2 = new Point(newX, newY, newZ, 1, point.r, point.g, point.b);
                    else
                        points.Add(new Polygon(point1, point2, new Point(newX, newY, newZ, 1, point.r, point.g, point.b)));

                }


            }
        }
    }
}
