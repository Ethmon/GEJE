"""
GEJE mesh generator.

Writes model files in GEJE's JSON format: a flat list of vertices,
[x, y, z, r, g, b], three vertices per triangle.

Conventions (matched to the existing Models/*.json, e.g. Pyramid.json):
  * +Z is "up" for the model (items are placed with xrot = 90 in CodeFile1.cs).
  * Models sit on z = 0 and are roughly 20 units across (-10..10),
    the same scale as FullBox.json, so .scale(...) values you already use still make sense.
  * Triangles are wound counter-clockwise when seen from outside,
    so they keep working if you ever turn on GL.Enable(EnableCap.CullFace).

GEJE's fragment shader has no lighting, so a single-color mesh renders as a flat
silhouette. This script bakes simple directional lighting into each triangle's color
(flat shading), so every face gets its own shade and the shape reads as 3D.

Usage:  python mesh_gen.py [output_dir]      (default: ../Models)
"""
import json, math, os, sys

LIGHT = (0.45, -0.35, 0.82)          # direction *towards* the light, z-up
_l = math.sqrt(sum(c * c for c in LIGHT)); LIGHT = tuple(c / _l for c in LIGHT)
AMBIENT = 0.42


def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def dot(a, b): return a[0]*b[0] + a[1]*b[1] + a[2]*b[2]
def norm(a):
    l = math.sqrt(dot(a, a)) or 1.0
    return (a[0]/l, a[1]/l, a[2]/l)


class Mesh:
    def __init__(self): self.tris = []          # (p1, p2, p3, (r,g,b))

    def tri(self, a, b, c, col): self.tris.append((a, b, c, col))

    def quad(self, a, b, c, d, col):            # a-b-c-d counter-clockwise
        self.tri(a, b, c, col); self.tri(a, c, d, col)

    def add(self, other, offset=(0, 0, 0)):
        ox, oy, oz = offset
        for a, b, c, col in other.tris:
            self.tris.append(tuple((p[0]+ox, p[1]+oy, p[2]+oz) for p in (a, b, c)) + (col,))
        return self

    def to_json(self, shade=True):
        rows = []
        for a, b, c, col in self.tris:
            k = 1.0
            if shade:
                n = norm(cross(sub(b, a), sub(c, a)))
                k = AMBIENT + (1 - AMBIENT) * max(0.0, dot(n, LIGHT))
            rgb = [max(0, min(255, int(round(ch * k)))) for ch in col]
            for p in (a, b, c):
                rows.append([round(p[0], 5), round(p[1], 5), round(p[2], 5)] + rgb)
        return "[\n" + ",\n".join("  " + json.dumps(r) for r in rows) + "\n]\n"


# ---------------------------------------------------------------- primitives

def box(x0, y0, z0, x1, y1, z1, col, faces="tbnsew"):
    """Axis-aligned box. faces: t=top b=bottom n=+y s=-y e=+x w=-x."""
    m = Mesh()
    p = lambda i, j, k: ((x0, x1)[i], (y0, y1)[j], (z0, z1)[k])
    c = col if isinstance(col, dict) else {f: col for f in "tbnsew"}
    if "t" in faces: m.quad(p(0,0,1), p(1,0,1), p(1,1,1), p(0,1,1), c["t"])
    if "b" in faces: m.quad(p(0,0,0), p(0,1,0), p(1,1,0), p(1,0,0), c["b"])
    if "s" in faces: m.quad(p(0,0,0), p(1,0,0), p(1,0,1), p(0,0,1), c["s"])
    if "n" in faces: m.quad(p(1,1,0), p(0,1,0), p(0,1,1), p(1,1,1), c["n"])
    if "e" in faces: m.quad(p(1,0,0), p(1,1,0), p(1,1,1), p(1,0,1), c["e"])
    if "w" in faces: m.quad(p(0,1,0), p(0,0,0), p(0,0,1), p(0,1,1), c["w"])
    return m


