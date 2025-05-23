﻿using GEJE;
using System.Collections.Generic;
using System;
using System.Linq;
//using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using static System.Windows.Forms.LinkLabel;

public class Camera : Proportie
{
    public double near = 0;
    public double far = 900000;
    public double constant = 10;
    private Window screen;
    private ThreeDSceen scene;
    public double fov = 80; // Field of view in degrees
    private double aspectRatio;
    private double[,] projectionMatrix;
    public bool outline = false;
    public bool fillin = true;
    public bool new_meshes = true;
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
    List<Polygon> pointss = new List<Polygon>();
    List<Polygon> orderedLines = new List<Polygon>();
    double projectedPoint10 = 0;
    double projectedPoint11 = 0;
    double projectedPoint12 = 0;
    double projectedPoint20 = 0;
    double projectedPoint21 = 0;
    double projectedPoint22 = 0;
    double projectedPoint30 = 0;
    double projectedPoint31 = 0;
    double projectedPoint32 = 0;
    double projectedPoint13 = 0;
    double projectedPoint23 = 0;
    double projectedPoint33 = 0;
    List<Item> items = new List<Item>();
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

        if (new_meshes)
        { 
            pointss.Clear();
            double distance = 0;
            foreach (Item item in scene.items)
            {
                items.Add(item);
                distance = (Math.Pow(item.x - nx, 2) + Math.Pow(item.y - ny, 2) + Math.Pow(item.z - nz, 2));
                if (distance < far && distance > near)
                {
                    {
                        foreach (Object propertie in item.properties)
                        {
                            if (propertie.GetType() == typeof(Mesh))
                            {
                                foreach (Polygon point in ((Mesh)propertie).points)
                                {

                                    pointss.Add(point);
                                }
                            }
                        }
                    }
                }
            }
            new_meshes = false;
        }
        else
        {
            pointss.Clear();
            double distance = 0;
            foreach (Item item in items)
            {
                distance = (Math.Pow(item.x - nx, 2) + Math.Pow(item.y - ny, 2) + Math.Pow(item.z - nz, 2));
                if (distance < far && distance > near)
                {
                    {
                        foreach (Object propertie in item.properties)
                        {
                            if (propertie.GetType() == typeof(Mesh))
                            {
                                foreach (Polygon point in ((Mesh)propertie).points)
                                {

                                    pointss.Add(point);
                                }
                            }
                        }
                    }
                }
            }
        }
        // sort lines by the average of the two points
        orderedLines = pointss.OrderByDescending(p => (Math.Abs(((p.p1.x + p.p2.x + p.p3.x - (this.nx * 3)) /3)) + Math.Abs(((p.p1.y + p.p2.y+p.p3.y - (this.ny * 3)) /3)) + Math.Abs(((p.p1.z + p.p2.z+p.p3.z - (this.nz * 3))/3)))).ToList();
        List<List<object>> group = new List<List<object>>();
        foreach (Polygon line in orderedLines)
        {
            
            Point[] points = { line.p1, line.p2 };
            Point point1 = line.p1;
            Point point2 = line.p2;
            double[] point3D1 = { point1.x, point1.y, point1.z, point1.w };
            double[] point3D2 = { point2.x, point2.y, point2.z, point2.w };
            double[] point3D3 = { line.p3.x, line.p3.y, line.p3.z, line.p3.w };
            point3D1[0] -= this.nx;
            point3D1[1] -= this.ny;
            point3D1[2] -= this.nz;
            point3D2[0] -= this.nx;
            point3D2[1] -= this.ny;
            point3D2[2] -= this.nz;
            point3D3[0] -= this.nx;
            point3D3[1] -= this.ny;
            point3D3[2] -= this.nz;
            

            // Apply camera transformations (rotation and translation)
            double[] transformedPoint1 = MatrixMultiply(combinedRotationMatrix, point3D1);
            double[] transformedPoint2 = MatrixMultiply(combinedRotationMatrix, point3D2);
            double[] transformedPoint3 = MatrixMultiply(combinedRotationMatrix, point3D3);
            //Console.WriteLine(transformedPoint1[0]+ " : " + transformedPoint1[1]);
           // Console.WriteLine("____________________________________________________________________\n"+transformedPoint1[0] + " " + transformedPoint1[1] + " " + transformedPoint1[2]);
           // Console.WriteLine(transformedPoint2[0] + " " + transformedPoint2[1] + " " + transformedPoint2[2]+"__________________________________________________________________________\n");

            //if(transformedPoint1 == transformedPoint2 && transformedPoint1 == transformedPoint3 && transformedPoint2 == transformedPoint3)
            //{
            //    continue;
            //}
            // check if the dot product of the two points is less than 0
            if (transformedPoint1[0] * transformedPoint2[0] * transformedPoint3[0] + transformedPoint1[1] * transformedPoint2[1] * transformedPoint3[1] + transformedPoint1[2] * transformedPoint2[2] * transformedPoint3[2] < 0)
            {
                continue;
            }
            if (transformedPoint1[2] < 0 || transformedPoint2[2] < 0 || transformedPoint3[2] < 0)
            {
                continue;
            }










            
            



            // Apply projection matrix
            double[] projectedPoint1 = MatrixMultiply(projectionMatrix, transformedPoint1);
            double[] projectedPoint2 = MatrixMultiply(projectionMatrix, transformedPoint2);
            double[] projectedPoint3 = MatrixMultiply(projectionMatrix, transformedPoint3);

            projectedPoint10 = projectedPoint1[0];
            projectedPoint11 = projectedPoint1[1];
            projectedPoint12 = projectedPoint1[2];
            projectedPoint20 = projectedPoint2[0];
            projectedPoint21 = projectedPoint2[1];
            projectedPoint23 = projectedPoint2[2];
            projectedPoint30 = projectedPoint3[0];
            projectedPoint31 = projectedPoint3[1];
            projectedPoint32 = projectedPoint3[2];
            projectedPoint13 = projectedPoint1[3];
            projectedPoint23 = projectedPoint2[3];
            projectedPoint33 = projectedPoint3[3];
            // Adjust for screen dimensions

            projectedPoint10 /= projectedPoint13;
            projectedPoint11 /= projectedPoint13;
            projectedPoint12 /= projectedPoint13;
            projectedPoint20 /= projectedPoint23;
            projectedPoint21 /= projectedPoint23;
            projectedPoint22 /= projectedPoint23;
            projectedPoint30 /= projectedPoint33;
            projectedPoint31 /= projectedPoint33;
            projectedPoint32 /= projectedPoint33;
            
            
            projectedPoint10 = (projectedPoint10 + 1) * (screen.Ethwidth / 2);
            projectedPoint11 = (1 - projectedPoint11) * (screen.Ethheight / 2);
            projectedPoint20 = (projectedPoint20 + 1) * (screen.Ethwidth / 2);
            projectedPoint21 = (1 - projectedPoint21) * (screen.Ethheight / 2);
            projectedPoint30 = (projectedPoint30 + 1) * (screen.Ethwidth / 2);
            projectedPoint31 = (1 - projectedPoint31) * (screen.Ethheight / 2);
            //if out of bounds, skip
            int outOfBoundsCount = 0;

            // Check each condition and count the number of true conditions
            if (projectedPoint10 < -10 || projectedPoint10 > screen.Ethwidth + 10 || projectedPoint11 < -10 || projectedPoint11 > screen.Ethheight + 10)
            {
                outOfBoundsCount++;
            }

            if (projectedPoint20 < -10 || projectedPoint20 > screen.Ethwidth + 10 || projectedPoint21 < -10 || projectedPoint21 > screen.Ethheight + 10)
            {
                outOfBoundsCount++;
            }

            if (projectedPoint30 < -10 || projectedPoint30 > screen.Ethwidth + 10 || projectedPoint31 < -10 || projectedPoint31 > screen.Ethheight + 10)
            {
                outOfBoundsCount++;
            }

            // If at least two out of three conditions are true
            if (outOfBoundsCount >= 3)
            {
                continue;
            }

            // 
            //Console.WriteLine(transformedPoint[0] + " " + transformedPoint[1] + " " + transformedPoint[2]);
            //Console.WriteLine(projectedPoint1[0] + " " + projectedPoint1[1] + " " + projectedPoint2[0]+ " " + projectedPoint2[1]);
            // Draw the line on the screen
            group.Add(new List<object>() { projectedPoint10,projectedPoint11, projectedPoint20,projectedPoint21, projectedPoint30,projectedPoint31, new byte[] { line.p1.r, line.p1.g, line.p1.b } });
            //if (fillin)
            //    DrawFilledPolygonOnScreen(screen, projectedPoint1, projectedPoint2, projectedPoint3,new int[] { (int)line.p1.r, (int)line.p1.g, (int)line.p1.b });

            //if (outline)
            //{
            //    DrawLineOnScreen(screen, projectedPoint1, projectedPoint2, new int[] { 0, 0, 0 });
            //    DrawLineOnScreen(screen, projectedPoint2, projectedPoint3, new int[] { 0, 0, 0 });
            //    DrawLineOnScreen(screen, projectedPoint3, projectedPoint1, new int[] { 0, 0, 0 });
            //}


            
            //Console.WriteLine(projectedPoint1[0] + " , " + projectedPoint1[1] + " | " + projectedPoint2[0] + " , " + projectedPoint2[1]);

        }
        //group.Reverse();
        foreach(List<object> list in group)
        {
            if (fillin)
                DrawFilledPolygonOnScreen(screen, (double)list[0], (double)list[1], (double)list[2], (double)list[3],(double)list[4],(double)list[5], (byte[])list[6]);

            if (outline)
            {
                DrawLineOnScreen(screen, (double)list[0], (double)list[1],(double)list[2],(double)list[3], new byte[] { 0, 0, 0 });
                DrawLineOnScreen(screen, (double)list[2],(double)list[3], (double)list[4],(double)list[5], new byte[] { 0, 0, 0 });
                DrawLineOnScreen(screen, (double)list[4],(double)list[5], (double)list[0],(double)list[1], new byte[] { 0, 0, 0 });
            }
        }
        screen.UpdateLoop();

