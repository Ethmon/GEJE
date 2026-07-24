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
        bool clicked = false;
        
        public Mesh lastOne;

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

                //Console.WriteLine(mouse[0] + " : " + mouse[1] + " -:- " + a);
                //Console.WriteLine(a);
                if (a != 0)
                {
                    //Console.WriteLine(a.ToString());
                    Mesh b = hovered;

                    if (b != null)
                    {

                        if (lastOne != b)
                        {
                            //Console.WriteLine("OK");
                            //b.hueit(20, 20, 20);

                            camera.ItemMap[hovered].ExitHover();

                            

                            if (lastOne != null)
                            {

                                camera.ItemMap[lastOne].EnterHover();
                            }
                            lastOne = b;
                        }
                        else if(lastOne==b)
                        {

                        }
                        else if (lastOne == null)
                        {

                            camera.ItemMap[hovered].ExitHover();

                        }
                        else { }
                    }
                    if (b == null)
                        Console.WriteLine("WHY");


                    if(window.left_click)
                    {
                        if (clicked == false)
                        {
                            clicked = true;
                            camera.ItemMap[hovered].OnLeftClick_start();



                        }
                        else
                            camera.ItemMap[hovered].OnLeftClick_hold();
                    }
                    else
                    {
                        if (clicked == true)
                        {
                            clicked = false;
                            camera.ItemMap[hovered].OnLEftClick_end();
                        }

                    }


                }
            }

            //window.Cleartags();
            
        }

        
        
    }
}