def cylinder(r, h, seg, col, z0=0.0, r_top=None, caps=True, cap_col=None):
    """Cylinder/frustum around the z axis. r_top=0 gives a cone."""
    m = Mesh(); rt = r if r_top is None else r_top; cap_col = cap_col or col
    for i in range(seg):
        a0 = 2*math.pi*i/seg; a1 = 2*math.pi*(i+1)/seg
        b0 = (r*math.cos(a0), r*math.sin(a0), z0); b1 = (r*math.cos(a1), r*math.sin(a1), z0)
        t0 = (rt*math.cos(a0), rt*math.sin(a0), z0+h); t1 = (rt*math.cos(a1), rt*math.sin(a1), z0+h)
        if rt > 0: m.quad(b0, b1, t1, t0, col)
        else:      m.tri(b0, b1, t0, col)
        if caps:
            m.tri((0, 0, z0), b1, b0, cap_col)
            if rt > 0: m.tri((0, 0, z0+h), t0, t1, cap_col)
    return m


def uv_sphere(r, rings, seg, col_fn, center=(0, 0, 0), squash=(1, 1, 1)):
    """col_fn(lat_index, lon_index) -> (r,g,b)."""
    m = Mesh(); cx, cy, cz = center; sx, sy, sz = squash
    def P(i, j):
        th = math.pi * i / rings; ph = 2*math.pi*j/seg
        return (cx + sx*r*math.sin(th)*math.cos(ph), cy + sy*r*math.sin(th)*math.sin(ph), cz + sz*r*math.cos(th))
    for i in range(rings):
        for j in range(seg):
            c = col_fn(i, j)
            a, b, cc, d = P(i, j), P(i+1, j), P(i+1, j+1), P(i, j+1)
            if i == 0:            m.tri(a, b, cc, c)
            elif i == rings - 1:  m.tri(a, b, d, c)
            else:                 m.quad(a, b, cc, d, c)
    return m


def icosphere(r, subdiv, col_fn, center=(0, 0, 0), jitter=None):
    """col_fn(face_center) -> (r,g,b). jitter(vertex)->scale factor for lumpy rocks."""
    t = (1 + math.sqrt(5)) / 2
    v = [norm(p) for p in [(-1,t,0),(1,t,0),(-1,-t,0),(1,-t,0),(0,-1,t),(0,1,t),(0,-1,-t),(0,1,-t),(t,0,-1),(t,0,1),(-t,0,-1),(-t,0,1)]]
    f = [(0,11,5),(0,5,1),(0,1,7),(0,7,10),(0,10,11),(1,5,9),(5,11,4),(11,10,2),(10,7,6),(7,1,8),
         (3,9,4),(3,4,2),(3,2,6),(3,6,8),(3,8,9),(4,9,5),(2,4,11),(6,2,10),(8,6,7),(9,8,1)]
    for _ in range(subdiv):
        cache = {}; nf = []
        def mid(i, j):
            key = (min(i, j), max(i, j))
            if key not in cache:
                cache[key] = len(v); v.append(norm(tuple((v[i][k]+v[j][k])/2 for k in range(3))))
            return cache[key]
        for a, b, c in f:
            ab, bc, ca = mid(a, b), mid(b, c), mid(c, a)
            nf += [(a, ab, ca), (b, bc, ab), (c, ca, bc), (ab, bc, ca)]
        f = nf
    s = [jitter(p) if jitter else 1.0 for p in v]
    pts = [(center[0]+p[0]*r*k, center[1]+p[1]*r*k, center[2]+p[2]*r*k) for p, k in zip(v, s)]
    m = Mesh()
    for a, b, c in f:
        fc = tuple((pts[a][k]+pts[b][k]+pts[c][k])/3 for k in range(3))
        m.tri(pts[a], pts[b], pts[c], col_fn(fc))
    return m


def torus(R, r, seg, tube, col_fn, z=0.0):
    m = Mesh()
    def P(i, j):
        u = 2*math.pi*i/seg; w = 2*math.pi*j/tube
        return ((R + r*math.cos(w))*math.cos(u), (R + r*math.cos(w))*math.sin(u), z + r*math.sin(w))
    for i in range(seg):
        for j in range(tube):
            m.quad(P(i, j), P(i+1, j), P(i+1, j+1), P(i, j+1), col_fn(i, j))
    return m


def hash01(*a):
    """Deterministic pseudo-random 0..1 so the files are the same every run."""
    h = 2166136261
    for x in a:
        h = ((h ^ (int(x * 1000) & 0xffffffff)) * 16777619) & 0xffffffff
    return (h % 10007) / 10007


# ---------------------------------------------------------------- models

