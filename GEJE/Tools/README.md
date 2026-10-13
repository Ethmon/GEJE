# GEJE Tools

## MeshWorkshop.html: build models in your browser

Double-click `MeshWorkshop.html` to open it in any browser. Nothing to install, and it works offline.

- Build models from shapes (box, sphere, cylinder, stairs, arch, capsule, star and more).
- Reshape them by extruding, insetting, subdividing and moving individual faces and points.
- Draw shapes:
  - **Lathe** spins a side outline into vases, bottles and pillars.
  - **Outline** gives a flat drawing thickness, for logos, letters and signs.
- Make terrain from a heightmap image, and trace over a reference picture.
- Click **Guide** inside the tool for the full walkthrough and keyboard shortcuts.

### Getting a model into GEJE

1. Click **Export for GEJE**, then **Download**, and save the file into `GEJE/Models/`.
2. Add it to your scene in `CodeFile1.cs` (the export window writes this for you):

```csharp
Item lamp = new Item(0, 0, 0, 90, 0, 0);
Mesh lampMesh = new Mesh("Models\\Lamp.json", 0, 0, 0, 0, 0, 0);
lampMesh.scale(20, 20, 20);
lamp.add_propertie(lampMesh);
sceen.add_item(lamp);
```

### Saving your work

- **File → Save project** saves a `.gejeproj.json` file that keeps every part editable. Use File → Open project to continue later.
- The exported model file is the finished triangles, so keep the project file too if you want to make changes.

### Conventions

- Models are built with **Z up**, sitting on z = 0, about 20 units across. Items use `xrot = 90` to stand them upright.
- The engine has no lighting, so the tool darkens faces that point away from a fixed sun and saves those colors into the file.

## mesh_gen.py: generate models with code

`python mesh_gen.py` writes a set of ready-made models into `../Models`. Add your own shapes by writing a function and listing it in `MODELS` at the bottom of the file.
