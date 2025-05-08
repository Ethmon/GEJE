using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace GEJE
{
    public class Window : Form
    {
        private byte[,,] tiles;
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
        public ISet<int> pressed = new HashSet<int>();

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
                }
            }
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
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
                tiles[x, y, 0] = r;
                tiles[x, y, 1] = g;
                tiles[x, y, 2] = b;
            }
        }
        public void QPlaceColor(int x, int y, byte r, byte g, byte b)
        {
            if (x < Ethwidth && x >= 0 && y < Ethheight && y >= 0 && tiles[x, y, 0] == 255 && tiles[x, y, 1] == 255 && tiles[x,y,2]==255)
            {
                tiles[x, y, 0] = r;
                tiles[x, y, 1] = g;
                tiles[x, y, 2] = b;
            }
        }


        unsafe void Clear()
        {
            fixed (byte* ptr = &tiles[0, 0, 0])
            {
                for (int i = 0; i < Ethwidth * Ethheight * 3; i++)
                {
                    ptr[i] = 255;
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
                    byte* ptr = (byte*)bmpData.Scan0; // Get the pointer to the start of the bitmap data
                    int index = 0;
                    int tileX = 0;
                    int tileY = 0;
                    // Iterate through the pixels and set their color directly in memory
                    for (int y = 0; y < bmpData.Height; y++)
                    {
                        for (int x = 0; x < bmpData.Width; x++)
                        {
                             index = y * bmpData.Stride + x * 4; // Calculate the index of the current pixel

                            // Calculate the corresponding tile coordinates
                            tileX = x / PixelWidth;
                            tileY = y / PixelHeight;

                            // Set the pixel color based on your tiles array
                            ptr[index + 2] = tiles[tileX, tileY, 0]; // Red component
                            ptr[index + 1] = tiles[tileX, tileY, 1]; // Green component
                            ptr[index] = tiles[tileX, tileY, 2];     // Blue component
                            ptr[index + 3] = 255; // Alpha (assuming 32-bit ARGB format)
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
            currentBufferIndex = (currentBufferIndex + 1) % 2;
        }

    }
}
