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
            /*
            List<terrainTile> terrain = FlatTriangleLayer.GenerateFlatLayer(
                    widthDivisions: 100,
                    depthDivisions: 100,
                    cellSize: 100,
                    jitter: .6,   // 0 = perfect grid, 0.25 = some randomness
                    zLevel: -100
                    );

            foreach (var t in terrain)
                sceen.add_item(t);
            
            Item d = new Item(0,20,0,90,0,0);
            Mesh m = new Mesh("Models\\apple.json", 0, 0, 0, 0, 0, 0);
            m.scale(100, 100, 100);
            d.add_propertie(m);
            sceen.add_item(d);
            Item jjdsj = new Item(10000, -150, 0, 90, 0, 0);
            Mesh mm = new Mesh("Models\\Wall1.json", 0, 0, 0, 0, 0, 0);
            mm.scale(100, 100, 100);
            jjdsj.add_propertie(mm);
            sceen.add_item(jjdsj);
            */
            Random random = new Random();
            for (int i = 1; i < 10; i++)
            {

                for (int k = 1; k < 10; k++)
                {
                    for (int ppp = 0; ppp < 5; ppp++)
                    {
                        if (((i == 1 || i == 9) && ppp < 4) || ((i == 5) && ppp < 2 && k > 3 && k < 6) || ((k == 9)))
                        {
                            double d = random.NextDouble();
                            double d2 = random.NextDouble();
                            Item under = new Item((-20 + 20 * i) * 20, (30 - (ppp * 20))*20 , (-10 + k * 20) *20, 90, (d > .75) ? 0 : (d > .5) ? 90 : (d > .25) ? 180 : 270, (d2 > .75) ? 0 : (d2 > .5) ? 90 : (d2 > .25) ? 180 : 270);
                            Mesh box3 = new Mesh("Models\\Wall1.JSON", 0, 0, 0, 0, 0, 0);
                            box3.scale(20,20,20);
                            int red = random.Next(0, 10) * i;
                            int green = random.Next(0, 10) * k;
                            int blue = random.Next(0, 10) * ppp;
                            box3.hueit(red, green, blue);
                            under.add_propertie(box3);
                            sceen.add_item(under);
                        }
                    }
                }
            }
            Item sword = new Item(80 *20, -15 *20, 80 * 20, 90, 0, 0);
            Mesh swordmesh = new Mesh("Models\\Sword.JSON", 0, 0, 0, 0, 0, 0);
            swordmesh.scale(20,20,20);
            SwordDemo demo = new SwordDemo(0, 0, 0, 0, 0, 0);
            demo.sword = sword;

            sword.add_propertie(demo);

            sword.add_propertie(swordmesh);
            sceen.add_item(sword);

            Item.rotatei = false;
            Item.floatingyay = false;
            //Console.WriteLine(box2.ToString());
            Item camera = new Item(0, 000100, 0, 0, 0, 0);
            Window win = new Window(600, 400, 3, 3);
            demo.win = win;
            win.scene = sceen;
            Camera cam = new Camera(0, 0, 10, 90, 0, 0, sceen, win, (double)win.Ethwidth / win.Ethheight);
            //cam.outline = false;
            //cam.fillin = true;
            Movement cam_movement = new Movement(0, 0, 0, 0, 0, 0, camera, 15);
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
        Ocean = 6,
        DeepOcean = 7,
        Plateau = 8,
        River = 9

    }



    public static class TileRules
    {
        public static Dictionary<GroundType, double> clusterBias = new Dictionary<GroundType, double>
        {
            [GroundType.Grass] = 32.0,
            [GroundType.Water] = 40.0,
            [GroundType.Hill] = 20.0,
            [GroundType.Mountain] = 0.7,
            [GroundType.FarmLand] = 8.5,
            [GroundType.Ocean] = 65.0,
            [GroundType.DeepOcean] = 10.0,
            [GroundType.Plateau] = 1.0,
            [GroundType.River] = 0.5
        };

        public static Dictionary<GroundType, HashSet<GroundType>> forbiddenNeighbors = new Dictionary<GroundType, HashSet<GroundType>>
        {
            [GroundType.Mountain] = new HashSet<GroundType> { GroundType.Water, GroundType.Ocean,GroundType.DeepOcean, GroundType.Plateau },
            [GroundType.Water] = new HashSet<GroundType> { GroundType.Mountain,GroundType.DeepOcean }, 
            [GroundType.Ocean] = new HashSet<GroundType> { GroundType.FarmLand,GroundType.Hill,GroundType.Mountain,GroundType.Plateau,GroundType.Grass, GroundType.River },
            [GroundType.DeepOcean] = new HashSet<GroundType> { GroundType.Water,GroundType.Grass,GroundType.Hill,GroundType.Mountain, GroundType.FarmLand,GroundType.Plateau, GroundType.River },
            [GroundType.Plateau] = new HashSet<GroundType> { GroundType.Mountain,GroundType.Ocean,GroundType.DeepOcean,GroundType.Hill,GroundType.Plateau},
            [GroundType.Grass] = new HashSet<GroundType> { GroundType.DeepOcean, GroundType.Ocean },
            [GroundType.Hill] = new HashSet<GroundType> { GroundType.DeepOcean,GroundType.Ocean,GroundType.Plateau},
            [GroundType.FarmLand] = new HashSet<GroundType> { GroundType.Ocean, GroundType.DeepOcean },
            [GroundType.River] = new HashSet<GroundType> {GroundType.Ocean, GroundType.DeepOcean }
        };

        public static Dictionary<GroundType, int> globalLimits = new Dictionary<GroundType, int>
        {

        };


        public static Dictionary<GroundType, int> globalCounts = new Dictionary<GroundType, int>();


        public static Dictionary<GroundType, int> adjacencyLimits = new Dictionary<GroundType, int>
        {
            [GroundType.River] = 2,
            [GroundType.Mountain] = 3
        };

        public static Dictionary<GroundType, Dictionary<GroundType, double>> neighborInfluence =
        new Dictionary<GroundType, Dictionary<GroundType, double>>
        {
            [GroundType.Water] = new Dictionary<GroundType, double>
            {
                [GroundType.Water] = 17,
                [GroundType.Hill] = 1.5,
                [GroundType.Grass] = 7,
                [GroundType.Plateau] = .1,
                [GroundType.River] = .2,
                [GroundType.Ocean] = 12.4
                
            },
            [GroundType.Mountain] = new Dictionary<GroundType, double>
            {
                [GroundType.Hill] = 2.5,    
                [GroundType.FarmLand] = 0.7,
                [GroundType.Mountain] = 9.5
            },
            [GroundType.River] = new Dictionary<GroundType, double>
            {
                [GroundType.River] = 15,
                [GroundType.Water] = 1.4,
                [GroundType.Plateau] = .2
            },
            [GroundType.Grass]= new Dictionary<GroundType, double>
            {
                [GroundType.Water] = 1.25,
                [GroundType.Grass] = 4.3,
                [GroundType.FarmLand] = 3.2,
                [GroundType.Hill] = 4,
                [GroundType.Plateau] = 3,
                [GroundType.River] = 4,
                [GroundType.Mountain] = 4

            },
            [GroundType.FarmLand]= new Dictionary<GroundType, double>
            {
                [GroundType.Water] = .05,
                [GroundType.FarmLand] = 1.4,
                [GroundType.River] = 1.2
            },
            [GroundType.DeepOcean]= new Dictionary<GroundType, double>
            {
                [GroundType .DeepOcean] = 4.6,
                [GroundType.Ocean] = 2
            },
            [GroundType.Ocean] = new Dictionary<GroundType, double>
            {
                [GroundType.DeepOcean] = 2.8,
                [GroundType.Ocean] = 2.2,
                [GroundType.Water] = 2.3
            },
            [GroundType.Plateau] = new Dictionary<GroundType, double>
            {
                [GroundType.Water] = .15,
                [GroundType.River ] = .2
            }
            
        };
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
            [GroundType.Ocean] = new byte[] { 22, 105, 140 },
            [GroundType.DeepOcean] = new byte[] { 20, 22, 107 },
            [GroundType.Plateau] = new byte[] { 230, 191, 138 },
            [GroundType.River] = new byte[] { 44, 124, 204 },

        };
        public Mesh tileMesh;
        public GroundType type;
        public List<Tile> Neighbors = new List<Tile>();
        public override void Start()
        {
            base.Start();
            //tileMesh.hueit(-255, -255, -255);
            //byte[] a = tile_colors[type];
            //tileMesh.hueit(a[0], a[1], a[2]);
        }
        public static List<Tile> GetNeighbors(Tile[,] tiles, int x, int y, bool includeDiagonals = false)
        {
            var neighbors = new List<Tile>();

            var offsets = new List<(int dx, int dy)>
            {
                (-1, 0), (1, 0), (0, -1), (0, 1)
            };

            if (includeDiagonals)
            {
                offsets.AddRange(new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) });
            }

            foreach (var (dx, dy) in offsets)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < tiles.GetLength(0) &&
                    ny >= 0 && ny < tiles.GetLength(1))
                {
                    if (tiles[nx,ny] != null)
                        neighbors.Add(tiles[nx, ny]);
                }
            }

            return neighbors;
        }

        public void Randomiza(Random rand, List<Tile> neighbors)
        {
            var allTypes = Enum.GetValues(typeof(GroundType))
                .Cast<GroundType>()
                .Where(t => t != GroundType.None).ToList();

            Dictionary<GroundType, double> weights = new Dictionary<GroundType, double>();

            foreach (var typeCandidate in allTypes)
            {
                double weight = 1.0;

                
                if (neighbors.Any(n => n != null && n.type == typeCandidate) &&
                    TileRules.clusterBias.TryGetValue(typeCandidate, out double bias))
                {
                    weight += bias;
                }

                
                if (neighbors.Any(n => n != null &&
                                       TileRules.forbiddenNeighbors.TryGetValue(n.type, out var forbidden) &&
                                       forbidden.Contains(typeCandidate)))
                {
                    weight = 0;
                }

                
                if (TileRules.globalLimits.TryGetValue(typeCandidate, out int limit) &&
                    TileRules.globalCounts.TryGetValue(typeCandidate, out int count) &&
                    count >= limit)
                {
                    weight = 0;
                }

                
                int sameNeighborCount = neighbors.Count(n => n != null && n.type == typeCandidate);
                if (TileRules.adjacencyLimits.TryGetValue(typeCandidate, out int maxAdj) &&
                    sameNeighborCount >= maxAdj)
                {
                    weight = 0;
                }

                foreach (var n in neighbors.Where(n => n != null))
                {
                    if (TileRules.neighborInfluence.TryGetValue(n.type, out var influenceMap) &&
                        influenceMap.TryGetValue(typeCandidate, out double influence))
                    {
                        weight *= influence;
                    }
                }

                weights[typeCandidate] = weight;
            }

            double total = weights.Values.Sum();
            if (total <= 0)
            {
                type = allTypes[rand.Next(allTypes.Count)];
                return;
            }

            double pick = rand.NextDouble() * total;
            foreach (var kv in weights)
            {
                pick -= kv.Value;
                if (pick <= 0)
                {
                    type = kv.Key;

                    
                    if (!TileRules.globalCounts.ContainsKey(type))
                        TileRules.globalCounts[type] = 0;
                    TileRules.globalCounts[type]++;
                    return;
                }
            }

            type = allTypes[0];
        }



        public override string ToString()
        {
            String a = type.ToString() + "\n";
            foreach(Tile N in Neighbors)
            {
                if(N!=null)
                    a += N.type.ToString() + " ";
            }
            return a;
        }

    }
    public class terrainTile : Item
    {
        byte[] color1;
        byte[] color2;
        public terrainTile(double x, double y, double z, double xrot, double yrot, double zrot, byte[] c1, byte[]c2) : base(x, y, z, xrot, yrot, zrot) {
        this.color1 = c1;
        this.color2 = c2;
        }
        public override void EnterHover()
        {
            foreach (Proportie proportie in this.properties)
            {
                if (proportie is Mesh)
                {
                    //((Mesh)proportie).hardsetcolor(color1[0], color1[1], color1[2]);
                    

                }
            }
        }
        public override void ExitHover()
        {
            foreach (Proportie proportie in this.properties)
            {
                if (proportie is Mesh)
                {
                    //((Mesh)proportie).hardsetcolor(color2[0], color2[1], color2[2]);
                    

                }
            }
        }
        public override void OnLeftClick_start()
        {
            foreach (Proportie proportie in this.properties)
            {
                if (proportie is Tile)
                {
                    Console.WriteLine(proportie.ToString());
                    break;
                }
            }
        }
    }
    public static class FlatTriangleLayer
    {
        public static List<terrainTile> GenerateFlatLayer(
    int widthDivisions,
    int depthDivisions,
    double cellSize,
    double jitter,
    double zLevel)
        {
            Random rand = new Random();
            List<terrainTile> terrainItems = new List<terrainTile>();

            // Store tiles in a 2D array for neighbor lookup
            Tile[,] tiles = new Tile[widthDivisions, depthDivisions];

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

                    // --- Get neighbors for this cell ---
                    List<Tile> neighbors = new List<Tile>();
                    /*
                    if (x > 0) neighbors.Add(tiles[x - 1, y]);       // left
                    if (y > 0) neighbors.Add(tiles[x, y - 1]);       // top
                    if (x > 0 && y > 0) neighbors.Add(tiles[x - 1, y - 1]); // top-left
                    if (x > 0 && y < depthDivisions - 1) neighbors.Add(tiles[x - 1, y + 1]); // bottom-left
                    if (x < widthDivisions - 1 && y > 0) neighbors.Add(tiles[x + 1, y - 1]); // top-right
                    */
                    neighbors = Tile.GetNeighbors(tiles,x,y,true);
                    // --- Create tile and assign type based on neighbors ---
                    Tile t = new Tile();
                    if(x==0 && y==0) t.type = GroundType.Grass;
                    else t.Randomiza(rand, neighbors);
                    

                    tiles[x, y] = t; // store in grid

                    byte[] color = Tile.tile_colors[t.type];
                    byte cr = color[0], cg = color[1], cb = color[2];

                    // Triangle 1 (p1, p2, p3)
                    Mesh tri1 = new Mesh(new List<Polygon>
            {
                new Polygon(
                    new Point(p1.x, zLevel, p1.y, 1, cr, cg, cb),
                    new Point(p2.x, zLevel, p2.y, 1, cr, cg, cb),
                    new Point(p3.x, zLevel, p3.y, 1, cr, cg, cb)
                )
            }, 0, 0, 0, 0, 0, 0);

                    terrainTile triItem1 = new terrainTile(0, 0, 0, 0, 0, 0,color, new byte[] { (byte)((int)cr +20)  , (byte)((int)cg+20), (byte)((int)cb+20) });
                    t.tileMesh = tri1;
                    triItem1.add_propertie(t);
                    triItem1.add_propertie(tri1);
                    terrainItems.Add(triItem1);

                    // Triangle 2 (p2, p4, p3)
                    Mesh tri2 = new Mesh(new List<Polygon>
            {
                new Polygon(
                    new Point(p2.x, zLevel, p2.y, 1, cr, cg, cb),
                    new Point(p4.x, zLevel, p4.y, 1, cr, cg, cb),
                    new Point(p3.x, zLevel, p3.y, 1, cr, cg, cb)
                )
            }, 0, 0, 0, 0, 0, 0);

                    //Item triItem2 = new Item(0, 0, 0, 0, 0, 0);
                    //Tile t2 = new Tile();
                    //t2.tileMesh = tri2;
                    //t2.type = t.type; // same type for both triangles in cell
                    //triItem2.add_propertie(t2);
                    triItem1.add_propertie(tri2);
                    //terrainItems.Add(triItem2);
                }
            }
            for (int i = 0; i < widthDivisions; i++)
                for (int k = 0; k < depthDivisions; k++)
                    tiles[i,k].Neighbors = Tile.GetNeighbors(tiles, i, k,true);


            return terrainItems;
        }
       


    }

}
