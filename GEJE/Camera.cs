using GEJE;
using System.Collections.Generic;
using System;
using System.Linq;
//using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

public class Camera : Proportie
{
    public double near = 0.1;
    public double far = 50000;
    public double constant = 110;
    private Window screen;
    private ThreeDSceen scene;
    public double fov = 40; // Field of view in degrees
    private double aspectRatio;
    private double[,] projectionMatrix;

    public Camera(double x, double y, double z, double xrot, double yrot, double zrot, ThreeDSceen scene, Window screen, double aspectRatio) : base(x, y, z, xrot, yrot, zrot)
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
        this.nxrot = xrot;
        this.nyrot = yrot;
        this.nzrot = zrot;
        this.scene = scene;
        this.screen = screen;
        this.aspectRatio = aspectRatio;
        this.projectionMatrix = CalculateProjectionMatrix(fov, aspectRatio, near, far);
    }
     
    public override void Update()
    {
        //DrawLineOnScreen(screen, new double[] { 0, 0, 10, 10, 10 }, new double[] { 100, 100, 10, 10, 10 }, new int[] { 0, 0, 0 });
        // get all items in the scene that the camera can see
        this.nxrot = Rotation.WrapAngle(this.nxrot);
        this.nyrot = Rotation.WrapAngle(this.nyrot);
        this.nzrot = Rotation.WrapAngle(this.nzrot);
        double xRotRad = this.nxrot * (Math.PI / 180);
        double yRotRad = this.nyrot * (Math.PI / 180);
        double zRotRad = this.nzrot * (Math.PI / 180);
        double[,] rotationMatrixX = Rotation.GetRotationMatrixX(xRotRad);
        double[,] rotationMatrixY = Rotation.GetRotationMatrixY(yRotRad);
        double[,] rotationMatrixZ = Rotation.GetRotationMatrixZ(zRotRad);
        double[,] combinedRotationMatrix = Rotation.CombineMatrices(rotationMatrixZ, Rotation.CombineMatrices(rotationMatrixX, rotationMatrixY));


        List<Line> pointss = new List<Line>();

        foreach (Item item in scene.items)
        {
            // check if the item is in range for the camera
            double distance = Math.Sqrt(Math.Pow(item.x - nx, 2) + Math.Pow(item.y - ny, 2) + Math.Pow(item.z - nz, 2));
            //Console.WriteLine(distance);
            if (distance < far && distance > near)
            {

                /*double newx = item.x - this.x;
                double newy = item.y - this.y;
                double newz = item.z - this.z;
                double screenX = newx * Math.Cos(yRotRad) - newz * Math.Sin(yRotRad);
                double screenY = newx * Math.Sin(yRotRad) + newy * Math.Cos(xRotRad) + newz * Math.Sin(xRotRad);
                double screenZ = newx * Math.Cos(yRotRad) + newy * Math.Sin(xRotRad) + newz * Math.Cos(xRotRad);*/

                //if (screenX > -fovW && screenX < fovW && screenY > -fovH && screenY < fovH)
                {
                    foreach (Object propertie in item.properties)
                    {
                        if (propertie.GetType() == typeof(Mesh))
                        {
                            foreach (Line point in ((Mesh)propertie).points)
                            {

                                pointss.Add(point);
                            }
                        }
                    }
                }
            }
        }
        // sort lines by the average of the two points
        List<Line> orderedLines = pointss.OrderByDescending(p => (((p.p1.x + p.p2.x)/2-this.nx) + ((p.p1.y + p.p2.y)/2-this.ny) + ((p.p1.z + p.p2.z)/2-this.nz))).ToList();

        foreach (Line line in orderedLines)
        {
            
            Point[] points = { line.p1, line.p2 };
            Point point1 = line.p1;
            Point point2 = line.p2;
            double[] point3D1 = { point1.x, point1.y, point1.z, point1.w };
            double[] point3D2 = { point2.x, point2.y, point2.z, point2.w };
            point3D1[0] -= this.nx;
            point3D1[1] -= this.ny;
            point3D1[2] -= this.nz;
            point3D2[0] -= this.nx;
            point3D2[1] -= this.ny;
            point3D2[2] -= this.nz;
            

            // Apply camera transformations (rotation and translation)
            double[] transformedPoint1 = MatrixMultiply(combinedRotationMatrix, point3D1);
            double[] transformedPoint2 = MatrixMultiply(combinedRotationMatrix, point3D2);
            //Console.WriteLine(transformedPoint1[0]+ " : " + transformedPoint1[1]);
           // Console.WriteLine("____________________________________________________________________\n"+transformedPoint1[0] + " " + transformedPoint1[1] + " " + transformedPoint1[2]);
           // Console.WriteLine(transformedPoint2[0] + " " + transformedPoint2[1] + " " + transformedPoint2[2]+"__________________________________________________________________________\n");

            if(transformedPoint1 == transformedPoint2)
            {
                continue;
            }
            // check if the dot product of the two points is less than 0
            if (transformedPoint1[0] * transformedPoint2[0] + transformedPoint1[1] * transformedPoint2[1] + transformedPoint1[2] * transformedPoint2[2] < 0)
            {
                continue;
            }
            //check if just point 1 is behind the camera
            if (transformedPoint1[2] < 0)
            {
                continue;
            }
            //check if just point 2 is behind the camera
            if (transformedPoint2[2] < 0)
            {
                continue;
            }














            // Apply projection matrix
            double[] projectedPoint1 = MatrixMultiply(projectionMatrix, transformedPoint1);
            double[] projectedPoint2 = MatrixMultiply(projectionMatrix, transformedPoint2);

            // Adjust for screen dimensions



            projectedPoint1[0] = (projectedPoint1[0] + 1) + (screen.Ethwidth / 2);
            projectedPoint1[1] = (1 - projectedPoint1[1]) + (screen.Ethheight / 2);
            projectedPoint2[0] = (projectedPoint2[0] + 1) + (screen.Ethwidth / 2);
            projectedPoint2[1] = (1 - projectedPoint2[1]) + (screen.Ethheight / 2);
            // 
            //Console.WriteLine(transformedPoint[0] + " " + transformedPoint[1] + " " + transformedPoint[2]);
            //Console.WriteLine(projectedPoint1[0] + " " + projectedPoint1[1] + " " + projectedPoint2[0]+ " " + projectedPoint2[1]);
            // Draw the line on the screen
            DrawLineOnScreen(screen, projectedPoint1, projectedPoint2,
                new int[] { (int)line.p1.r, (int)line.p1.g, (int)line.p1.b });
            //Console.WriteLine(projectedPoint1[0] + " , " + projectedPoint1[1] + " | " + projectedPoint2[0] + " , " + projectedPoint2[1]);
            
        }








        /*foreach (Line line in orderedLines)
        {
            double[] pointoneScreenCords = new double[2];
            double[] pointtwoScreenCords = new double[2];
            int[] color = new int[3];





            // THIS IS WHERE THE NEW CAMERA CODE SHOULD GO




            /*for(int i = 0; i < 2; i++)
            {
                Point point = (i == 0) ? line.p1 : line.p2;
                double[] point3D = { point.x, point.y, point.z, point.w };
                point3D[0] -= this.nx;
                point3D[1] -= this.ny;
                point3D[2] -= this.nz;
                double[] poity = Rotation.ApplyRotation(combinedRotationMatrix, point3D[0], point3D[1], point3D[2]);
                poity.Append(point3D[3]);
                double[] projectedPoint = MatrixMultiply(projectionMatrix, poity);
                double screenX = projectedPoint[0] / projectedPoint[3];
                double screenY = projectedPoint[1] / projectedPoint[3];
                screenX = (screenX + 1) * 0.5 * screen.Width;
                screenY = (1 - screenY) * 0.5 * screen.Height;
                Console.
                if (i == 0)
                {
                    pointoneScreenCords[0] = screenX;
                    pointoneScreenCords[1] = screenY;
                    color[0] = point.r;
                    color[1] = point.g;
                    color[2] = point.b;
                }
                else
                {
                    pointtwoScreenCords[0] = screenX;
                    pointtwoScreenCords[1] = screenY;
                }

            }*/

        /*for (int i = 0; i < 2; i++)
        {
            Point point = (i == 0) ? line.p1 : line.p2;
            double[] point3D = { point.x, point.y, point.z, point.w };
            point3D[0] -= this.nx;
            point3D[1] -= this.ny;
            point3D[2] -= this.nz;

            // Apply camera transformations (rotation and translation)
            double[] transformedPoint = MatrixMultiply(combinedRotationMatrix, point3D);

            // Apply projection matrix
            double[] projectedPoint = MatrixMultiply(projectionMatrix, transformedPoint);

            // Normalize homogeneous coordinates
            double screenX = projectedPoint[0] / projectedPoint[3];
            double screenY = projectedPoint[1] / projectedPoint[3];

            // Adjust for screen dimensions
            screenX = (screenX + 1) * 0.5 * screen.Width;
            screenY = (1 - screenY) * 0.5 * screen.Height;
            Console.WriteLine(screenX + " " + screenY);
            if (i == 0)
            {
                pointoneScreenCords[0] = screenX;
                pointoneScreenCords[1] = screenY;
                color[0] = point.r;
                color[1] = point.g;
                color[2] = point.b;
            }
            else
            {
                pointtwoScreenCords[0] = screenX;
                pointtwoScreenCords[1] = screenY;
            }
        }

        // Draw the line on the screen*/

        //DrawLineOnScreen(screen, pointoneScreenCords, pointtwoScreenCords, color);


        //screen.Update();

        // Sort points by distance (farthest to closest)
        //List<Point> orderedPoints = points.OrderByDescending(p => Math.Sqrt(Math.Pow(p.x - x, 2) + Math.Pow(p.y - y, 2) + Math.Pow(p.z - z, 2))).ToList();
        // ...
        //double scaleFactors = screen.Ethwidth / (2 * Math.Tan(((fovW*(Math.PI / 180)) / 2)));
        /*
        foreach (Point point in orderedPoints)
        {
            double relativex = point.x - this.nx;
            double relativey = point.y - this.ny;
            double relativez = point.z - this.nz;
            double w = 1.0;

            double[] rotatedPoint = Rotation.ApplyRotation(combinedRotationMatrix, relativex, relativey, relativez);
            double newX = rotatedPoint[0];
            double newY = rotatedPoint[1];
            double newZ = rotatedPoint[2];
            //if (newY < 0) { continue; }
            double d1 = 0;
            //if ((newX < 0)) { 
                d1 = (newY-1) / (-1 / 1.6); 
            //}
            //else { d1 = (newZ - 1) / (1 / 1.6); }
            double d2 = screen.Ethwidth/2;
            //if(d1<newX){continue;}
            double dispX = (newX*d2) / d1;
            double r1 = 0;
            if (newZ < 0) { 
                r1 = (newY - 1) / (-1 / 1.8); 
            }
            //else { r1 = (newY - 1) / (1 / 1.5); }
            double r2 = screen.Ethheight/2;
            //if (r1 < newZ) { continue; }
            double dispY = (newZ*r2) / d1;
            dispX += screen.Ethwidth / 2;
            dispY += screen.Ethheight / 2;
            //double dispx = ((rotatedX) * screen.Ethwidth) + rotatedY * screen.
            
            screen.PlaceColor((int)dispX, (int)dispY, point.r, point.g, point.b);

            /*if (Math.Atan(rotatedX / rotatedZ) > Math.PI)
            {
                continue;
            }*/

        // Perspective projection
        /*double screenX = rotatedX / (rotatedZ == 0 ? 1 : rotatedZ);
        double screenY = rotatedY / (rotatedZ == 0 ? 1 : rotatedZ);
        double screenZ = rotatedZ / (rotatedY == 0 ? 1 : rotatedY);
        double distance = Math.Sqrt(Math.Pow((rotatedX), 2) + Math.Pow((rotatedY), 2) + Math.Pow((rotatedZ), 2));

        if (!((Math.Atan((Math.Sqrt((rotatedX * rotatedX) + (rotatedY * rotatedY))) / rotatedZ) > Math.Tan(((fovH / 2)*-1) * Math.PI / 180) && (Math.Atan((Math.Sqrt((rotatedX * rotatedX) + (rotatedY * rotatedY))) / rotatedZ) < Math.Tan((fovH / 2) * Math.PI / 180)) && (Math.Atan(rotatedX / rotatedY) < Math.Tan((fovW / 2 ) * Math.PI / 180) && Math.Atan(rotatedX / rotatedY) > Math.Tan((fovW/2)*-1 * Math.PI / 180)))))
        {
            continue;
        }

        // Apply scaling and translate to screen coordinates
        double scaleFactor = scaleFactors / (distance + 1.0);
        double projectedX = screen.Ethwidth / 2 + screenX * scaleFactor;
        double projectedY = screen.Ethheight / 2 + screenY * scaleFactor;
        double projectedZ = screen.Ethheight / 2 + screenZ * scaleFactor;
        int size = (int)(constant / (distance + 1.0) + 0.5);
        Console.WriteLine(rotatedX + " " + rotatedY + " " + rotatedZ +" :" + projectedX + " " + projectedY);
        if(size == 0)
        {
            continue;
        }
        screen.PlaceColor((int)projectedX, (int)projectedY, point.r, point.g, point.b);
        /*for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int screenPosX = (int)(projectedX + i);
                int screenPosY = (int)(projectedY + j);

                if (projectedX >= 0 && projectedX < screen.Ethwidth && projectedY >= 0 && projectedY < screen.Ethheight)
                {
                    screen.PlaceColor((int)screenPosX, (int)screenPosY, point.r, point.g, point.b);
                }
            }
        }


    }
        */
        // ...
        
        //screen.update();
        //Console.WriteLine(this.nxrot + " " + this.nyrot + " " + this.nzrot);

    }
    void DrawLineOnScreen(Window screen, double[] point1, double[] point2,int[] rgb)
    {
        int x1 = (int)Math.Round(point1[0]);
        int y1 = (int)Math.Round(point1[1]);
        int x2 = (int)Math.Round(point2[0]);
        int y2 = (int)Math.Round(point2[1]);

        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = (x1 < x2) ? 1 : -1;
        int sy = (y1 < y2) ? 1 : -1;
        int err = dx - dy;
        //Console.WriteLine(x1 + " " + y1);
        while (true)
        {
            screen.PlaceColor(x1, y1, rgb[0], rgb[1], rgb[2]);
            if (x1 == x2 && y1 == y2)
                break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
            
        }
        
    }
    private double[,] CalculateProjectionMatrix(double fov, double aspectRatio, double near, double far)
    {
        double fovRad = fov * (Math.PI / 180);
        double fovY = 1 / Math.Tan(fovRad / 2);
        double fovX = fovY / aspectRatio;

        double[,] projectionMatrix = new double[,]
        {
        { fovX, 0, 0, 0 },
        { 0, fovY, 0, 0 },
        { 0, 0, (far + near) / (near - far), (2 * far * near) / (near - far) },
        { 0, 0, -1, 0 }
        };

        return projectionMatrix;
    }
    private double[] MatrixMultiply(double[,] matrix, double[] vector)
    {
        int rowsA = matrix.GetLength(0);
        int colsA = matrix.GetLength(1);
        int rowsB = vector.GetLength(0);

        if (colsA != rowsB)
        {
            throw new ArgumentException("Matrix dimensions are not compatible for multiplication.");
        }

        double[] result = new double[rowsA];

        for (int i = 0; i < rowsA; i++)
        {
            double sum = 0;
            for (int j = 0; j < colsA; j++)
            {
                sum += matrix[i, j] * vector[j];
            }
            result[i] = sum;
        }

        return result;
    }


}

