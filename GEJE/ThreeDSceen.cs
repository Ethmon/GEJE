using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
namespace GEJE
{
    internal class ThreeDSceen
    {
        public List<Item> items = new List<Item>();
        int xsize, ysize, zsize;
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
        public void update()
        {
            //Thread.Sleep(10);
            while (true)
            {
                //Thread.Sleep(3);
                //var watch = new Stopwatch();
                //watch.Start();
                foreach (Item item in items)
                {
                   
                    item.Update();
                }
                //watch.Stop();
                //Console.WriteLine(watch.ElapsedMilliseconds);
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
