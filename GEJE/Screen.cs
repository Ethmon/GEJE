using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GEJE
{
    public class MeshGpuData
    {
        public int Vao;
        public int Vbo;
        public int VertexCount;
    }

    public class Window : GameWindow
    {
        private byte[,,] tiles;
        private bool[,] bools;
        public int[,] tag;

        public int Ethwidth, Ethheight;
        private int PixelWidth, PixelHeight;
        public ThreeDSceen scene;
        public Camera cam;

        public ISet<int> pressed = new HashSet<int>();
        public bool right_click = false;
        public bool left_click = false;


        private int blitShaderProgram;
        private int quadVao;
        private int quadVbo;


        private int sceneShaderProgram;
        private int uView, uProj, uTag;
        private Dictionary<Mesh, MeshGpuData> meshBuffers = new Dictionary<Mesh, MeshGpuData>();


        private int sceneFbo;
        private int sceneColorTex;
        private int sceneTagTex;
        private int sceneDepthRbo;

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
            tag = new int[Ethwidth, Ethheight];

            for (int i = 0; i < Ethwidth; i++)
                for (int j = 0; j < Ethheight; j++)
                {
                    tiles[i, j, 0] = 255;
                    tiles[i, j, 1] = 255;
                    tiles[i, j, 2] = 255;
                    bools[i, j] = false;
                    tag[i, j] = 0;
                }
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            SetupBlitQuad();
            SetupSceneShader();
            SetupSceneFramebuffer();
        }

        // ---------------- fullscreen blit quad (presents sceneColorTex to the window) ----------------
        
        
        private int uBlitTex, uBlitTagTex, uBlitTexelSize, uBlitOutlineThickness;
        public float OutlineThickness = 0.5f;
        private void SetupBlitQuad()
        {
            blitShaderProgram = CreateProgram(fullscreenVertexShaderSource, fullscreenFragmentShaderSource);
            
            uBlitTex = GL.GetUniformLocation(blitShaderProgram, "uTex");
            uBlitTagTex = GL.GetUniformLocation(blitShaderProgram, "uTagTex");
            uBlitTexelSize = GL.GetUniformLocation(blitShaderProgram, "uTexelSize");
            uBlitOutlineThickness = GL.GetUniformLocation(blitShaderProgram, "uOutlineThickness");
            float[] quadVerts = new float[]
            {
                -1f, -1f,  0f, 0f,
                 1f, -1f,  1f, 0f,
                 1f,  1f,  1f, 1f,
                -1f, -1f,  0f, 0f,
                 1f,  1f,  1f, 1f,
                -1f,  1f,  0f, 1f
            };

            quadVao = GL.GenVertexArray();
            quadVbo = GL.GenBuffer();
            GL.BindVertexArray(quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVerts.Length * sizeof(float), quadVerts, BufferUsageHint.StaticDraw);

            int stride = 4 * sizeof(float);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        // ---------------- 3D scene shader ----------------

        private void SetupSceneShader()
        {
            sceneShaderProgram = CreateProgram(sceneVertexShaderSource, sceneFragmentShaderSource);
            uView = GL.GetUniformLocation(sceneShaderProgram, "uView");
            uProj = GL.GetUniformLocation(sceneShaderProgram, "uProj");
            uTag = GL.GetUniformLocation(sceneShaderProgram, "uTag");
        }

        private void SetupSceneFramebuffer()
        {
            sceneFbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, sceneFbo);

            sceneColorTex = CreateAttachmentTexture();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, sceneColorTex, 0);

            sceneTagTex = CreateAttachmentTexture();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
                TextureTarget.Texture2D, sceneTagTex, 0);

            sceneDepthRbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, sceneDepthRbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, Ethwidth, Ethheight);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, sceneDepthRbo);

            GL.DrawBuffers(2, new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                throw new Exception("Scene framebuffer incomplete: " + status);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private int CreateAttachmentTexture()
        {
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Ethwidth, Ethheight, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return tex;
        }

        // ---------------- per-mesh GPU buffers ----------------

        public MeshGpuData GetOrUpdateMeshBuffer(Mesh mesh)
        {
            if (!meshBuffers.TryGetValue(mesh, out MeshGpuData data))
            {
                data = new MeshGpuData
                {
                    Vao = GL.GenVertexArray(),
                    Vbo = GL.GenBuffer()
                };
                meshBuffers[mesh] = data;
                mesh.Dirty = true;
            }

            if (mesh.Dirty)
            {
                List<Polygon> pointsSnapshot = mesh.points;

                int triCount = pointsSnapshot.Count;
                float[] verts = new float[triCount * 3 * 6]; // 3 verts/tri * (pos3 + color3)
                int i = 0;
                foreach (Polygon poly in pointsSnapshot)
                {
                    WriteVertex(verts, ref i, poly.p1);
                    WriteVertex(verts, ref i, poly.p2);
                    WriteVertex(verts, ref i, poly.p3);
                }

                GL.BindVertexArray(data.Vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, data.Vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.DynamicDraw);

                int stride = 6 * sizeof(float);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
                GL.BindVertexArray(0);

                data.VertexCount = triCount * 3;
                mesh.Dirty = false;
            }

            return data;
        }

        private static void WriteVertex(float[] arr, ref int i, Point p)
        {
            arr[i++] = (float)p.x;
            arr[i++] = (float)p.y;
            arr[i++] = (float)p.z;
            arr[i++] = p.r / 255f;
            arr[i++] = p.g / 255f;
            arr[i++] = p.b / 255f;
        }

        /// <summary>Issues one draw call for this mesh's buffer. Must be called with the scene FBO bound.</summary>
        public void DrawMesh(MeshGpuData data, Matrix4 view, Matrix4 proj, int tag)
        {
            GL.UseProgram(sceneShaderProgram);
            GL.UniformMatrix4(uView, false, ref view);
            GL.UniformMatrix4(uProj, false, ref proj);
            GL.Uniform1(uTag, tag);

            GL.BindVertexArray(data.Vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, data.VertexCount);
        }

        public void ReleaseMeshBuffer(Mesh mesh)
        {
            if (meshBuffers.TryGetValue(mesh, out MeshGpuData data))
            {
                GL.DeleteBuffer(data.Vbo);
                GL.DeleteVertexArray(data.Vao);
                meshBuffers.Remove(mesh);
            }
        }

        // ---------------- picking ----------------

        public int GetTagAt(int x, int y)
        {
            if (x < 0 || x >= Ethwidth || y < 0 || y >= Ethheight)
                return 0;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, sceneFbo);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
            byte[] pixel = new byte[4];
            GL.ReadPixels(x, Ethheight - 1 - y, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, pixel);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            return pixel[0] | (pixel[1] << 8);
        }

        // ---------------- legacy CPU tile API (unused by Camera now; kept for other callers) ----------------

        public void PlaceColor(int x, int y, byte r, byte g, byte b, int tag)
        {
            if (x >= 0 && x < Ethwidth && y >= 0 && y < Ethheight)
            {
                tiles[x, y, 0] = r;
                tiles[x, y, 1] = g;
                tiles[x, y, 2] = b;
                if (tag != -2)
                    this.tag[x, y] = tag;
            }
        }

        public void QPlaceColor(int x, int y, byte r, byte g, byte b, int tag)
        {
            if (x >= 0 && x < Ethwidth && y >= 0 && y < Ethheight && !bools[x, y])
            {
                tiles[x, y, 0] = r;
                tiles[x, y, 1] = g;
                tiles[x, y, 2] = b;
                bools[x, y] = true;
                this.tag[x, y] = tag;
            }
        }

        public void Cleartags()
        {
            Array.Clear(tag, 0, tag.Length);
        }

        public int getTagOfPixle(int x, int y) => GetTagAt(x, y);

        // ---------------- render loop ----------------

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, sceneFbo);
            GL.Viewport(0, 0, Ethwidth, Ethheight);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 1f, 1f, 1f, 1f });
            GL.ClearBuffer(ClearBuffer.Color, 1, new float[] { 0f, 0f, 0f, 1f });
            GL.Clear(ClearBufferMask.DepthBufferBit);

            cam.Render(); 

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            
            GL.Disable(EnableCap.DepthTest);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(blitShaderProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, sceneColorTex);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, sceneTagTex);

            if (uBlitTex >= 0) GL.Uniform1(uBlitTex, 0);
            if (uBlitTagTex >= 0) GL.Uniform1(uBlitTagTex, 1);
            if (uBlitTexelSize >= 0) GL.Uniform2(uBlitTexelSize, 1f / Ethwidth, 1f / Ethheight);
            if (uBlitOutlineThickness >= 0) GL.Uniform1(uBlitOutlineThickness, OutlineThickness);

            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            if (IsKeyDown(Keys.Escape))
                Close();

            scene.update();  
        }

        // ---------------- shaders ----------------

        private const string sceneVertexShaderSource = @"
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aColor;
out vec3 vColor;
uniform mat4 uView;
uniform mat4 uProj;
void main()
{
    vColor = aColor;
    gl_Position = uProj * uView * vec4(aPos, 1.0);
}
";

        private const string sceneFragmentShaderSource = @"
