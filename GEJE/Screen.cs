using System;
//using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GEJE
{
    public class Window : GameWindow
    {
        private byte[,,] tiles;
        private bool[,] bools;

        private int textureId;
        public int Ethwidth, Ethheight;
        private int PixelWidth, PixelHeight;
        public ThreeDSceen scene;
        public Camera cam;

        public ISet<int> pressed = new HashSet<int>();
        public bool right_click = false;
        public bool left_click = false;

        // OpenGL shader / quad resources

        private int shaderProgram;
        private int quadVao;
        private int quadVbo;

        public Window(int ewidth, int eheight, int pixelWidth, int pixelHeight)
            : base(GameWindowSettings.Default,
                  new NativeWindowSettings()
                  {
                      Size = new Vector2i(ewidth * pixelWidth, eheight * pixelHeight),
                      Title = "GEJE (GPU Window)"
                  })
        {
            Ethwidth = ewidth;
            Ethheight = eheight;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;

            tiles = new byte[Ethwidth, Ethheight, 3];
            bools = new bool[Ethwidth, Ethheight];

            for (int i = 0; i < Ethwidth; i++)
                for (int j = 0; j < Ethheight; j++)
                {
                    tiles[i, j, 0] = 255;
                    tiles[i, j, 1] = 255;
                    tiles[i, j, 2] = 255;
                    bools[i, j] = false;
                }
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Create texture
            textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            // allocate GPU memory for the texture (no data yet)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Ethwidth, Ethheight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Compile shader
            shaderProgram = CreateProgram(fullscreenVertexShaderSource, fullscreenFragmentShaderSource);

            // Fullscreen quad (two triangles). Each vertex: vec2 position, vec2 uv
            float[] quadVerts = new float[]
            {
        // Triangle 1
        -1f, -1f,  0f, 0f,
         1f, -1f,  1f, 0f,
         1f,  1f,  1f, 1f,
        // Triangle 2
        -1f, -1f,  0f, 0f,
         1f,  1f,  1f, 1f,
        -1f,  1f,  0f, 1f
            };

            quadVao = GL.GenVertexArray();
            quadVbo = GL.GenBuffer();
            GL.BindVertexArray(quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVerts.Length * sizeof(float), quadVerts, BufferUsageHint.StaticDraw);

            int stride = 4 * sizeof(float); // 4 floats per vertex: pos.x,pos.y, u,v
                                            // position attribute (location = 0)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            // texcoord attribute (location = 1)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));

            // unbind
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }


        public void PlaceColor(int x, int y, byte r, byte g, byte b)
        {
            if (x >= 0 && x < Ethwidth && y >= 0 && y < Ethheight)
            {
                tiles[x, y, 0] = r;
                tiles[x, y, 1] = g;
                tiles[x, y, 2] = b;
            }
        }

        public void QPlaceColor(int x, int y, byte r, byte g, byte b)
        {
            if (x >= 0 && x < Ethwidth && y >= 0 && y < Ethheight && !bools[x, y])
            {
                tiles[x, y, 0] = r;
                tiles[x, y, 1] = g;
                tiles[x, y, 2] = b;
                bools[x, y] = true;
            }
        }

        

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Upload tiles -> GPU texture
            UploadToGPU();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shaderProgram);

            // bind texture to unit 0
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            int loc = GL.GetUniformLocation(shaderProgram, "uTex");
            if (loc >= 0) GL.Uniform1(loc, 0);

            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);

            SwapBuffers();

            // clear frame data for next frame (like before)
            Clear();
        }


        private unsafe void Clear()
        {
            int pixels = Ethwidth * Ethheight;
            fixed (byte* pTiles = &tiles[0, 0, 0])
            fixed (bool* pBools = &bools[0, 0])
            {
                byte* px = pTiles;
                for (int i = 0; i < pixels; i++)
                {
                    px[0] = 255; px[1] = 255; px[2] = 255;
                    px += 3;
                    pBools[i] = false;
                }
            }
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            if (quadVbo != 0) GL.DeleteBuffer(quadVbo);
            if (quadVao != 0) GL.DeleteVertexArray(quadVao);
            if (textureId != 0) GL.DeleteTexture(textureId);
            if (shaderProgram != 0) GL.DeleteProgram(shaderProgram);
        }



        private const string fullscreenVertexShaderSource = @"
