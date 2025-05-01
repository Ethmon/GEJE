using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace GEJE
{
    public class Program
    {
        public static void Main()
        {
            ThreeDSceen sceen = new ThreeDSceen(100, 100, 100);

            ////item.add_propertie(box3);
            ////Mesh box3 = new Mesh(@"C:\Users\ethan\Downloads\Box.json", 0, 8, 10, 90, -90, 01);
            //item.add_propertie(box2);
            //item1.add_propertie(box21);
            //item3.add_propertie(box23);
            ////item13.add_propertie(box213);
            //item4.add_propertie(box24);
            //item14.add_propertie(box214);
            ////.add_propertie(box3);
            //sceen.add_item(item);
            //sceen.add_item(item1);
            //sceen.add_item(item3);
            ////sceen.add_item(item13);
            //sceen.add_item(item4);
            //sceen.add_item(item14);
            //Movement movement = new Movement(0, 0, 0, 0, 0, 0, item);

            //Item box = new Item(0, 0, 075, 0, 0, 0);
            //Mesh boxmesh = new Mesh("C:\\Users\\ethan\\OneDrive\\Desktop\\GEJE_Images\\Cube.json", 0, 0, 0, 0, 0, 0);
            //box.add_propertie(boxmesh);
            //sceen.add_item(box);

            //for(int i = 0, j = 0; i < 10; i++, j++)
            //{
            //    Item item = new Item(i * 10, 0, j * 20, 0, 0, 0);
            //    Mesh box2 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\Cube.json", 0, 0, 0, 0, 0, 0);
            //    item.add_propertie(box2);
            //    sceen.add_item(item);
            //}
            //for (int i = 0; i < 50; i++)
            //{
            // other files { @"C:\Users\ethan\3dassets\Box.json" , @"C:\Users\ethan\Downloads\JEGE_images\Poly.JSON" }
            Random random = new Random();
            for (int i = 0; i < 11; i++)
            {
                for (int k = 0; k < 11; k++)
                {
                    for (int v = 1; v < 2; v++)
                    {
                        double d = random.NextDouble();
                        Item item = new Item(-20 + 20 * i, -0 + v*40, -10 + k * 20, 90, (d > .75) ? 0 : (d>.5)?90:(d>.25)?180:270  , 0);
                        Mesh box2 = new Mesh("Models\\Ground1.JSON", 0, 0, 0, 0, 0, 0);
                        int greenchange = random.Next(-30, 30);
                        box2.hueit(0, greenchange, 0);
                        //Mesh box3 = new Mesh(@"C:\Users\ethan\Downloads\JEGE_images\Poly.JSON", 0, 0, 0, 0, 0, 180);
                        //Mesh box4 = new Mesh(@"C:\Users\ethan\Downloads\JEGE_images\Poly.JSON", 0, 0, 0, 0, 180, 0);
                        //Mesh box5 = new Mesh(@"C:\Users\ethan\Downloads\JEGE_images\Poly.JSON", 0, 0, 0, 0, 180, 180);
                        //item.add_propertie(box3);
                        item.add_propertie(box2);
                        //item.add_propertie(box4);
                        //item.add_propertie(box5);
                        sceen.add_item(item);



                        Item under = new Item(-20 + 20 * i, 50, -10 + k * 20, 90, (d > .75) ? 0 : (d > .5) ? 90 : (d > .25) ? 180 : 270, 0);
                        Mesh box3 = new Mesh("Models\\BlackSlate.JSON", 0, 0, 0, 0, 0, 0);
                        under.add_propertie(box3);
                        sceen.add_item(under);
                    }
                }
            }
            for(int i = 1; i < 10 ; i++)
            { 
            
                for (int k = 1; k < 10; k++)
                {
                    for (int ppp = 0; ppp < 5; ppp++)
                    {
                        if (((i ==1||i==9)&&ppp<4)||((i==5)&&ppp<2&&k>3&&k<6)||((k==9)))
                        {
                            double d = random.NextDouble();
                            Item under = new Item(-20 + 20 * i, 30 - (ppp * 20), -10 + k * 20, 90, (d > .75) ? 0 : (d > .5) ? 90 : (d > .25) ? 180 : 270, 0);
                            Mesh box3 = new Mesh("Models\\Wall1.JSON", 0, 0, 0, 0, 0, 0);
                            int red = random.Next(-50, 50);
                            int green = random.Next(-50, 50);
                            int blue = random.Next(-50, 50);
                            box3.hueit(red, green, blue);
                            under.add_propertie(box3);
                            sceen.add_item(under);
                        }
                    }
                }
            }

            //}

            Item.rotatei = false;
            Item.floatingyay = false;
            //Console.WriteLine(box2.ToString());
            Item camera = new Item(0, 0, -100, 0, 0, 0);
            Window win = new Window(600, 400,2,2);
            Camera cam = new Camera(0,0, 0, 0, 0, 0, sceen, win,1);
            cam.outline = false;
            cam.fillin = true;
            
            Movement cam_movement = new Movement(0, 0, 0, 0, 0, 0, camera,5);
            camera.add_propertie(cam_movement);
            camera.add_propertie(cam);
            //camera.add_propertie(cam_movement);
            sceen.add_item(camera);
            win.cam = cam;
            sceen.Start_scene();
            
            win.Run();
        }
    }
}
