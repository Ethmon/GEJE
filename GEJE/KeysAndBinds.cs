using Microsoft.AspNetCore.Http.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEJE
{
    public class KeysAndBinds : Proportie
    {
        List<ItemKeys> items = new List<ItemKeys>();
        public Window window;
        ISet <int> curPres;
        IEnumerable<int> Overlap;
        public KeysAndBinds (double x, double y, double z, double xrot, double yrot, double zrot, Window window) : base(x, y, z, xrot, yrot, zrot)
        {
            this.window = window;
        }
        public void addItemKey(ItemKeys key)
            { items.Add(key); }
        public void removeItemKey(ItemKeys key) 
            { items.Remove(key); }
        public override void Start()
        {
            curPres = window.pressed;
        }
        public override void Update()
        {
            
            foreach(var item in items)
            {
                Overlap = curPres.Intersect(item.keys);
                if(Overlap.Count() == 0) {continue;}
                foreach(int key in Overlap)
                {
                    item.Use(key);
                }
            }
        }

    }
}