#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUV;
out vec2 vUV;
void main()
{
    vUV = aUV;
    gl_Position = vec4(aPos.xy, 0.0, 1.0);
}
";

        private const string fullscreenFragmentShaderSource = @"
#version 330 core
in vec2 vUV;
out vec4 FragColor;
uniform sampler2D uTex;
void main()
{
    vec3 c = texture(uTex, vUV).rgb;
    FragColor = vec4(c, 1.0);
}
";

        private int CreateProgram(string vsSource, string fsSource)
        {
            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vsSource);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int success);
            if (success == 0) throw new Exception("Vertex shader compile error: " + GL.GetShaderInfoLog(vs));

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fsSource);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out success);
            if (success == 0) throw new Exception("Fragment shader compile error: " + GL.GetShaderInfoLog(fs));

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vs);
            GL.AttachShader(prog, fs);
            GL.LinkProgram(prog);
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out success);
            if (success == 0) throw new Exception("Program link error: " + GL.GetProgramInfoLog(prog));

            GL.DetachShader(prog, vs);
            GL.DetachShader(prog, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);

            return prog;
        }


        // call this instead of the previous UploadToGPU
        private unsafe void UploadToGPU(bool flipY = true)
        {
            // Ensure GL expects byte-aligned rows
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            int w = Ethwidth;
            int h = Ethheight;

            // Create single-dimensional buffer: rows of pixels, each pixel = R,G,B
            byte[] linear = new byte[w * h * 3];

            // Fill linear buffer in the order OpenGL expects:
            // for each row (y from 0..h-1) where y==0 is the BOTTOM row in texture space.
            // If your memory's origin is top-left, use flipY = true to flip rows during copy.
            if (flipY)
            {
                // GL expects bottom row first; our tiles probably have top row at y=0 -> write flipped
                for (int y = 0; y < h; y++)
                {
                    int srcY = h - 1 - y; // pick rows from bottom up
                    int rowStart = y * w * 3;
                    for (int x = 0; x < w; x++)
                    {
                        int dst = rowStart + x * 3;
                        // tiles indexed as tiles[x,y,channel]
                        linear[dst + 0] = tiles[x, srcY, 0]; // R
                        linear[dst + 1] = tiles[x, srcY, 1]; // G
                        linear[dst + 2] = tiles[x, srcY, 2]; // B
                    }
                }
            }
            else
            {
                // no vertical flip: write top-to-bottom
                for (int y = 0; y < h; y++)
                {
                    int rowStart = y * w * 3;
                    for (int x = 0; x < w; x++)
                    {
                        int dst = rowStart + x * 3;
                        linear[dst + 0] = tiles[x, y, 0];
                        linear[dst + 1] = tiles[x, y, 1];
                        linear[dst + 2] = tiles[x, y, 2];
                    }
                }
            }

            // Pin and upload
            fixed (byte* p = &linear[0])
            {
                GL.BindTexture(TextureTarget.Texture2D, textureId);
                GL.TexSubImage2D(TextureTarget.Texture2D,
                                 0, // level
                                 0, 0, // xoffset, yoffset
                                 w, h,
                                 PixelFormat.Rgb,
                                 PixelType.UnsignedByte,
                                 (IntPtr)p);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }
        public int[] currsor_pos()
        {
            // Get current mouse position in window coordinates
            var mouse = MouseState;
            int localX = (int)(mouse.X / PixelWidth);
            int localY = (int)(mouse.Y / PixelHeight);

            // Clamp to valid range
            localX = Math.Clamp(localX, 0, Ethwidth - 1);
            localY = Math.Clamp(localY, 0, Ethheight - 1);

            return new int[] { localX, localY };
        }


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButton.Left) left_click = true;
            if (e.Button == MouseButton.Right) right_click = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButton.Left) left_click = false;
            if (e.Button == MouseButton.Right) right_click = false;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            pressed.Add((int)e.Key);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            pressed.Remove((int)e.Key);
        }





        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            if (IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
                Close();
            scene.update();
        }

        public void RunGame()
        {
            Run();
        }
    }
}