def m_cube():
    s = 10
    return box(-s, -s, 0, s, s, 2*s, {"t": (220, 70, 70), "b": (70, 70, 220), "n": (70, 200, 90),
                                      "s": (240, 200, 60), "e": (230, 130, 40), "w": (160, 80, 200)})

def m_sphere():
    return uv_sphere(10, 12, 20, lambda i, j: (60, 140, 230) if (i + j) % 2 else (240, 240, 245), center=(0, 0, 10))

def m_icosphere():
    return icosphere(10, 2, lambda c: (90, 200, 170), center=(0, 0, 10))

def m_cylinder():
    return cylinder(8, 20, 24, (200, 200, 210), cap_col=(150, 150, 160))

def m_cone():
    return cylinder(10, 20, 24, (250, 130, 30), r_top=0)

def m_torus():
    return torus(7, 3, 32, 14, lambda i, j: (230, 190, 40), z=3)

def m_crate():
    wood, dark = (180, 130, 70), (120, 82, 40)
    m = box(-9, -9, 0.5, 9, 9, 19.5, wood)
    e = 1.2  # raised edge planks around every edge
    for (x0, y0, x1, y1) in [(-10,-10,-10+e*2,-10+e*2),(10-e*2,-10,10,-10+e*2),(-10,10-e*2,-10+e*2,10),(10-e*2,10-e*2,10,10)]:
        m.add(box(x0, y0, 0, x1, y1, 20, dark))
    for z0 in (0, 20 - e*2):
        m.add(box(-10, -10, z0, 10, -10+e*2, z0+e*2, dark)); m.add(box(-10, 10-e*2, z0, 10, 10, z0+e*2, dark))
        m.add(box(-10, -10, z0, -10+e*2, 10, z0+e*2, dark)); m.add(box(10-e*2, -10, z0, 10, 10, z0+e*2, dark))
    return m

def m_tree():
    m = cylinder(1.6, 8, 8, (110, 72, 40), cap_col=(90, 60, 35))
    for i, (r, z, h) in enumerate([(9, 6, 9), (7, 11, 8), (4.5, 16, 7)]):
        g = (40 + i*12, 140 + i*15, 55 + i*8)
        m.add(cylinder(r, h, 10, g, z0=z, r_top=0, cap_col=(30, 100, 40)))
    return m

def m_rock():
    def jit(p): return 0.78 + 0.35 * hash01(p[0]*7.1, p[1]*3.3, p[2]*5.9)
    def col(c): g = 120 + int(40 * hash01(c[0], c[1], c[2])); return (g, g - 5, g - 15)
    m = icosphere(9, 1, col, center=(0, 0, 0), jitter=jit)
    out = Mesh()                                   # flatten the bottom so it sits on the ground
    for a, b, c, cl in m.tris:
        out.tri(*[(p[0], p[1], max(p[2]*0.75 + 4.5, 0)) for p in (a, b, c)], cl)
    return out

def m_house():
    wall, roof, door, win, trim = (225, 210, 180), (160, 50, 40), (100, 60, 30), (120, 180, 230), (240, 240, 235)
    m = box(-10, -8, 0, 10, 8, 12, wall, faces="nsew")
    # gable roof (ridge along x)
    rz, ov = 19, 1.5
    a, b = (-10-ov, -8-ov, 12), (10+ov, -8-ov, 12)
    c, d = (10+ov, 8+ov, 12), (-10-ov, 8+ov, 12)
    r0, r1 = (-10-ov, 0, rz), (10+ov, 0, rz)
    m.quad(a, b, r1, r0, roof); m.quad(c, d, r0, r1, roof)
    m.tri(b, c, r1, roof); m.tri(d, a, r0, roof)         # roof underside-ends close the overhang
    m.tri((10, -8, 12), (10, 8, 12), (10, 0, rz - 1), wall)   # gable walls
    m.tri((-10, 8, 12), (-10, -8, 12), (-10, 0, rz - 1), wall)
    # door and windows on the -y (front) face, pushed slightly out to avoid z-fighting
    f = -8.05
    m.add(box(-2, f - .1, 0, 2, f, 7, door, faces="s"))
    for x in (-7, 4.5):
        m.add(box(x - .4, f - .15, 4.6, x + 3.4, f, 9.4, trim, faces="s"))
        m.add(box(x, f - .25, 5, x + 3, f - .1, 9, win, faces="s"))
    m.add(box(5, -2, 14, 7, 0, 21, (130, 120, 115)))     # chimney
    return m