        //screen.Draw();








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
    void DrawLineOnScreen(Window screen, double point10, double point11, double point20, double point21, byte[] rgb)
    {
        int x1 = (int)Math.Round(point10);
        int y1 = (int)Math.Round(point11);
        int x2 = (int)Math.Round(point20);
        int y2 = (int)Math.Round(point21);

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
    List<int[]> sortedPoints = new List<int[]>();
    public void DrawFilledPolygonOnScreen(Window screen, double point10,double point11, double point20,double point21, double point30,double point31, byte[] rgb)
    {
        sortedPoints = new List<int[]> { new int[] { (int)point10, (int)point11 }, new int[] { (int)point20, (int)point21 }, new int[] { (int)point30, (int)point31 } };
        sortedPoints.Sort((p1, p2) => p1[1].CompareTo(p2[1]));
        int[] topPoint = sortedPoints[0];
        int[] middlePoint = sortedPoints[1];
        int[] bottomPoint = sortedPoints[2];
        double segmentHeight = middlePoint[1] - topPoint[1] + 1;

        double totalHeight = bottomPoint[1] - topPoint[1];
        double alpha;
        double alpha2;
        int startX;
        int endX;
        if (totalHeight == 0) return;
        if (middlePoint[1] - topPoint[1] + 1 != 0)
        {

            for (int y = topPoint[1]; y <= middlePoint[1]; y++)
            {
                if (y < 0 && middlePoint[1] > 0) y = 0;
                else if (y < 0) break;
                else if (y > screen.Ethheight) break;

                alpha = (y - topPoint[1]) / totalHeight;
                alpha2 = (y - topPoint[1]) / segmentHeight;
                startX = (int)Math.Round((1 - alpha) * topPoint[0] + alpha * bottomPoint[0]);
                endX = (int)Math.Round((1 - alpha2) * topPoint[0] + alpha2 * middlePoint[0]);
                DrawHorizontalLine(screen, startX, endX, y, rgb);
            }
        }
        segmentHeight = bottomPoint[1] - middlePoint[1] + 1;
        if (bottomPoint[1] - middlePoint[1] + 1 != 0)
        {
            for (int y = middlePoint[1] + 1; y <= bottomPoint[1]; y++)
            {
                if (y < 0 && middlePoint[1] > 0) y = 0;
                else if (y < 0) break;
                else if (y > screen.Ethheight) break;
                alpha = (y - topPoint[1]) / totalHeight;
                alpha2 = (y - middlePoint[1]) / segmentHeight;
                startX = (int)Math.Round((1 - alpha) * topPoint[0] + alpha * bottomPoint[0]);
                endX = (int)Math.Round((1 - alpha2) * middlePoint[0] + alpha2 * bottomPoint[0]);
                DrawHorizontalLine(screen, startX, endX, y, rgb);
            }
        }
    }


    private void DrawHorizontalLine(Window screen, int x1, int x2, int y, byte[] rgb)
    {
        if (x1 > x2)
        {
            int temp = x1;
            x1 = x2;
            x2 = temp;
        }
        for (int x = x1; x <= x2; x++)
        {
            if (x < 0 && x2 > 0) x = 0;
            else if (x < 0) break;
            else if(x > screen.Ethwidth) break;
            screen.PlaceColor(x, y, rgb[0], rgb[1], rgb[2]);
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

