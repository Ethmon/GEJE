using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GEJE
{
    public class Window : Form
    {
        private byte[,,] tiles;
        private bool[,] bools;
        private Bitmap[] buffers;
        
        private int currentBufferIndex;
        private readonly object graphicsLock = new object();
        private readonly object RenderLock = new object();
        public int Ethwidth;
        public int Ethheight;
        public Camera cam;
        // New variables for controlling pixel size
        public int PixelWidth;
        public int PixelHeight;

        public int[] currsor_pos()
        {
            int[] pos = new int[2];
            pos[0] = (int)(this.PointToClient(Cursor.Position).X / PixelWidth);
            pos[1] = (int)(this.PointToClient(Cursor.Position).Y / PixelHeight);
            if (pos[0] < 0) pos[0] = 0;
                else if (pos[0] >= Ethwidth) pos[0] = Ethwidth - 1;
            if (pos[1] < 0) pos[1] = 0;
                else if (pos[1] >= Ethheight) pos[1] = Ethheight - 1;
            return pos;
        }
        
        public ISet<int> pressed = new HashSet<int>();
        public bool right_click = false;
        public bool left_click = false;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                right_click = true;
            }
            else if (e.Button == MouseButtons.Left)
            {
                left_click = true;
            }
           
        }
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                right_click = false;
            }
            else if (e.Button == MouseButtons.Left)
            {
                left_click = false;
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            pressed.Add(e.KeyValue);
            
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            pressed.Remove(e.KeyValue);
        }



        public Window(int ewidth, int ehight, int pixelWidth, int pixelHeight)
        {
            this.Ethwidth = ewidth;
            this.Ethheight = ehight;
            this.PixelWidth = pixelWidth;
            this.PixelHeight = pixelHeight;

            // Calculate the actual window size based on Ethwidth, Ethheight, and pixel size
            this.Text = "Game";
            this.Size = new Size(Ethwidth * PixelWidth, Ethheight * PixelHeight);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            tiles = new byte[Ethwidth, Ethheight, 3];
            bools = new bool[Ethwidth, Ethheight];
            buffers = new Bitmap[3];  // Triple buffering
            for (int i = 0; i < 3; i++)
            {
                buffers[i] = new Bitmap(this.Width, this.Height);
            }

            currentBufferIndex = 0;

            for (int i = 0; i < Ethwidth; i++)
            {
                for (int j = 0; j < Ethheight; j++)
                {
                    tiles[i, j, 0] = 255;
                    tiles[i, j, 1] = 255;
                    tiles[i, j, 2] = 255;
                    bools[i, j] = false;
                }
            }
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.MouseDown += Form1_MouseDown;
            this.MouseUp += Form1_MouseUp;
        }

        public int Ethsize(bool worh)
        {
            if (worh)
                return Ethwidth;
            else
                return Ethheight;
        }

        public void Run()
        {
            //Thread video_rendering = new Thread(new ThreadStart(UpdateLoop));
            //video_rendering.Start();
            Application.Run(this);
        }
        public void update()
        {

            //Thread video_rendering = new Thread(new ThreadStart(UpdateLoop));
            //video_rendering.Start();
        }

        public void PlaceColor(int x, int y, byte r, byte g, byte b)
        {
            if (x < Ethwidth && x >= 0 && y < Ethheight && y >= 0)
            {
                tiles[x, y, 0] = r;//(byte)(a * r + (1 - a) * tiles[x,y,0]);
                tiles[x, y, 1] = g;//(byte)(a * g + (1 - a) * tiles[x, y, 1]);
                tiles[x, y, 2] = b;//(byte)(a * b + (1 - a) * tiles[x,y,2]);

            }
        }
        public void QPlaceColor(int x, int y, byte r, byte g, byte b)
        {
            if (x < Ethwidth && x >= 0 && y < Ethheight && y >= 0 && bools[x, y] == false)
            {
                tiles[x, y, 0] = r;
                tiles[x, y, 1] = g;
                tiles[x, y, 2] = b;
                bools[x, y] = true;
            }
        }


        unsafe void Clear()
        {
            int pixCount = Ethwidth * Ethheight;
            fixed (byte* pTiles = &tiles[0, 0, 0])
            {
                fixed (bool* ppp = &bools[0, 0])
                {


                    byte* px = pTiles;
                    for (int i = 0; i < pixCount; i++)
                    {
                        px[0] = 255;
                        px[1] = 255;
                        px[2] = 255;
                        px += 3;
                        ppp[i] = false; // if bools is linearized; or write using pointer
                    }
                }
            }
        }

        bool d = false;
        private Graphics g;
        public void UpdateLoop()
        {
            Draw();
            if (g == null) g = this.CreateGraphics();
            Invoke(new MethodInvoker(() =>
            {
                g.DrawImage(buffers[currentBufferIndex], 0, 0);
            }));
            Clear();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            lock (graphicsLock)
            {
                e.Graphics.DrawImageUnscaled(buffers[(currentBufferIndex + buffers.Length - 1) % buffers.Length], 0, 0);
            }
        }

        public void Draw()
        {
            Bitmap buffer = buffers[currentBufferIndex]; // Get the current buffer

            // Lock the bitmap's bits to allow direct access
            Rectangle rect = new Rectangle(0, 0, buffer.Width, buffer.Height);
            BitmapData bmpData = buffer.LockBits(rect, ImageLockMode.ReadWrite, buffer.PixelFormat);

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;

                    for (int tileY = 0; tileY < tiles.GetLength(1); tileY++)
                    {
                        int yStart = tileY * PixelHeight;
                        int yEnd = Math.Min(yStart + PixelHeight, bmpData.Height);

                        for (int tileX = 0; tileX < tiles.GetLength(0); tileX++)
                        {
                            int xStart = tileX * PixelWidth;
                            int xEnd = Math.Min(xStart + PixelWidth, bmpData.Width);

                            byte r = tiles[tileX, tileY, 0];
                            byte g = tiles[tileX, tileY, 1];
                            byte b = tiles[tileX, tileY, 2];

                            unsafe
                            {
                                byte* basePtr = (byte*)bmpData.Scan0;
                                int stride = bmpData.Stride;
                                for (int y = yStart; y < yEnd; ++y)
                                {
                                    byte* row = basePtr + y * stride;
                                    // compute xStart/xEnd once, then copy using pointer arithmetic
                                    for (int x = xStart; x < xEnd; ++x)
                                    {
                                        int idx = x * 4;
                                        row[idx + 2] = r; // R
                                        row[idx + 1] = g;
                                        row[idx] = b;
                                        row[idx + 3] = 255;
                                    }
                                }
                            }

                        }
                    }
                }
            }
            finally
            {
                // Unlock the bitmap's bits when done
                buffer.UnlockBits(bmpData);
            }

            // Switch to the next buffer for the next frame
            currentBufferIndex = (currentBufferIndex + 1) % 3;
        }

    }
}
