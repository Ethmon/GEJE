using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace GEJE
{
    public class Window : Form
    {
        private int[,,] tiles;
        private Bitmap[] buffers;
        private int currentBufferIndex;
        private readonly object graphicsLock = new object();
        private readonly object RenderLock = new object();
        public int Ethwidth;
        public int Ethheight;

        public int Ethsize(bool worh)
        {
            if (worh)
                return Ethwidth;
            else
                return Ethheight;
        }

        public Window(int ewidth, int ehight)
        {
            this.Ethwidth = ewidth;
            this.Ethheight = ehight;
            this.Text = "Game";
            this.Size = new Size(Ethwidth * 2, Ethheight * 2);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            tiles = new int[Ethwidth, Ethheight, 3];
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
        }

        public void Run()
        {
            Thread video_rendering = new Thread(new ThreadStart(UpdateLoop));
            video_rendering.Start();
            Application.Run(this);
        }
        public void update()
        {

            //Thread video_rendering = new Thread(new ThreadStart(UpdateLoop));
            //video_rendering.Start();
        }

        public void PlaceColor(int x, int y, int r, int g, int b)
        {
            if (x < Ethwidth && x >= 0 && y < Ethheight && y >= 0)
            {
                tiles[x, y, 0] = r;
                tiles[x, y, 1] = g;
                tiles[x, y, 2] = b;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < Ethwidth; i++)
            {
                for (int j = 0; j < Ethheight; j++)
                {
                    tiles[i, j, 0] = 255;
                    tiles[i, j, 1] = 255;
                    tiles[i, j, 2] = 255;
                }
            }
        }

        private void UpdateLoop()
        {
            while (true)
            {
                {
                    //var watch = Stopwatch.StartNew();
                    //watch.Start();
                    Draw();
                    this.Invoke(new MethodInvoker(() =>
                    {
                        using (Graphics g = this.CreateGraphics())
                        {
                            g.DrawImage(buffers[currentBufferIndex], 0, 0);
                        }
                    }));

                    //Thread.Sleep(4);
                    Clear();
                    //watch.Stop();
                    //Console.WriteLine(watch.ElapsedMilliseconds);
                }
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
                    byte* ptr = (byte*)bmpData.Scan0; // Get the pointer to the start of the bitmap data

                    // Iterate through the pixels and set their color directly in memory
                    for (int y = 0; y < bmpData.Height; y++)
                    {
                        for (int x = 0; x < bmpData.Width; x++)
                        {
                            int index = y * bmpData.Stride + x * 4; // Calculate the index of the current pixel

                            // Set the pixel color based on your tiles array
                            ptr[index + 2] = (byte)tiles[x / 2, y / 2, 0]; // Red component
                            ptr[index + 1] = (byte)tiles[x / 2, y / 2, 1]; // Green component
                            ptr[index] = (byte)tiles[x / 2, y / 2, 2];     // Blue component
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
            currentBufferIndex = (currentBufferIndex + 1) % 3;
        }

    }
}
