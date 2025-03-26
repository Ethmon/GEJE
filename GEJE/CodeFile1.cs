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
            for (int i = 1; i < 2; i++)
            {
                Item item = new Item(-240 + 240*i, 0, -10 + i* 20 , 0, 0, 0);
                Mesh box2 = new Mesh("Models\\FullBox.JSON", 0, 0, 0, 0, 0, 0);
                //Mesh box3 = new Mesh(@"C:\Users\ethan\Downloads\JEGE_images\Poly.JSON", 0, 0, 0, 0, 0, 180);
                //Mesh box4 = new Mesh(@"C:\Users\ethan\Downloads\JEGE_images\Poly.JSON", 0, 0, 0, 0, 180, 0);
                //Mesh box5 = new Mesh(@"C:\Users\ethan\Downloads\JEGE_images\Poly.JSON", 0, 0, 0, 0, 180, 180);
                //item.add_propertie(box3);
                item.add_propertie(box2);
                //item.add_propertie(box4);
                //item.add_propertie(box5);
                sceen.add_item(item);
            }

            //}

            Item.rotatei = true;
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
