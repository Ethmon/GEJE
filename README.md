# 3D Graphics Engine From Scratch

A custom 3D graphics engine built entirely in C# without using any game engines or graphics libraries. The engine implements real-time 3D rendering, perspective projection, and multi-threaded architecture to create a fully functional 3D visualization system.


## Core Features

The engine implements:
- Real-time 3D rendering with perspective projection
- Custom matrix mathematics for 3D transformations
- Multi-threaded architecture for parallel processing
- Triple buffering system with direct memory access
- Depth sorting and backface culling
- JSON-based 3D model loading
- First-person camera controls

## Technical Implementation

### Efficient Rendering Pipeline
```csharp
unsafe
{
    byte* ptr = (byte*)bmpData.Scan0;
    for (int y = 0; y < bmpData.Height; y++)
    {
        for (int x = 0; x < bmpData.Width; x++)
        {
            // Direct pixel manipulation for maximum efficiency
        }
    }
}
```

### 3D Mathematics
```csharp
public static double[,] GetRotationMatrixY(double angle)
{
    double cosTheta = Math.Cos(angle);
    double sinTheta = Math.Sin(angle);

    return new double[,] {
        {cosTheta, 0, sinTheta, 0},
        {0, 1, 0, 0},
        {-sinTheta, 0, cosTheta, 0},
        {0, 0, 0, 1}
    };
}
```

### Multi-threaded Architecture
```csharp
public void Update()
{
    Thread renderThread = new Thread(new ThreadStart(RenderLoop));
    Thread updateThread = new Thread(new ThreadStart(UpdateLoop));
    renderThread.Start();
    updateThread.Start();
}
```

## Technical Challenges Overcome

### Performance Optimization
Initial implementation suffered from severe performance bottlenecks. Solved through:
- Direct memory access for pixel manipulation
- Triple buffering system implementation
- Matrix calculation optimization
Result: Achieved stable 60+ FPS with complex scenes

### Depth Management
Implementing proper depth sorting required:
- Efficient depth sorting algorithm
- Backface culling implementation
- Robust projection matrix system
Result: Accurate rendering of overlapping objects in 3D space

## Project Structure
```
GraphicsEngine/
├── src/
│   ├── Camera.cs        # Camera and projection handling
│   ├── Item.cs         # Base object management
│   ├── Mesh.cs         # 3D model processing
│   ├── Movement.cs     # Object movement control
│   ├── Point.cs        # Core geometry
│   ├── Program.cs      # Entry point
│   ├── Proportie.cs    # Transform properties
│   ├── Rotation.cs     # Matrix operations
│   ├── Screen.cs       # Rendering system
│   └── ThreeDSceen.cs  # Scene management
├── models/
│   ├── Cube.json       # Sample 3D models
│   └── Prisim.json
├── GEJE.csproj
├── packages.config
└── README.md
```

## Development Requirements
- Visual Studio 2019 or later
- .NET Framework 4.7.2
- Windows OS (7 or later)

## Build Instructions

1. Clone the repository
```bash
git clone https://github.com/Ethmon/GEJE.git
cd graphics-engine
```

2. Open the solution in Visual Studio
```bash
start GEJE.sln
```

3. Restore NuGet packages
```bash
nuget restore GEJE.sln
```

4. Build and run
- Press F5 to build and run in debug mode
- Or use Build > Build Solution from the menu

## Usage
1. Controls:
   - WASD: Camera movement
   - Arrow Keys: Camera rotation
   - ESC: Exit application

2. Loading Models:
   - Place JSON model files in the models directory
   - Format: Array of vertex coordinates and RGB colors
   ```json
   [
     [x, y, z, r, g, b],
     // Additional vertices...
   ]
   ```

## Performance Notes
- Rendering optimized for modern multi-core processors
- Direct memory access for efficient pixel manipulation
- Triple buffering eliminates screen tearing
- Efficient matrix operations for 3D transformations

## Future Development
- Shader system implementation
- Texture mapping support
- SIMD optimization for matrix operations
- Spatial partitioning for large scenes
- Physics system integration
- Normal mapping and advanced lighting

## Dependencies
- System.Drawing.Common (8.0.0)
- System.Text.Json (8.0.0)
- Additional dependencies listed in packages.config

## License
MIT License - See LICENSE file for details

## Contact
Ethan
- GitHub: TEthmon
- Email: Ethan6Salu@gmail.com


Built with C# and a passion for learning from scratch.
