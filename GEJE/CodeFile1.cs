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
            Random rnd = new Random();
            /*
            int i = 0;
            int max = 1000;
            {
                Item place = new Item((int)rnd.Next() % 551 * (((int)rnd.Next() % 2 == 0) ? 1 : -1), 80, (int)rnd.Next() % 551 * (((int)rnd.Next() % 2 == 0) ? 1 : -1), 90, 0, 0);
                byte r = (byte)(rnd.NextDouble() * 100 + 30);
                Mesh places = new Mesh(new List<Polygon> {
            new Polygon(
                 new Point(10,0,0,1,r,r,r), new Point(10,10,0,1,r,r,r), new Point(0,10,0,1,r,r,r) )


            }, 0, 0, 0, 0, 0, 0);
                //places.scale(5, 5, 1);

                place.add_propertie(places);
                sceen.add_item(place);
            }
            */
            //}

            List<Item> terrain = FlatTriangleLayer.GenerateFlatLayer(
                    widthDivisions: 50,
                    depthDivisions: 50,
                    cellSize: 15,
                    jitter: .5,   // 0 = perfect grid, 0.25 = some randomness
                    zLevel: 0,
                        r: 0, g: 0, b: 0
                    );

            foreach (var t in terrain)
                sceen.add_item(t);

            Item.rotatei = false;
            Item.floatingyay = false;
            //Console.WriteLine(box2.ToString());
            Item camera = new Item(0, -500, 0, 0, 0, 0);
            Window win = new Window(300, 200, 4, 4);
            win.scene = sceen;
            Camera cam = new Camera(0, 0, 0, 90, 0, 0, sceen, win, 1);
            cam.outline = false;
            cam.fillin = true;
            Movement cam_movement = new Movement(0, 0, 0, 0, 0, 0, camera, 5);
            cam_movement.window = win;
            HoverHighlight ffff = new HoverHighlight(camera, win, sceen);
            ffff.camera = cam;
            camera.add_propertie(ffff);
            camera.add_propertie(cam_movement);
            camera.add_propertie(cam);
            //camera.add_propertie(cam_movement);
            sceen.add_item(camera);
            win.cam = cam;
            sceen.Start_scene();

            win.RunGame();
        }
    }

    public enum GroundType
    {
        None = 0,
        Grass = 1,
        Water = 2,
        Hill = 3,
        Mountain = 4,
        FarmLand = 5,
        Ocean = 6

    }




    public class Tile : Proportie
    {
        public static Dictionary<GroundType, byte[]> tile_colors = new Dictionary<GroundType, byte[]>()
        {
            [GroundType.None] = new byte[] { 0, 0, 0 },
            [GroundType.Grass] = new byte[] { 68, 125, 47 },
            [GroundType.Water] = new byte[] { 73, 151, 184 },
            [GroundType.Hill] = new byte[] { 43, 87, 27 },
            [GroundType.Mountain] = new byte[] { 97, 97, 97 },
            [GroundType.FarmLand] = new byte[] { 55, 97, 7 },
            [GroundType.Ocean] = new byte[] { 22, 105, 140 }
        };
        public Mesh tileMesh;
        public GroundType type;
        public override void Start()
        {
            base.Start();
            tileMesh.hueit(-255, -255, -255);
            byte[] a = tile_colors[type];
            tileMesh.hueit(a[0], a[1], a[2]);
        }
        public void Randomiza(Random p)
        {
            //Random p = new Random();
            int a = (int)p.Next() % 6;
            if (a == 0) type = GroundType.Grass;
            else if (a == 1) type = GroundType.Water;
            else if (a == 2) type = GroundType.Hill;
            else if (a == 3) type = GroundType.Mountain;
            else if (a == 4) type = GroundType.FarmLand;
            else if (a == 5) type = GroundType.Ocean;
        }
    }
    public static class FlatTriangleLayer
    {
        public static List<Item> GenerateFlatLayer(
            int widthDivisions,
            int depthDivisions,
            double cellSize,
            double jitter,
            double zLevel,
            byte r, byte g, byte b)
        {
            Random rand = new Random();
            List<Item> terrainItems = new List<Item>();

            // Generate grid points
            (double x, double y)[,] points = new (double, double)[widthDivisions + 1, depthDivisions + 1];

            for (int x = 0; x <= widthDivisions; x++)
            {
                for (int y = 0; y <= depthDivisions; y++)
                {
                    double worldX = (x - widthDivisions / 2.0) * cellSize + (rand.NextDouble() - 0.5) * cellSize * jitter;
                    double worldY = (y - depthDivisions / 2.0) * cellSize + (rand.NextDouble() - 0.5) * cellSize * jitter;

                    points[x, y] = (worldX, worldY);
                }
            }

            // Create triangles
            for (int x = 0; x < widthDivisions; x++)
            {
                for (int y = 0; y < depthDivisions; y++)
                {
                    var p1 = points[x, y];
                    var p2 = points[x + 1, y];
                    var p3 = points[x, y + 1];
                    var p4 = points[x + 1, y + 1];

                    // Triangle 1 (p1, p2, p3)
                    {
                        Mesh tri1 = new Mesh(new List<Polygon>
                        {
                            new Polygon(
                                new Point(p1.x, zLevel, p1.y, 1, r, g, b),
                                new Point(p2.x, zLevel, p2.y, 1, r, g, b),
                                new Point(p3.x, zLevel, p3.y, 1, r, g, b)
                            )
                        }, 0, 0, 0, 0, 0, 0);

                        Item triItem1 = new Item(0, 0, 0, 0, 0, 0);
                        Tile t = new Tile();
                        t.tileMesh = tri1;
                        t.Randomiza(rand);
                        triItem1.add_propertie(t);
                        triItem1.add_propertie(tri1);
                        terrainItems.Add(triItem1);
                    }

                    // Triangle 2 (p2, p4, p3)
                    {
                        Mesh tri2 = new Mesh(new List<Polygon>
                        {
                            new Polygon(
                                new Point(p2.x, zLevel, p2.y, 1, r, g, b),
                                new Point(p4.x, zLevel, p4.y, 1, r, g, b),
                                new Point(p3.x, zLevel, p3.y, 1, r, g, b)
                            )
                        }, 0, 0, 0, 0, 0, 0);

                        Item triItem2 = new Item(0, 0, 0, 0, 0, 0);
                        Tile t2 = new Tile();
                        t2.tileMesh = tri2;
                        t2.Randomiza(rand);
                        triItem2.add_propertie(t2);
                        triItem2.add_propertie(tri2);
                        terrainItems.Add(triItem2);
                    }
                }
            }

            return terrainItems;
        }
    }

}
