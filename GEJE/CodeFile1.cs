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

            // Ground: a big flat box that never moves
            Item ground = new Item(0, 0, 0, 0, 0, 0);
            Mesh groundMesh = new Mesh("Models\\Wall1.JSON", 0, 0, 0, 0, 0, 0);
            groundMesh.scale(100, 2, 100);
            ground.add_propertie(groundMesh);
            new PhysicsBody(ground, sceen.physics) { isStatic = true };
            sceen.add_item(ground);




            Item Box1 = new Item(0, -100, 0, 0, 0, 0);
            Mesh Box1Mesh = new Mesh("Models\\Wall1.JSON", 0, 0, 0, 0, 0, 0);
            Box1Mesh.scale(10, 5, 10);
            Box1Mesh.hueit(0, 0, 100);
            Box1.add_propertie(Box1Mesh);
            new PhysicsBody(Box1, sceen.physics) { isStatic = true };
            sceen.add_item(Box1);




            Item Box2 = new Item(600, -180, 0, 0, 0, 0);
            Mesh Box2Mesh = new Mesh("Models\\Wall1.JSON", 0, 0, 0, 0, 0, 0);
            Box2Mesh.scale(10, 5, 10);
            Box2Mesh.hueit(0, 100, 0);
            Box2.add_propertie(Box2Mesh);
            new PhysicsBody(Box2, sceen.physics) { isStatic = true };
            sceen.add_item(Box2);


            Item Box3 = new Item(750, -240, 500, 0, 0, 0);
            Mesh Box3Mesh = new Mesh("Models\\Wall1.JSON", 0, 0, 0, 0, 0, 0);
            Box3Mesh.scale(10, 5, 10);
            Box3Mesh.hueit(100, 0, 0);
            Box3.add_propertie(Box3Mesh);
            new PhysicsBody(Box3, sceen.physics) { isStatic = true };
            sceen.add_item(Box3);




            // Elevator: an animated mesh with physics. It slowly rises next to the red box and
            // sinks back down, carrying whatever stands on it. Ride it up, then walk onto the red box.
            Item elevator = new Item(950, -20, 170, 0, 0, 0);
            PhysicsAnimatedMesh elevatorMesh = new PhysicsAnimatedMesh(0, 0, 0, 0, 0, 0);
            elevatorMesh.AddFrame("down", BlockMesh(400, 20, 400, 200, 170, 40));    // thin pad
            elevatorMesh.AddFrame("up", BlockMesh(400, 280, 400, 200, 170, 40));     // tall: top just above the red box
            // wait 1.5 s at the bottom, rise for 3 s, wait 1.5 s at the top, sink for 3 s, repeat
            elevatorMesh.AddAnimation("lift",
                new[] { "down", "down", "up", "up" },
                new[] { 1.5, 3.0, 1.5, 3.0 },
                true).smooth = true;
            elevatorMesh.Play("lift");
            elevator.add_propertie(elevatorMesh);
            new PhysicsBody(elevator, sceen.physics) { isStatic = true };
            sceen.add_item(elevator);



            // A pyramid sitting on the ground. Physics uses its real sloped shape.
            Item pyramid = new Item(-1000, -22, -260, 90, 180, 0);
            Mesh pyramidMesh = new Mesh("Models\\Kirby.JSON", 0, 0, 0, 0, 0, 0);
            pyramidMesh.scale(60, 60, 60);
            pyramid.add_propertie(pyramidMesh);
            new PhysicsBody(pyramid, sceen.physics) { isStatic = true };
            sceen.add_item(pyramid);

            // An apple that drops onto the pyramid and slides off its side (-Y is up in this engine)
            Item apple = new Item(170, -1000, -87, 90, 0, 0);
            Mesh appleMesh = new Mesh("Models\\Coin.json", 0, 0, 0, 0, 0, 0);
            appleMesh.scale(20, 20, 20);
            apple.add_propertie(appleMesh);
            new PhysicsBody(apple, sceen.physics) { bounciness = 0.001 };
            sceen.add_item(apple);
            apple.LastBody.friction = 1;

            // Invisible wall: physics only, no visible mesh. Thrown boxes bounce off thin air.

            Item invisibleWall = new Item(0, -800, 700, 0, 0, 0);
            PhysicsBody wallBody = new PhysicsBody(invisibleWall, sceen.physics) { isStatic = true };
            wallBody.SetColliderBox(new Vec3(2200, 1600, 50));
            sceen.add_item(invisibleWall);


            // Camera: WASD/QE to move, arrows to look, F to throw boxes
            Window win = new Window(1200, 600, 1, 1);
            win.scene = sceen;
            Item camera = new Item(0, -1000, -1300, 40, 0, 0);
            Camera cam = new Camera(0, 0, 10, 90, 0, 0, sceen, win, (double)win.Ethwidth / win.Ethheight);
            Physics_Mover movement = new Physics_Mover(0, 0, 0, 0, 0, 0, apple.LastBody);
            movement.multiplyer = 600;
            apple.LastBody.drag = .1;
            movement.window = win;
            apple.add_propertie(movement);
            camera.add_propertie(cam);
            camera.add_propertie(new BoxThrower(camera, win, sceen));
            sceen.add_item(camera);
            win.cam = cam;

            sceen.Start_scene();
            win.RunGame();
        }

        /// <summary>
        /// A box mesh whose bottom sits on the item's position and that goes up by height
        /// (-Y is up in GEJE). Each face gets its own shade so the edges are visible.
        /// Every call builds the triangles in the same order, so two of these can be
        /// blended smoothly as animation frames.
        /// </summary>
        static Mesh BlockMesh(double width, double height, double depth, byte r, byte g, byte b)
        {
            double x = width / 2, z = depth / 2, top = -height;
            var polys = new List<Polygon>();
            void Face(double shade, (double, double, double) a, (double, double, double) c1,
                      (double, double, double) c2, (double, double, double) d)
            {
                byte R = (byte)Math.Min(255, r * shade), G = (byte)Math.Min(255, g * shade), B = (byte)Math.Min(255, b * shade);
                Point P((double px, double py, double pz) v) => new Point(v.px, v.py, v.pz, 1, R, G, B);
                polys.Add(new Polygon(P(a), P(c1), P(c2)));
                polys.Add(new Polygon(P(a), P(c2), P(d)));
            }
            Face(1.15, (-x, top, -z), (x, top, -z), (x, top, z), (-x, top, z));   // top
            Face(0.55, (-x, 0, -z), (-x, 0, z), (x, 0, z), (x, 0, -z));           // bottom
            Face(0.85, (-x, 0, -z), (x, 0, -z), (x, top, -z), (-x, top, -z));     // sides
            Face(0.85, (-x, 0, z), (-x, top, z), (x, top, z), (x, 0, z));
            Face(0.70, (-x, 0, -z), (-x, top, -z), (-x, top, z), (-x, 0, z));
            Face(0.70, (x, 0, -z), (x, 0, z), (x, top, z), (x, top, -z));
            return new Mesh(polys, 0, 0, 0, 0, 0, 0);
        }
    }
    public class Box : Item
    {
        public Box(double x, double y, double z, double xrot, double yrot, double zrot) : base(x, y, z, xrot, yrot, zrot)
        {

        }
        public override void EnterHover()
        {
            foreach (Proportie item in this.properties)
            {
                if (item is Mesh)
                {
                    Mesh a = (Mesh)item;
                    a.hueit(-20, -20, -20);

                }
            }
        }
        public override void ExitHover()
        {
            foreach (Proportie item in this.properties)
            {
                if (item is Mesh)
                {
                    Mesh a = (Mesh)item;
                    a.hueit(20, 20, 20);

                }
            }
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

    public class lolgag
    {
        public static TimeDeltaTime dd = new TimeDeltaTime();
        public static void d()
        {
            Console.WriteLine(dd.DeltaTime());
        }
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
            [GroundType.Mountain] = new HashSet<GroundType> { GroundType.Water, GroundType.Ocean, GroundType.DeepOcean, GroundType.Plateau },
            [GroundType.Water] = new HashSet<GroundType> { GroundType.Mountain, GroundType.DeepOcean },
            [GroundType.Ocean] = new HashSet<GroundType> { GroundType.FarmLand, GroundType.Hill, GroundType.Mountain, GroundType.Plateau, GroundType.Grass, GroundType.River },
            [GroundType.DeepOcean] = new HashSet<GroundType> { GroundType.Water, GroundType.Grass, GroundType.Hill, GroundType.Mountain, GroundType.FarmLand, GroundType.Plateau, GroundType.River },
            [GroundType.Plateau] = new HashSet<GroundType> { GroundType.Mountain, GroundType.Ocean, GroundType.DeepOcean, GroundType.Hill, GroundType.Plateau },
            [GroundType.Grass] = new HashSet<GroundType> { GroundType.DeepOcean, GroundType.Ocean },
            [GroundType.Hill] = new HashSet<GroundType> { GroundType.DeepOcean, GroundType.Ocean, GroundType.Plateau },
            [GroundType.FarmLand] = new HashSet<GroundType> { GroundType.Ocean, GroundType.DeepOcean },
            [GroundType.River] = new HashSet<GroundType> { GroundType.Ocean, GroundType.DeepOcean }
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
            [GroundType.Grass] = new Dictionary<GroundType, double>
            {
                [GroundType.Water] = 1.25,
                [GroundType.Grass] = 4.3,
                [GroundType.FarmLand] = 3.2,
                [GroundType.Hill] = 4,
                [GroundType.Plateau] = 3,
                [GroundType.River] = 4,
                [GroundType.Mountain] = 4

            },
            [GroundType.FarmLand] = new Dictionary<GroundType, double>
            {
                [GroundType.Water] = .05,
                [GroundType.FarmLand] = 1.4,
                [GroundType.River] = 1.2
            },
            [GroundType.DeepOcean] = new Dictionary<GroundType, double>
            {
                [GroundType.DeepOcean] = 4.6,
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
                [GroundType.River] = .2
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
                    if (tiles[nx, ny] != null)
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
            foreach (Tile N in Neighbors)
            {
                if (N != null)
                    a += N.type.ToString() + " ";
            }
            return a;
        }

    }
    public class terrainTile : Item
    {
        byte[] color1;
        byte[] color2;
        public terrainTile(double x, double y, double z, double xrot, double yrot, double zrot, byte[] c1, byte[] c2) : base(x, y, z, xrot, yrot, zrot)
        {
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
                    neighbors = Tile.GetNeighbors(tiles, x, y, true);
                    // --- Create tile and assign type based on neighbors ---
                    Tile t = new Tile();
                    if (x == 0 && y == 0) t.type = GroundType.Grass;
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

                    terrainTile triItem1 = new terrainTile(0, 0, 0, 0, 0, 0, color, new byte[] { (byte)((int)cr + 20), (byte)((int)cg + 20), (byte)((int)cb + 20) });
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
                    tiles[i, k].Neighbors = Tile.GetNeighbors(tiles, i, k, true);


            return terrainItems;
        }



    }

}