using System;
using System.Drawing;
using System.Collections.Generic;

namespace GEJE
{
    public class HoverHighlight : Proportie
    {
        private Color? originalColor = null;
        private double hueShiftAmount;
        private bool isHovered = false;
        public Item parent;
        public Window window;
        public Camera camera;
        ThreeDSceen scene;
        
        Mesh lastOne;

        public HoverHighlight(Item parent, Window window, ThreeDSceen scene,double hueShift = 0.08)
        {
            this.hueShiftAmount = hueShift;
            this.parent = parent;
            this.window = window;
            this.scene = scene;

        }

        public override void Update()
        {
            int[] mouse = window.currsor_pos();
            int a = window.getTagOfPixle(mouse[0], mouse[1]);

            Mesh hovered = null;
            if (camera.TagMap.TryGetValue(a, out hovered))
            {


                //Console.WriteLine(a);
                if (a != 0)
                {
                    //Console.WriteLine(a.ToString());
                    Mesh b = camera.TagMap[a];
                    if (b != null)
                    {

                        if (lastOne != b)
                        {
                            //Console.WriteLine("OK");
                            b.hueit(50, 50, 50);

                            if (lastOne != null)
                            {
                                lastOne.hueit(-50, -50, -50);
                            }
                            lastOne = b;
                        }
                        else if(lastOne==b)
                        {

                        }
                        else if (lastOne == null)
                        {
                            b.hueit(50, 50, 50);
                            lastOne = b;

                        }
                        else { }
                    }
                    if (b == null)
                        Console.WriteLine("WHY");


                }
            }

            window.Cleartags();
            
        }

        
        
    }
}
