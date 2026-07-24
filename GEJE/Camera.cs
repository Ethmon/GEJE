using GEJE;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

public class Camera : Proportie
{
    public double near = 0.1;      // must be > 0 for a valid projection matrix
    public double far = 900000000;
    public double fov = 80;        // degrees
    private double aspectRatio;

    private Window screen;
    private ThreeDSceen scene;

    private Dictionary<int, Mesh> tagToPolygon = new Dictionary<int, Mesh>();
    private Dictionary<Mesh, int> meshToTag = new Dictionary<Mesh, int>();
    private Dictionary<Mesh, Item> meshToItem = new Dictionary<Mesh, Item>();

    public Dictionary<Mesh, Item> ItemMap => meshToItem;
    public Dictionary<int, Mesh> TagMap => tagToPolygon;

    public Camera(double x, double y, double z, double xrot, double yrot, double zrot,
                  ThreeDSceen scene, Window screen, double aspectRatio)
        : base(x, y, z, xrot, yrot, zrot)
    {
        this.x = x; this.y = y; this.z = z;
        this.xrot = xrot; this.yrot = yrot; this.zrot = zrot;
        this.nx = x; this.ny = y; this.nz = z;
        this.nxrot = xrot; this.nyrot = yrot; this.nzrot = zrot;
        this.scene = scene;
        this.screen = screen;
        this.aspectRatio = aspectRatio;
    }


    public override void Update()
    {
        this.nxrot = Rotation.WrapAngle(this.nxrot);
        this.nyrot = Rotation.WrapAngle(this.nyrot);
        this.nzrot = Rotation.WrapAngle(this.nzrot);
    }


    public void Render()
    {
        Matrix4 view = BuildViewMatrix();
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(
            (float)(fov * Math.PI / 180.0),
            (float)aspectRatio,
            (float)Math.Max(near, 0.01),
            (float)far);

        tagToPolygon.Clear();
        meshToTag.Clear();
        meshToItem.Clear();

        int tag = 1;
        foreach (Item item in scene.items)
        {
            double dx = item.x - nx, dy = item.y - ny, dz = item.z - nz;
            double distSq = dx * dx + dy * dy + dz * dz;
            if (!(distSq < far && distSq > near))
                continue;

            foreach (object propertie in item.properties)
            {
                if (propertie is Mesh mesh)
                {
                    if (mesh.points.Count == 0)
                        continue;

                    tagToPolygon[tag] = mesh;
                    meshToTag[mesh] = tag;
                    meshToItem[mesh] = item;

                    var buffer = screen.GetOrUpdateMeshBuffer(mesh);
                    screen.DrawMesh(buffer, view, proj, tag);

                    tag++;
                }
            }
        }
    }

    private Matrix4 BuildViewMatrix()
    {

        float pitchRad = (float)(-nxrot * Math.PI / 180.0);
        float yawRad = (float)(-nyrot * Math.PI / 180.0);

        Matrix4 pitch = Matrix4.CreateRotationX(pitchRad);
        Matrix4 yaw = Matrix4.CreateRotationY(yawRad);
        Matrix4 rotation = yaw * pitch;



        Matrix4 translation = Matrix4.CreateTranslation((float)-nx, (float)-ny, (float)-nz);

        return translation * rotation;
    }
}