#version 330 core
in vec3 vColor;
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outTag;
uniform int uTag;
void main()
{
    outColor = vec4(vColor, 1.0);
    float r = float(uTag & 0xFF) / 255.0;
    float g = float((uTag >> 8) & 0xFF) / 255.0;
    outTag = vec4(r, g, 0.0, 1.0);
}
";

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
uniform sampler2D uTagTex;
uniform vec2 uTexelSize;        // 1.0 / Ethwidth, 1.0 / Ethheight
uniform float uOutlineThickness; // in texels — try 1.0 to start

int decodeTag(vec2 uv)
{
    vec4 t = texture(uTagTex, uv);
    int r = int(round(t.r * 255.0));
    int g = int(round(t.g * 255.0));
    return r | (g << 8);
}

void main()
{
    int centerTag = decodeTag(vUV);
    vec2 o = uTexelSize * uOutlineThickness;

    bool edge =
        decodeTag(vUV + vec2( o.x,  0.0)) != centerTag ||
        decodeTag(vUV + vec2(-o.x,  0.0)) != centerTag ||
        decodeTag(vUV + vec2( 0.0,  o.y)) != centerTag ||
        decodeTag(vUV + vec2( 0.0, -o.y)) != centerTag ||
        decodeTag(vUV + vec2( o.x,  o.y)) != centerTag ||
        decodeTag(vUV + vec2(-o.x,  o.y)) != centerTag ||
        decodeTag(vUV + vec2( o.x, -o.y)) != centerTag ||
        decodeTag(vUV + vec2(-o.x, -o.y)) != centerTag;

    if (edge)
        FragColor = vec4(0.0, 0.0, 0.0, 1.0);
    else
        FragColor = vec4(texture(uTex, vUV).rgb, 1.0);
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

        // ---------------- input ----------------

        public int[] currsor_pos()
        {
            var mouse = MouseState;
            int localX = (int)(mouse.X / PixelWidth);
            int localY = (int)(mouse.Y / PixelHeight);
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

        public void RunGame() => Run();
    }
}