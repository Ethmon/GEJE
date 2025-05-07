using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEJE
{
    public class Point
    {
        public double x, y, z, w;
        public byte r, g, b;

        public Point(double x, double y, double z, double w, byte r, byte g, byte b)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;  // Homogeneous coordinate
            this.r = r;
            this.g = g;
            this.b = b;
        }
        public override string ToString()
        {
            return x + ", " + y + ", " + z;
        }
    }
    public class Line
    {
        public Point p1, p2;
        public Line(Point p1, Point p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }
    }
    public class Polygon
    {
        public Point p1, p2, p3;
        public Polygon(Point p1, Point p2, Point p3)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }
        public override string ToString()
        {
            return p1.ToString() + " | " + p2.ToString() + " | " + p3.ToString();
        }
    }

}
