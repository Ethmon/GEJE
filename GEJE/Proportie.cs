using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEJE
{
    public class Proportie
    {
        public double x, y, z , w;
        public double xrot, yrot, zrot , wrot;
        public double nx, ny, nz, nw;
        public double nyrot, nzrot, nxrot;
        public Proportie(double x, double y, double z, double xrot, double yrot, double zrot)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.xrot = xrot;
            this.yrot = yrot;
            this.zrot = zrot;
            this.nx = x;
            this.ny = y;
            this.nz = z;
            this.nyrot = yrot;
            this.nzrot = zrot;
            this.nxrot = xrot;
        }
        public void move(double x, double y, double z, double w) // Add w parameter
        {
            this.nx = x;
            this.ny = y;
            this.nz = z;
            this.nw = w; // Set w
        }

        public void rotate(double x, double y, double z, double w) // Add w parameter
        {
            this.nxrot = x;
            this.nyrot = y;
            this.nzrot = z;
            //this.wrot = w; // Set wrot
        }
        public virtual void Start()
        {

        }
        public virtual void Update()
        {

        }
    }
}