def m_coin():
    return cylinder(8, 1.5, 28, (240, 190, 40), z0=0, cap_col=(255, 215, 70))

def m_column():
    stone = (215, 210, 200)
    m = box(-5, -5, 0, 5, 5, 2, stone)
    m.add(cylinder(3.2, 22, 16, stone, z0=2, caps=False))
    m.add(box(-5, -5, 24, 5, 5, 26, stone))
    return m

def m_heart():
    """Extruded heart, like a pickup."""
    pts = []
    for i in range(40):
        t = 2*math.pi*i/40
        pts.append((0.6*16*math.sin(t)**3, 0, 0.6*(13*math.cos(t) - 5*math.cos(2*t) - 2*math.cos(3*t) - math.cos(4*t)) + 10))
    m = Mesh(); red, side = (230, 40, 70), (200, 30, 55); d = 2
    cz = 10
    for i in range(len(pts)):
        p, q = pts[i], pts[(i+1) % len(pts)]
        m.tri((0, -d, cz), (q[0], -d, q[2]), (p[0], -d, p[2]), red)
        m.tri((0, d, cz), (p[0], d, p[2]), (q[0], d, q[2]), red)
        m.quad((p[0], -d, p[2]), (q[0], -d, q[2]), (q[0], d, q[2]), (p[0], d, p[2]), side)
    return m

def m_terrain():
    m = Mesh(); n, size = 12, 40
    def h(i, j):
        x, y = i / n, j / n
        return 3*math.sin(x*6.0)*math.cos(y*5.0) + 2*hash01(i, j) + 1.5*math.sin((x+y)*9)
    def P(i, j): return (-size/2 + size*i/n, -size/2 + size*j/n, h(i, j))
    for i in range(n):
        for j in range(n):
            a, b, c, d = P(i, j), P(i+1, j), P(i+1, j+1), P(i, j+1)
            avg = (a[2] + b[2] + c[2] + d[2]) / 4
            col = (70, 150, 60) if avg < 3 else (120, 160, 70) if avg < 5 else (150, 140, 120)
            m.quad(a, b, c, d, col)
    return m

def m_stairs():
    m = Mesh(); col = (190, 185, 175)
    for i in range(5):
        m.add(box(-8, -10 + i*4, 0, 8, 10, (i+1)*4, col))
    return m

def m_arch():
    stone = (170, 165, 160)
    m = box(-10, -3, 0, -6, 3, 12, stone).add(box(6, -3, 0, 10, 3, 12, stone))
    seg = 12; R, r = 10, 6
    for i in range(seg):
        a0 = math.pi*i/seg; a1 = math.pi*(i+1)/seg
        o0 = (R*math.cos(a0), R*math.sin(a0)+12); o1 = (R*math.cos(a1), R*math.sin(a1)+12)
        i0 = (r*math.cos(a0), r*math.sin(a0)+12); i1 = (r*math.cos(a1), r*math.sin(a1)+12)
        P = lambda q, y: (q[0], y, q[1])
        m.quad(P(i0,-3), P(o0,-3), P(o1,-3), P(i1,-3), stone)      # front
        m.quad(P(o0, 3), P(i0, 3), P(i1, 3), P(o1, 3), stone)      # back
        m.quad(P(o0,-3), P(o0, 3), P(o1, 3), P(o1,-3), stone)      # outer curve
        m.quad(P(i0, 3), P(i0,-3), P(i1,-3), P(i1, 3), stone)      # inner curve
    return m


MODELS = {
    "Cube": m_cube, "Sphere": m_sphere, "IcoSphere": m_icosphere, "Cylinder": m_cylinder,
    "Cone": m_cone, "Torus": m_torus, "Crate": m_crate, "Tree": m_tree, "Rock": m_rock,
    "House": m_house, "Coin": m_coin, "Column": m_column, "Heart": m_heart,
    "Terrain": m_terrain, "Stairs": m_stairs, "Arch": m_arch,
}

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "Models")
    os.makedirs(out, exist_ok=True)
    for name, fn in MODELS.items():
        m = fn()
        with open(os.path.join(out, name + ".json"), "w") as fh:
            fh.write(m.to_json())
        print(f"{name:10s} {len(m.tris):5d} triangles")
