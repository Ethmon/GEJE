using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.Metrics;
namespace GEJE
{
    public class ThreeDSceen
    {
        public List<Item> items = new List<Item>();
        public PhysicsWorld physics = new PhysicsWorld();
        // Items added/removed while the scene is updating (e.g. spawned by a script)
        // are applied at the start of the next update, so the list isn't changed mid-loop.
        private readonly List<Item> pendingAdds = new List<Item>();
        private readonly List<Item> pendingRemoves = new List<Item>();
        private readonly object pendingLock = new object();
        int xsize, ysize, zsize;
        static TimeDeltaTime timer = new TimeDeltaTime();
        /// <summary>
        /// Seconds since the last scene update (e.g. 0.0167 at 60 FPS, 0.0069 at 144 FPS).
        /// Multiply any per-frame change by this to make it per-second instead, so things
        /// move at the same speed no matter how fast the computer runs the loop.
        /// </summary>
        public static double deltaTime = 0;
        /// <summary>Longest step allowed, so a hitch (window drag, breakpoint) doesn't teleport things.</summary>
        public static double maxDeltaTime = 0.1;
        public ThreeDSceen(int xtotal, int ytotal, int ztotal)
        {

            this.xsize = xtotal;
            this.ysize = ytotal;
            this.zsize = ztotal;

        }
        public void add_item(Item item)
        {
            items.Add(item);
        }
        public void remove_item(Item item)
        {
            items.Remove(item);
        }
        /// <summary>Safe to call from inside an Item/Proportie Update.</summary>
        public void queue_add(Item item)
        {
            lock (pendingLock) pendingAdds.Add(item);
        }
        /// <summary>Safe to call from inside an Item/Proportie Update.</summary>
        public void queue_remove(Item item)
        {
            lock (pendingLock) pendingRemoves.Add(item);
        }
        private void apply_pending()
        {
            lock (pendingLock)
            {
                foreach (Item item in pendingAdds)
                {
                    item.Start();
                    items.Add(item);
                }
                foreach (Item item in pendingRemoves)
                    items.Remove(item);
                pendingAdds.Clear();
                pendingRemoves.Clear();
            }
        }
        byte counter = 0;
        int counter2 = 0;
        public void update()
        {
            //Thread.Sleep(10);
            //while (true)
            {
                //Thread.Sleep(3);

                //var watch = new Stopwatch();
                //watch.Start();
                deltaTime = Math.Min(timer.DeltaTime().TotalSeconds, maxDeltaTime);
                apply_pending();
                physics.Step(deltaTime);
                foreach (Item item in items)
                {
                    if (item.state)
                        item.Update();
                }
                //watch.Stop();
                //counter2 += (int)watch.ElapsedMilliseconds;
                if (counter2 > 1000)
                {
                    counter2 = 0;
                    Console.WriteLine("FPS: " + counter);
                    counter = 0;
                }
                else counter++;

            }
        }
        public void Start_scene()
        {

            foreach (Item item in items)
            {
                item.Start();
            }
            Thread item_updates = new Thread(new ThreadStart(update));
            item_updates.Start();
        }

    }
}