using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEJE
{
    internal class Point
    {
        public double x, y, z, w;
        public int r, g, b;

        public Point(double x, double y, double z, double w, int r, int g, int b)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;  // Homogeneous coordinate
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }
    internal class Line
    {
        public Point p1, p2;
        public Line(Point p1, Point p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }
    }

}
