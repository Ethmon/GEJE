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
                    widthDivisions: 75,
                    depthDivisions: 75,
                    cellSize: 25,
                    jitter: .6,   // 0 = perfect grid, 0.25 = some randomness
                    zLevel: 0
                    );

            foreach (var t in terrain)
                sceen.add_item(t);

            Item.rotatei = false;
            Item.floatingyay = false;
            //Console.WriteLine(box2.ToString());
            Item camera = new Item(0, -1000, 0, 0, 0, 0);
            Window win = new Window(600, 400, 2, 2);
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
        Ocean = 6,
        DeepOcean = 7,
        Plateau = 8,
        River = 9

    }



    public static class TileRules
    {
        public static Dictionary<GroundType, double> clusterBias = new Dictionary<GroundType, double>
        {
            [GroundType.Grass] = 14.0,
            [GroundType.Water] = 6.0,
            [GroundType.Hill] = 5.0,
            [GroundType.Mountain] = 0.3,
            [GroundType.FarmLand] = 1.5,
            [GroundType.Ocean] = 16.0,
            [GroundType.DeepOcean] = 15.0,
            [GroundType.Plateau] = 1.0,
            [GroundType.River] = 0.3
        };

        public static Dictionary<GroundType, HashSet<GroundType>> forbiddenNeighbors = new Dictionary<GroundType, HashSet<GroundType>>
        {
            [GroundType.Mountain] = new HashSet<GroundType> { GroundType.Water, GroundType.Ocean,GroundType.DeepOcean, GroundType.Plateau },
            [GroundType.Water] = new HashSet<GroundType> { GroundType.Mountain, GroundType.Plateau,GroundType.DeepOcean }, 
            [GroundType.Ocean] = new HashSet<GroundType> { GroundType.FarmLand,GroundType.Hill,GroundType.Mountain,GroundType.Plateau,GroundType.Grass, GroundType.River },
            [GroundType.DeepOcean] = new HashSet<GroundType> { GroundType.Water,GroundType.Grass,GroundType.Hill,GroundType.Mountain, GroundType.FarmLand,GroundType.Plateau, GroundType.River },
            [GroundType.Plateau] = new HashSet<GroundType> { GroundType.Mountain,GroundType.Water,GroundType.Ocean,GroundType.DeepOcean,GroundType.Hill,GroundType.Plateau},
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
            [GroundType.River] = 3
        };

        public static Dictionary<GroundType, Dictionary<GroundType, double>> neighborInfluence =
        new Dictionary<GroundType, Dictionary<GroundType, double>>
        {
            [GroundType.Water] = new Dictionary<GroundType, double>
            {
                [GroundType.Water] = 1.7,
                [GroundType.Hill] = .5,
                [GroundType.Grass] = .95,
                [GroundType.River] = .2,
                [GroundType.DeepOcean] = 1.4
                
            },
            [GroundType.Mountain] = new Dictionary<GroundType, double>
            {
                [GroundType.Hill] = 1.5,    
                [GroundType.FarmLand] = 0.7,
                [GroundType.Mountain] = 3
            },
            [GroundType.River] = new Dictionary<GroundType, double>
            {
                [GroundType.River] = 5,
                [GroundType.Water] = .4,
                [GroundType.Plateau] = .2
            },
            [GroundType.Grass]= new Dictionary<GroundType, double>
            {
                [GroundType.Water] = .75
            },
            [GroundType.DeepOcean]= new Dictionary<GroundType, double>
            {
                [GroundType .DeepOcean] = 2.6,
                [GroundType.Ocean] = 1.4
            },
            [GroundType.Ocean] = new Dictionary<GroundType, double>
            {
                [GroundType.DeepOcean] = .9,
                [GroundType.Ocean] = 2,
                [GroundType.Water] = 1.3
            },
            [GroundType.Plateau] = new Dictionary<GroundType, double>
            {
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
    public static class FlatTriangleLayer
    {
        public static List<Item> GenerateFlatLayer(
    int widthDivisions,
    int depthDivisions,
    double cellSize,
    double jitter,
    double zLevel)
        {
            Random rand = new Random();
            List<Item> terrainItems = new List<Item>();

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
                    t.Randomiza(rand, neighbors);
                    

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

                    Item triItem1 = new Item(0, 0, 0, 0, 0, 0);
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
