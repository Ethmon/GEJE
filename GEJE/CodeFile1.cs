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

            //Item item = new Item(200, 0, 0, 0, 0, 0);
            //Mesh box2 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\Pyramid.json", 0, 0, 0, 0, 0, 0);

            //Item item1 = new Item(-200, 0, 0, 0, 0, 0);
            //Mesh box21 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\Pyramid.json", 0, 0, 0, 30, 60, 60);


            //Item item3 = new Item(0, 0, 200, 0, 0, 0);
            //Mesh box23 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\box.json", 0, 0, 0, 0, 0, 0);

            ////Item item13 = new Item(0, 0, -200, 0, 0, 0);
            ////Mesh box213 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\box.json", 0, 0, 0, 30, 60, 60);

            //Item item4 = new Item(200, 0, 200, 0, 0, 0);
            //Mesh box24 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\Prisim.json", 0, 0, 0, 0, 0, 0);

            //Item item14 = new Item(-200, 0, -200, 0, 0, 0);
            //Mesh box214 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\Prisim.json", 0, 0, 0, 30, 60, 60);


            ////Mesh box3 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\box.json", 800, 200, -10, -20, 0, -20);
            ////Mesh box3 = new Mesh(@"C:\Users\ethan\Downloads\Box.json", 10, 20, 0, 0, 0, 0);// No initial rotation for the mesh

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
            for (int i = 0; i < 10; i++)
            {
                Item item = new Item(0, 0, 250 + i*60, 0, 0, 0);
                Mesh box2 = new Mesh(@"C:\Users\ethan\OneDrive\Desktop\GEJE_Images\Complex2.json", 0, 0, 0, 90, 0, 0);
                item.add_propertie(box2);
                sceen.add_item(item);
            }



            Item camera = new Item(0, 0, -30, 0, 0, 0);
            Window win = new Window(600, 300);
            Camera cam = new Camera(0,0, 0, 0, 0, 0, sceen, win,1);
            Movement cam_movement = new Movement(0, 0, 0, 0, 0, 0, camera,2);
            camera.add_propertie(cam_movement);
            camera.add_propertie(cam);
            camera.add_propertie(cam_movement);
            sceen.add_item(camera);

            sceen.Start_scene();
            
            win.Run();
        }
    }
}
