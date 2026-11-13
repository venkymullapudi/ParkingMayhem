using UnityEngine;

/// <summary>
/// Generates procedural sprites at runtime so the game runs without external art assets.
/// Vehicles get glossy gradient shading + soft highlight; zones glow; backgrounds are themed per terrain.
/// </summary>
public static class SpriteGenerator
{
    private static Texture2D CreateTexture(int size, System.Func<int, int, Color> pixelFunc)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[size * size];
        bool toLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var col = pixelFunc(x, y);
                if (toLinear)
                {
                    var c = col.linear;
                    col = new Color(c.r, c.g, c.b, col.a);
                }
                px[y * size + x] = col;
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ── Glossy shading ─────────────────────────────────────────────────────────
    // nx, ny in [-1, 1] relative to shape center. Adds top-lighting + specular highlight.
    private static Color Shade(Color c, float nx, float ny, float alpha)
    {
        float light = 0.70f + 0.42f * ((ny + 1f) * 0.5f);     // brighter toward top
        float r = Mathf.Clamp01(c.r * light);
        float g = Mathf.Clamp01(c.g * light);
        float b = Mathf.Clamp01(c.b * light);

        // Soft specular highlight upper-left
        float hx = nx + 0.32f, hy = ny - 0.42f;
        float hd = Mathf.Sqrt(hx * hx + hy * hy);
        float spec = Mathf.Clamp01(1f - hd / 0.6f);
        spec = spec * spec * 0.45f;
        r = Mathf.Lerp(r, 1f, spec);
        g = Mathf.Lerp(g, 1f, spec);
        b = Mathf.Lerp(b, 1f, spec);
        return new Color(r, g, b, alpha);
    }

    // Darker rim color for outlines
    private static Color Rim(Color c, float alpha)
        => new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, alpha);

    public static Sprite CreateCircle(int size = 128, Color? color = null)
    {
        Color c = color ?? Color.white;
        float r = size / 2f - 2f;
        float cx = size / 2f, cy = size / 2f;
        float rim = r * 0.12f;
        var tex = CreateTexture(size, (x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > r + 1f) return Color.clear;
            float alpha = Mathf.Clamp01(r + 1f - dist);
            float nx = dx / r, ny = dy / r;
            if (dist >= r - rim) return Rim(c, alpha);
            return Shade(c, nx, ny, alpha);
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    // Flat white speaker icon for the mute toggle. muted=true draws a diagonal slash
    // instead of the two sound-wave arcs. Transparent background; tint via Image.color.
    public static Sprite CreateSpeakerIcon(int size = 64, bool muted = false)
    {
        Color on = Color.white;
        var tex = CreateTexture(size, (x, y) =>
        {
            float u = (float)x / (size - 1);   // 0 left → 1 right
            float v = (float)y / (size - 1);   // 0 bottom → 1 top
            float cy = 0.5f;

            // Speaker body: a rectangle (left) + trapezoidal cone (toward center)
            bool box  = u >= 0.10f && u <= 0.30f && Mathf.Abs(v - cy) <= 0.18f;
            float coneHalf = Mathf.Lerp(0.18f, 0.34f, Mathf.InverseLerp(0.30f, 0.50f, u));
            bool cone = u >= 0.30f && u <= 0.50f && Mathf.Abs(v - cy) <= coneHalf;
            if (box || cone) return on;

            if (muted)
            {
                // Diagonal slash through the right side
                float d = Mathf.Abs((v - 0.18f) - (u - 0.50f));   // line v-0.18 = u-0.50
                if (u >= 0.52f && u <= 0.92f && d < 0.05f) return on;
            }
            else
            {
                // Two concentric sound-wave arcs to the right of the cone
                float dx = u - 0.46f, dy = v - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                bool arc1 = Mathf.Abs(r - 0.22f) < 0.035f && dx > 0.05f;
                bool arc2 = Mathf.Abs(r - 0.34f) < 0.035f && dx > 0.08f;
                if (arc1 || arc2) return on;
            }
            return Color.clear;
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    // Rounded square with gradient + rim
    public static Sprite CreateSquare(int size = 128, Color? color = null)
    {
        Color c = color ?? Color.white;
        float cx = size / 2f, cy = size / 2f;
        float half = size / 2f - 2f;
        float rad = size * 0.18f;           // corner radius
        float rimW = size * 0.06f;
        var tex = CreateTexture(size, (x, y) =>
        {
            float dx = Mathf.Abs(x - cx), dy = Mathf.Abs(y - cy);
            // Rounded-rect signed distance
            float qx = Mathf.Max(dx - (half - rad), 0f);
            float qy = Mathf.Max(dy - (half - rad), 0f);
            float sdf = Mathf.Sqrt(qx * qx + qy * qy) - rad; // <=0 inside
            if (sdf > 1f) return Color.clear;
            float alpha = Mathf.Clamp01(1f - sdf);
            float nx = (x - cx) / half, ny = (y - cy) / half;
            if (sdf >= -rimW) return Rim(c, alpha);
            return Shade(c, nx, ny, alpha);
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    public static Sprite CreateRegularPolygon(int sides, int size = 128, Color? color = null)
    {
        Color c = color ?? Color.white;
        float r = size / 2f - 3f;
        float cx = size / 2f, cy = size / 2f;
        float rimW = r * 0.12f;
        var tex = CreateTexture(size, (x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            float angle = Mathf.Atan2(dy, dx);
            float sector = Mathf.PI * 2f / sides;
            float nearest = Mathf.Round(angle / sector) * sector;
            float polyR = r * Mathf.Cos(Mathf.PI / sides) /
                          Mathf.Max(0.001f, Mathf.Cos(angle - nearest));
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > polyR + 1f) return Color.clear;
            float alpha = Mathf.Clamp01(polyR + 1f - dist);
            float nx = dx / r, ny = dy / r;
            if (dist >= polyR - rimW) return Rim(c, alpha);
            return Shade(c, nx, ny, alpha);
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    public static Sprite CreateStarPolygon(int points, float innerRatio, int size = 128, Color? color = null)
    {
        Color c = color ?? Color.white;
        float cx = size / 2f, cy = size / 2f;
        float outerR = size / 2f - 3f;
        float innerR = outerR * innerRatio;
        int totalVerts = points * 2;
        float rimW = outerR * 0.10f;
        var tex = CreateTexture(size, (x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            float angle = Mathf.Atan2(dy, dx);
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float sector = Mathf.PI * 2f / totalVerts;
            float nearest = Mathf.Round(angle / sector) * sector;
            float t = Mathf.Abs(angle - nearest) / sector;
            float polyR = Mathf.Lerp(outerR, innerR, t);
            if (dist > polyR + 1f) return Color.clear;
            float alpha = Mathf.Clamp01(polyR + 1f - dist);
            float nx = dx / outerR, ny = dy / outerR;
            if (dist >= polyR - rimW) return Rim(c, alpha);
            return Shade(c, nx, ny, alpha);
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    public static Sprite CreateWheel(int size = 32) => CreateCircle(size, Color.black);

    // ── Real top-down vehicles (per terrain) ─────────────────────────────────────
    // All silhouettes point UP (+v) which matches transform.up = forward.

    private static float RBox(float u, float v, float cx, float cy, float hx, float hy, float r)
    {
        float dx = Mathf.Max(Mathf.Abs(u - cx) - (hx - r), 0f);
        float dy = Mathf.Max(Mathf.Abs(v - cy) - (hy - r), 0f);
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }
    private static bool InBox(float u, float v, float cx, float cy, float hx, float hy)
        => Mathf.Abs(u - cx) <= hx && Mathf.Abs(v - cy) <= hy;
    private static bool InEllipse(float u, float v, float cx, float cy, float rx, float ry)
    {
        float dx = (u - cx) / rx, dy = (v - cy) / ry; return dx * dx + dy * dy <= 1f;
    }

    private static readonly Color Glass = new Color(0.55f, 0.78f, 0.95f);
    private static readonly Color Tyre  = new Color(0.10f, 0.10f, 0.13f);
    private static readonly Color Head  = new Color(1f, 0.96f, 0.72f);   // headlights
    private static readonly Color Tail  = new Color(0.90f, 0.16f, 0.12f); // taillights

    private static readonly Color White2 = new Color(0.92f, 0.92f, 0.95f);
    private static readonly Color RedAcc = new Color(0.82f, 0.27f, 0.24f);

    // Returns the part color for a vehicle pixel (a=1 if part of vehicle, else a=0).
    private static Color VPixel(GameManager.VehicleKind k, float u, float v, Color baseCol)
    {
        switch (k)
        {
            // ── ROAD ──────────────────────────────────────────────────────────
            case GameManager.VehicleKind.Car:
            {
                bool inBody = RBox(u, v, 0f, 0f, 0.34f, 0.50f, 0.16f) <= 0f;
                bool wheel = InBox(u, v, -0.40f, 0.28f, 0.09f, 0.15f) || InBox(u, v, 0.40f, 0.28f, 0.09f, 0.15f)
                          || InBox(u, v, -0.40f, -0.30f, 0.09f, 0.15f) || InBox(u, v, 0.40f, -0.30f, 0.09f, 0.15f);
                if (inBody)
                {
                    if (InEllipse(u, v, -0.20f, 0.45f, 0.06f, 0.045f) || InEllipse(u, v, 0.20f, 0.45f, 0.06f, 0.045f)) return Head;
                    if (InEllipse(u, v, -0.20f, -0.45f, 0.06f, 0.04f) || InEllipse(u, v, 0.20f, -0.45f, 0.06f, 0.04f)) return Tail;
                    if (InBox(u, v, 0f, 0.24f, 0.21f, 0.12f)) return Shade(Glass, u, v, 1f);
                    if (InBox(u, v, 0f, -0.18f, 0.21f, 0.10f)) return Shade(Glass * 0.8f, u, v, 1f);
                    return Shade(baseCol, u, v, 1f);
                }
                if (wheel) return Tyre;
                return Color.clear;
            }
            case GameManager.VehicleKind.Truck:
            {
                bool cab   = RBox(u, v, 0f, 0.55f, 0.28f, 0.22f, 0.08f) <= 0f;   // front cab
                bool cargo = RBox(u, v, 0f, -0.22f, 0.33f, 0.55f, 0.05f) <= 0f;  // long trailer
                bool wheel = InBox(u, v, -0.36f, 0.5f, 0.07f, 0.12f) || InBox(u, v, 0.36f, 0.5f, 0.07f, 0.12f)
                          || InBox(u, v, -0.36f, -0.15f, 0.07f, 0.12f) || InBox(u, v, 0.36f, -0.15f, 0.07f, 0.12f)
                          || InBox(u, v, -0.36f, -0.55f, 0.07f, 0.12f) || InBox(u, v, 0.36f, -0.55f, 0.07f, 0.12f);
                if (cab)
                {
                    if (InEllipse(u, v, -0.18f, 0.74f, 0.055f, 0.04f) || InEllipse(u, v, 0.18f, 0.74f, 0.055f, 0.04f)) return Head;
                    if (InBox(u, v, 0f, 0.66f, 0.20f, 0.07f)) return Shade(Glass, u, v, 1f);
                    return Shade(baseCol, u, v, 1f);
                }
                if (cargo)
                {
                    if (InEllipse(u, v, -0.22f, -0.74f, 0.06f, 0.04f) || InEllipse(u, v, 0.22f, -0.74f, 0.06f, 0.04f)) return Tail;
                    return Shade(White2, u, v, 1f);
                }
                if (wheel) return Tyre;
                return Color.clear;
            }
            case GameManager.VehicleKind.Bus:
            {
                bool body = RBox(u, v, 0f, 0f, 0.31f, 0.62f, 0.16f) <= 0f;
                bool wheel = InBox(u, v, -0.34f, 0.40f, 0.07f, 0.12f) || InBox(u, v, 0.34f, 0.40f, 0.07f, 0.12f)
                          || InBox(u, v, -0.34f, -0.42f, 0.07f, 0.12f) || InBox(u, v, 0.34f, -0.42f, 0.07f, 0.12f);
                if (body)
                {
                    if (InEllipse(u, v, -0.20f, 0.58f, 0.05f, 0.04f) || InEllipse(u, v, 0.20f, 0.58f, 0.05f, 0.04f)) return Head;
                    if (InEllipse(u, v, -0.20f, -0.58f, 0.05f, 0.04f) || InEllipse(u, v, 0.20f, -0.58f, 0.05f, 0.04f)) return Tail;
                    if (InBox(u, v, 0f, 0.55f, 0.22f, 0.06f)) return Shade(Glass, u, v, 1f);     // windshield
                    if (InBox(u, v, 0f, 0.0f, 0.24f, 0.42f)) return Shade(Glass * 0.85f, u, v, 1f); // long window strip
                    return Shade(baseCol, u, v, 1f);
                }
                if (wheel) return Tyre;
                return Color.clear;
            }
            // ── WATER ─────────────────────────────────────────────────────────
            case GameManager.VehicleKind.Boat:
            {
                bool inHull;
                if (v <= 0.45f && v >= -0.82f) inHull = Mathf.Abs(u) <= 0.32f;
                else if (v > 0.45f && v <= 0.95f) inHull = Mathf.Abs(u) <= 0.32f * (1f - (v - 0.45f) / 0.5f);
                else inHull = false;
                if (inHull)
                {
                    if (InBox(u, v, 0f, -0.05f, 0.17f, 0.20f)) return Shade(White2, u, v, 1f);
                    if (InBox(u, v, 0f, 0.30f, 0.12f, 0.10f)) return Shade(Glass, u, v, 1f);
                    return Shade(baseCol, u, v, 1f);
                }
                return Color.clear;
            }
            case GameManager.VehicleKind.Yacht:
            {
                bool inHull;
                if (v <= 0.40f && v >= -0.85f) inHull = Mathf.Abs(u) <= 0.22f;
                else if (v > 0.40f && v <= 0.98f) inHull = Mathf.Abs(u) <= 0.22f * (1f - (v - 0.40f) / 0.58f);
                else inHull = false;
                if (inHull)
                {
                    if (InBox(u, v, 0f, 0.05f, 0.15f, 0.34f)) return Shade(White2, u, v, 1f);    // long white deck
                    if (InBox(u, v, 0f, -0.45f, 0.13f, 0.13f)) return Shade(Glass, u, v, 1f);    // aft cabin glass
                    return Shade(baseCol, u, v, 1f);
                }
                return Color.clear;
            }
            case GameManager.VehicleKind.Ship:
            {
                bool inHull;
                if (v <= 0.55f && v >= -0.88f) inHull = Mathf.Abs(u) <= 0.34f;
                else if (v > 0.55f && v <= 0.92f) inHull = Mathf.Abs(u) <= Mathf.Lerp(0.34f, 0.12f, (v - 0.55f) / 0.37f);
                else inHull = false;
                if (inHull)
                {
                    if (InBox(u, v, 0f, -0.30f, 0.24f, 0.26f)) return Shade(White2, u, v, 1f);   // superstructure
                    if (InBox(u, v, 0f, -0.34f, 0.07f, 0.10f)) return Shade(RedAcc, u, v, 1f);   // funnel
                    if (InBox(u, v, 0f, 0.25f, 0.26f, 0.22f)) return Shade(baseCol * 0.8f, u, v, 1f); // cargo deck
                    return Shade(baseCol, u, v, 1f);
                }
                return Color.clear;
            }
            // ── AIR ───────────────────────────────────────────────────────────
            case GameManager.VehicleKind.Plane:
            {
                bool fus = InEllipse(u, v, 0f, 0f, 0.13f, 0.72f);
                bool wings = Mathf.Abs(v + 0.02f) <= 0.10f && Mathf.Abs(u) <= 0.70f - 0.6f * Mathf.Abs(v + 0.02f);
                bool tail = Mathf.Abs(v + 0.60f) <= 0.07f && Mathf.Abs(u) <= 0.28f - 1.5f * Mathf.Abs(v + 0.60f);
                if (fus || wings || tail)
                {
                    if (InEllipse(u, v, 0f, 0.42f, 0.08f, 0.14f)) return Shade(Glass, u, v, 1f); // cockpit
                    // engine nacelles on the wings
                    if (InEllipse(u, v, -0.34f, -0.04f, 0.06f, 0.09f) || InEllipse(u, v, 0.34f, -0.04f, 0.06f, 0.09f))
                        return Shade(new Color(0.25f, 0.25f, 0.3f), u, v, 1f);
                    return Shade(baseCol, u, v, 1f);
                }
                return Color.clear;
            }
            case GameManager.VehicleKind.Jet:
            {
                bool fus = InEllipse(u, v, 0f, 0.05f, 0.10f, 0.78f);
                // swept delta wings: wide at the back, narrowing toward the nose
                bool wings = v >= -0.45f && v <= 0.25f && Mathf.Abs(u) <= 0.62f * (0.25f - v) / 0.70f;
                bool tail = Mathf.Abs(v + 0.62f) <= 0.05f && Mathf.Abs(u) <= 0.22f - 2f * Mathf.Abs(v + 0.62f);
                if (fus || wings || tail)
                {
                    if (InEllipse(u, v, 0f, 0.5f, 0.06f, 0.13f)) return Shade(Glass, u, v, 1f);
                    return Shade(baseCol, u, v, 1f);
                }
                return Color.clear;
            }
            case GameManager.VehicleKind.Helicopter:
            {
                bool body = InEllipse(u, v, 0f, 0.12f, 0.20f, 0.36f);
                bool boom = Mathf.Abs(u) <= 0.05f && v <= -0.18f && v >= -0.78f;       // tail boom
                bool tailFin = Mathf.Abs(v + 0.74f) <= 0.10f && Mathf.Abs(u) <= 0.12f;
                bool rotor = (Mathf.Abs(v - 0.12f) <= 0.035f && Mathf.Abs(u) <= 0.66f)  // rotor blades (cross)
                          || (Mathf.Abs(u) <= 0.035f && Mathf.Abs(v - 0.12f) <= 0.66f);
                if (body)
                {
                    if (InEllipse(u, v, 0f, 0.30f, 0.10f, 0.12f)) return Shade(Glass, u, v, 1f); // cockpit
                    return Shade(baseCol, u, v, 1f);
                }
                if (rotor) return Shade(new Color(0.3f, 0.3f, 0.34f), u, v, 1f);
                if (boom || tailFin) return Shade(baseCol * 0.85f, u, v, 1f);
                return Color.clear;
            }
            // ── SPACE ─────────────────────────────────────────────────────────
            case GameManager.VehicleKind.Rocket:
            {
                bool body = RBox(u, v, 0f, 0f, 0.17f, 0.52f, 0.14f) <= 0f;
                bool nose = v > 0.40f && v <= 0.95f && Mathf.Abs(u) <= 0.17f * (1f - (v - 0.40f) / 0.55f);
                bool fin = false;
                if (v <= -0.30f && v >= -0.72f)
                {
                    float tt = (-0.30f - v) / 0.42f;
                    fin = Mathf.Abs(u) >= 0.12f && Mathf.Abs(u) <= 0.17f + 0.22f * tt;
                }
                if (body || nose || fin)
                {
                    if (nose && v > 0.78f) return Shade(RedAcc, u, v, 1f);
                    if (InEllipse(u, v, 0f, 0.26f, 0.10f, 0.13f)) return Shade(Glass, u, v, 1f);
                    if (fin && !body) return Shade(RedAcc, u, v, 1f);
                    return Shade(baseCol, u, v, 1f);
                }
                return Color.clear;
            }
            case GameManager.VehicleKind.Shuttle:
            {
                bool fus = InEllipse(u, v, 0f, 0.05f, 0.15f, 0.60f);
                bool nose = v > 0.55f && v <= 0.92f && Mathf.Abs(u) <= 0.15f * (1f - (v - 0.55f) / 0.37f);
                bool wings = v >= -0.55f && v <= -0.05f && Mathf.Abs(u) <= 0.55f * (-0.05f - v) / 0.50f;
                bool tail = Mathf.Abs(v + 0.55f) <= 0.06f && Mathf.Abs(u) <= 0.10f;
                if (fus || nose || wings || tail)
                {
                    if (nose && v > 0.80f) return Shade(new Color(0.15f, 0.15f, 0.18f), u, v, 1f); // black nose
                    if (InEllipse(u, v, 0f, 0.40f, 0.07f, 0.10f)) return Shade(Glass, u, v, 1f);
                    return Shade(White2, u, v, 1f);  // shuttle is white
                }
                return Color.clear;
            }
            case GameManager.VehicleKind.Satellite:
            {
                bool body = RBox(u, v, 0f, 0f, 0.16f, 0.24f, 0.05f) <= 0f;
                bool arms = Mathf.Abs(v) <= 0.04f && Mathf.Abs(u) > 0.16f && Mathf.Abs(u) <= 0.26f;
                bool panels = (InBox(u, v, -0.46f, 0f, 0.20f, 0.16f) || InBox(u, v, 0.46f, 0f, 0.20f, 0.16f));
                bool dish = InEllipse(u, v, 0f, 0.30f, 0.10f, 0.09f);
                if (panels) return Shade(new Color(0.2f, 0.35f, 0.75f), u, v, 1f);   // blue solar panels
                if (body)
                {
                    if (InBox(u, v, 0f, 0f, 0.10f, 0.16f)) return Shade(new Color(0.85f, 0.78f, 0.4f), u, v, 1f); // gold foil
                    return Shade(baseCol, u, v, 1f);
                }
                if (dish) return Shade(White2, u, v, 1f);
                if (arms) return Tyre;
                return Color.clear;
            }
            // ── BIZARRE (very high levels) ────────────────────────────────────
            case GameManager.VehicleKind.Blob:
            {
                bool b = InEllipse(u, v, 0f, 0f, 0.55f, 0.45f)
                      || InEllipse(u, v, 0.30f, 0.26f, 0.30f, 0.30f)
                      || InEllipse(u, v, -0.28f, 0.20f, 0.28f, 0.34f)
                      || InEllipse(u, v, 0.10f, -0.36f, 0.36f, 0.30f);
                if (b)
                {
                    if (InEllipse(u, v, -0.12f, 0.08f, 0.10f, 0.12f)) return Shade(Glass, u, v, 1f); // "eye"
                    return Shade(baseCol, u, v, 1f);
                }
                return Color.clear;
            }
            case GameManager.VehicleKind.Star:
            {
                float dist = Mathf.Sqrt(u * u + v * v);
                float angle = Mathf.Atan2(v, u);
                float sector = Mathf.PI * 2f / 10f;       // 5-point star = 10 verts
                float nearest = Mathf.Round(angle / sector) * sector;
                float tt = Mathf.Abs(angle - nearest) / sector;
                float polyR = Mathf.Lerp(0.92f, 0.42f, tt);
                if (dist <= polyR) return Shade(baseCol, u, v, 1f);
                return Color.clear;
            }
        }
        return Color.clear;
    }

    public static Sprite CreateVehicle(GameManager.VehicleKind kind, int size = 128, Color? color = null)
    {
        Color basec = color ?? Color.white;
        float texel = 2f / size;
        float o = texel * 0.25f;
        bool toLin = QualitySettings.activeColorSpace == ColorSpace.Linear;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) * texel - 1f;
                float v = (y + 0.5f) * texel - 1f;
                // 2x2 supersample (premultiplied) for smooth edges
                float aSum = 0f, r = 0f, g = 0f, b = 0f;
                for (int sy = -1; sy <= 1; sy += 2)
                    for (int sx = -1; sx <= 1; sx += 2)
                    {
                        var c = VPixel(kind, u + sx * o, v + sy * o, basec);
                        aSum += c.a; r += c.r * c.a; g += c.g * c.a; b += c.b * c.a;
                    }
                Color outc;
                if (aSum <= 0f) outc = Color.clear;
                else
                {
                    outc = new Color(r / aSum, g / aSum, b / aSum, aSum / 4f);
                    if (toLin) { var l = outc.linear; outc = new Color(l.r, l.g, l.b, outc.a); }
                }
                px[y * size + x] = outc;
            }

        // Clean dark outline around the silhouette for a crisp, designed look.
        int t2 = Mathf.Max(1, size / 90);
        var src = (Color[])px.Clone();
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                if (src[i].a < 0.55f) continue;
                bool edge = false;
                for (int d = 0; d < 4 && !edge; d++)
                {
                    int nx = x + (d == 0 ? t2 : d == 1 ? -t2 : 0);
                    int ny = y + (d == 2 ? t2 : d == 3 ? -t2 : 0);
                    if (nx < 0 || ny < 0 || nx >= size || ny >= size) edge = true;
                    else if (src[ny * size + nx].a < 0.2f) edge = true;
                }
                if (edge) px[i] = new Color(src[i].r * 0.28f, src[i].g * 0.28f, src[i].b * 0.28f, src[i].a);
            }

        tex.SetPixels(px);
        tex.Apply();
        // ppu scales with resolution so the world-space size stays constant at any size.
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / 1.28f);
    }

    // Glowing parking footprint matching the vehicle silhouette.
    public static Sprite CreateVehicleZone(GameManager.VehicleKind kind, int size = 320)
    {
        float texel = 2f / size;
        bool toLin = QualitySettings.activeColorSpace == ColorSpace.Linear;
        Color glow = new Color(0.3f, 1f, 0.5f);
        var inside = new bool[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) * texel - 1f;
                float v = (y + 0.5f) * texel - 1f;
                inside[y * size + x] = VPixel(kind, u, v, Color.white).a > 0f;
            }
        int k = Mathf.Max(2, size / 36);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                if (!inside[i]) { px[i] = Color.clear; continue; }
                bool edge = false;
                for (int d = 0; d < 4 && !edge; d++)
                {
                    int nx = x + (d == 0 ? k : d == 1 ? -k : 0);
                    int ny = y + (d == 2 ? k : d == 3 ? -k : 0);
                    if (nx < 0 || ny < 0 || nx >= size || ny >= size) edge = true;
                    else if (!inside[ny * size + nx]) edge = true;
                }
                Color c = edge ? new Color(glow.r, glow.g, glow.b, 0.92f)
                               : new Color(glow.r, glow.g, glow.b, 0.16f);
                if (toLin) { var l = c.linear; c = new Color(l.r, l.g, l.b, c.a); }
                px[i] = c;
            }
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / 1.6f);
    }

    // ── Hazards / obstacles (themed per terrain) ────────────────────────────────
    // Road/Air hazards reuse real vehicles (other traffic); Water adds whales/dolphins;
    // Space adds asteroids/debris.
    public static Sprite CreateHazard(GameManager.TerrainType terrain, int index, int size = 256, Color? color = null)
    {
        switch (terrain)
        {
            case GameManager.TerrainType.Water:
                if (index % 3 == 0) return CreateWhale(size);
                if (index % 3 == 1) return CreateDolphin(size);
                return CreateVehicle(GameManager.VehicleKind.Ship, size, new Color(0.5f, 0.5f, 0.55f));
            case GameManager.TerrainType.Space:
                return CreateAsteroid(size, index % 2 == 0 ? 1f : 0.6f);
            case GameManager.TerrainType.Air:
            {
                var kinds = new[] { GameManager.VehicleKind.Plane, GameManager.VehicleKind.Jet, GameManager.VehicleKind.Helicopter };
                return CreateVehicle(kinds[index % kinds.Length], size, color ?? new Color(0.6f, 0.6f, 0.65f));
            }
            default: // Road traffic + the occasional pedestrian
            {
                if (index % 4 == 3) return CreatePedestrian(size);
                var kinds = new[] { GameManager.VehicleKind.Car, GameManager.VehicleKind.Truck, GameManager.VehicleKind.Bus };
                return CreateVehicle(kinds[index % 3], size, color ?? new Color(0.6f, 0.6f, 0.65f));
            }
        }
    }

    // Top-down pedestrian hazard: shirt torso (rounded), skin-tone head, two arm bumps.
    public static Sprite CreatePedestrian(int size = 256, Color? shirt = null)
    {
        Color body = shirt ?? new Color(0.20f, 0.55f, 0.90f);   // shirt
        Color skin = new Color(0.94f, 0.78f, 0.62f);            // head
        Color arm  = new Color(body.r * 0.85f, body.g * 0.85f, body.b * 0.85f);
        float cx = size * 0.5f, cy = size * 0.5f;
        var tex = CreateTexture(size, (x, y) =>
        {
            float dx = (x - cx) / size, dy = (y - cy) / size;   // normalized -0.5..0.5
            // Head: circle near the front (top, +dy)
            float hx = dx, hy = dy - 0.14f;
            if (hx*hx + hy*hy < 0.11f*0.11f) return skin;
            // Torso: vertical ellipse centered slightly back
            float tx = dx / 0.20f, ty = (dy + 0.04f) / 0.28f;
            if (tx*tx + ty*ty < 1f) return body;
            // Arms: two small circles at the sides
            float ax = Mathf.Abs(dx) - 0.22f, ay = dy + 0.02f;
            if (ax*ax + ay*ay < 0.07f*0.07f) return arm;
            return Color.clear;
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    // A landing runway (tall strip): dark asphalt, glowing green edges, dashed centreline,
    // threshold bars at both ends. Plane lands by being ON it, aligned, and slow.
    public static Sprite CreateRunwayZone(int w = 110, int h = 340)
    {
        bool toLin = QualitySettings.activeColorSpace == ColorSpace.Linear;
        Color glow = new Color(0.3f, 1f, 0.5f);
        Color asphalt = new Color(0.16f, 0.17f, 0.20f, 0.72f);
        Color line = new Color(0.95f, 0.95f, 0.9f, 0.95f);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        int border = Mathf.Max(2, w / 14);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c;
                bool edge = x < border || x >= w - border || y < border || y >= h - border;
                if (edge) c = new Color(glow.r, glow.g, glow.b, 0.95f);
                else
                {
                    c = asphalt;
                    // dashed centreline
                    if (Mathf.Abs(x - w / 2) <= Mathf.Max(1, w / 28) && (y / Mathf.Max(1, h / 16)) % 2 == 0) c = line;
                    // threshold bars near each end
                    else if ((y < h * 0.12f && y > h * 0.06f) || (y > h * 0.88f && y < h * 0.94f))
                    {
                        if (((x - border) / Mathf.Max(1, w / 8)) % 2 == 0) c = line;
                    }
                }
                if (toLin && c.a > 0f) { var l = c.linear; c = new Color(l.r, l.g, l.b, c.a); }
                px[y * w + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        // ppu sized so the runway is ~1.1 world units wide (length scales with h)
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w / 1.1f);
    }

    // Top-down aircraft carrier: hull + flight deck + island superstructure + deck markings.
    // At PPU 100 this is 2.56 wu wide and 4.48 wu long — big enough to frame the runway zone.
    public static Sprite CreateAircraftCarrier(int w = 256, int h = 448)
    {
        bool toLin = QualitySettings.activeColorSpace == ColorSpace.Linear;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        float cx = w * 0.5f;

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);  // 0 = stern, 1 = bow

            // Hull half-width tapers at bow (top) and rounds at stern (bottom)
            float halfW;
            if      (v > 0.88f) halfW = Mathf.Lerp(w * 0.42f, w * 0.12f, (v - 0.88f) / 0.12f);
            else if (v < 0.05f) halfW = Mathf.Lerp(w * 0.14f, w * 0.42f, v / 0.05f);
            else                halfW = w * 0.42f;

            int x0 = Mathf.Max(0, Mathf.RoundToInt(cx - halfW));
            int x1 = Mathf.Min(w - 1, Mathf.RoundToInt(cx + halfW));

            for (int x = x0; x <= x1; x++)
            {
                float u = (float)x / (w - 1);  // 0 = port (left), 1 = starboard (right)

                bool edge      = x <= x0 + 2 || x >= x1 - 2 || y <= 2 || y >= h - 3;
                bool island    = !edge && u >= 0.62f && u <= 0.86f && v >= 0.44f && v <= 0.70f;
                bool portWarn  = !edge && !island && x <= x0 + 13 && x >= x0 + 3;
                bool stbdWarn  = !edge && !island && x >= x1 - 13 && x <= x1 - 3;
                bool centreLine= !edge && !island && Mathf.Abs(x - cx) < 1.5f && (y / 16) % 2 == 0;
                bool tdBar     = !edge && !island && v >= 0.36f && v <= 0.64f
                               && Mathf.Abs(x - cx) <= w * 0.26f && y % 22 <= 2;

                Color c;
                if      (edge)                    c = new Color(0.18f, 0.20f, 0.23f, 1f); // dark hull rim
                else if (island)                  c = new Color(0.14f, 0.16f, 0.20f, 1f); // island block
                else if (portWarn || stbdWarn)    c = new Color(0.95f, 0.80f, 0.06f, 1f); // yellow edge stripe
                else if (centreLine || tdBar)     c = new Color(0.90f, 0.90f, 0.88f, 1f); // white markings
                else                              c = new Color(0.45f, 0.47f, 0.50f, 1f); // flight deck gray

                // Subtle top-lighting: lighter toward bow
                float shade = 0.86f + 0.14f * v;
                c = new Color(c.r * shade, c.g * shade, c.b * shade, c.a);
                if (toLin && c.a > 0f) { var l = c.linear; c = new Color(l.r, l.g, l.b, c.a); }
                px[y * w + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // Top-down wooden pier/dock (Water terrain) — lengthwise planks with dark gaps, dark edge
    // pilings, and white mooring cleats. Purely visual backing behind the boat parking zone.
    // At PPU 100 this is 2.56 wu wide × 4.2 wu long.
    public static Sprite CreatePier(int w = 256, int h = 420)
    {
        bool toLin = QualitySettings.activeColorSpace == ColorSpace.Linear;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        float cx = w * 0.5f;
        float halfW = w * 0.40f;
        int x0 = Mathf.Max(0, Mathf.RoundToInt(cx - halfW));
        int x1 = Mathf.Min(w - 1, Mathf.RoundToInt(cx + halfW));
        int plankH = Mathf.Max(6, h / 26);   // plank running across the short axis

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            for (int x = x0; x <= x1; x++)
            {
                bool edge   = x <= x0 + 2 || x >= x1 - 2 || y <= 2 || y >= h - 3;
                bool piling = !edge && (x <= x0 + 12 || x >= x1 - 12) && (y / 40) % 2 == 0; // posts along both rails
                bool gap    = !edge && (y % plankH) < 2;                                    // dark gaps between planks
                // White mooring cleats near each end, just inside the rails
                bool cleat  = !edge && ((v > 0.06f && v < 0.11f) || (v > 0.89f && v < 0.94f))
                              && (Mathf.Abs(x - (x0 + 16)) < 5 || Mathf.Abs(x - (x1 - 16)) < 5);

                Color c;
                if      (edge)   c = new Color(0.20f, 0.13f, 0.07f, 1f);  // dark plank rim
                else if (piling) c = new Color(0.16f, 0.11f, 0.06f, 1f);  // pilings/posts
                else if (cleat)  c = new Color(0.90f, 0.90f, 0.86f, 1f);  // white cleats
                else if (gap)    c = new Color(0.30f, 0.20f, 0.11f, 1f);  // gap shadow
                else
                {
                    // Wood plank with subtle per-plank tone variation
                    float tone = 0.90f + 0.10f * (((y / plankH) % 3) * 0.5f);
                    c = new Color(0.52f * tone, 0.37f * tone, 0.20f * tone, 1f);
                }
                if (toLin && c.a > 0f) { var l = c.linear; c = new Color(l.r, l.g, l.b, c.a); }
                px[y * w + x] = c;
            }
        }

        // Pilings extending into the water: dark round-ish post stubs protruding from both long
        // rails at regular intervals, with a faint shadow beyond — adds depth around the deck.
        Color postCol   = new Color(0.13f, 0.09f, 0.05f, 1f);
        Color postShade = new Color(0.06f, 0.05f, 0.03f, 0.55f);
        int postLen  = Mathf.Max(4, w / 14);   // how far the post sticks out past the rail
        int postHalf = Mathf.Max(3, h / 70);   // half-thickness of each post along the long axis
        for (int py = h / 12; py < h; py += h / 6)
        {
            for (int dy = -postHalf; dy <= postHalf; dy++)
            {
                int yy = py + dy;
                if (yy < 0 || yy >= h) continue;
                // Left rail posts (protrude toward x=0)
                for (int x = Mathf.Max(0, x0 - postLen); x < x0; x++)
                {
                    Color c = (x < x0 - postLen / 2) ? postShade : postCol;
                    if (toLin && c.a > 0f) { var l = c.linear; c = new Color(l.r, l.g, l.b, c.a); }
                    px[yy * w + x] = c;
                }
                // Right rail posts (protrude toward x=w)
                for (int x = x1 + 1; x < Mathf.Min(w, x1 + 1 + postLen); x++)
                {
                    Color c = (x > x1 + postLen / 2) ? postShade : postCol;
                    if (toLin && c.a > 0f) { var l = c.linear; c = new Color(l.r, l.g, l.b, c.a); }
                    px[yy * w + x] = c;
                }
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // Narrow docking port zone (Space terrain) — cyan glowing edges, dashed centreline,
    // alignment bars near each end, midpoint cross-tick guides. Sized at ~0.9wu wide × 1.3wu tall.
    public static Sprite CreateDockingPort(int w = 90, int h = 130)
    {
        bool toLin   = QualitySettings.activeColorSpace == ColorSpace.Linear;
        Color glow   = new Color(0.15f, 0.85f, 0.95f);
        Color dark   = new Color(0.04f, 0.06f, 0.10f, 0.72f);
        Color marker = new Color(0.88f, 0.92f, 0.96f, 0.90f);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        int border = Mathf.Max(2, w / 9);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Color c;
            bool edge = x < border || x >= w - border || y < border || y >= h - border;
            if (edge)
            {
                c = new Color(glow.r, glow.g, glow.b, 0.95f);
            }
            else
            {
                c = dark;
                bool centre = Mathf.Abs(x - w * 0.5f) < Mathf.Max(1f, w / 22f);
                bool dash   = (y / Mathf.Max(1, h / 14)) % 2 == 0;
                if (centre && dash) c = marker;
                else
                {
                    bool nearEnd = (y < h * 0.15f && y > h * 0.07f) || (y > h * 0.85f && y < h * 0.93f);
                    if (nearEnd && ((x - border) / Mathf.Max(1, w / 5)) % 2 == 0) c = marker;
                    else if (y >= h * 0.46f && y <= h * 0.54f && (x < border + 7 || x >= w - border - 7))
                        c = new Color(glow.r, glow.g, glow.b, 0.75f);
                }
            }
            if (toLin && c.a > 0f) { var lc = c.linear; c = new Color(lc.r, lc.g, lc.b, c.a); }
            px[y * w + x] = c;
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w / 0.9f);
    }

    // Top-down space station: central hub with 4 radial arms, blue solar panels on the horizontal
    // arms, end modules on each arm, and a glowing cyan docking port ring at the center.
    // At PPU 100 this is 3.2 wu × 3.2 wu — large enough to frame the docking zone.
    public static Sprite CreateSpaceStation(int size = 320)
    {
        Color hull  = new Color(0.70f, 0.72f, 0.75f);
        Color foil  = new Color(0.82f, 0.74f, 0.32f);
        Color solar = new Color(0.20f, 0.35f, 0.78f);
        Color port  = new Color(0.15f, 0.88f, 0.95f);
        bool toLin  = QualitySettings.activeColorSpace == ColorSpace.Linear;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        float texel = 2f / size;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (x + 0.5f) * texel - 1f;
            float v = (y + 0.5f) * texel - 1f;

            float pd   = Mathf.Sqrt(u * u + v * v);
            bool portRing  = pd >= 0.09f && pd <= 0.17f;
            bool portInner = pd < 0.09f;
            bool hub       = pd > 0.17f && pd <= 0.27f;

            bool armH = Mathf.Abs(v) < 0.055f && Mathf.Abs(u) > 0.27f && Mathf.Abs(u) < 0.62f;
            bool armV = Mathf.Abs(u) < 0.055f && Mathf.Abs(v) > 0.27f && Mathf.Abs(v) < 0.62f;

            bool modL  = InBox(u, v, -0.68f,  0f,    0.09f, 0.14f);
            bool modR  = InBox(u, v,  0.68f,  0f,    0.09f, 0.14f);
            bool modUp = InBox(u, v,  0f,  0.68f,   0.12f, 0.09f);
            bool modDn = InBox(u, v,  0f, -0.68f,   0.12f, 0.09f);

            bool panL = InBox(u, v, -0.46f,  0.24f, 0.14f, 0.22f)
                     || InBox(u, v, -0.46f, -0.24f, 0.14f, 0.22f);
            bool panR = InBox(u, v,  0.46f,  0.24f, 0.14f, 0.22f)
                     || InBox(u, v,  0.46f, -0.24f, 0.14f, 0.22f);

            Color c = Color.clear;
            if      (portInner)           c = new Color(0.04f, 0.08f, 0.12f, 0.82f);
            else if (portRing)            c = Shade(port, u, v, 1f);
            else if (panL || panR)
            {
                float pu = u * size * 0.5f, pv = v * size * 0.5f;
                bool grid = Mathf.Abs(pu % 10f) < 1.3f || Mathf.Abs(pv % 10f) < 1.3f;
                c = Shade(grid ? new Color(0.14f, 0.26f, 0.60f) : solar, u, v, 1f);
            }
            else if (hub)                 c = Shade(foil, u, v, 1f);
            else if (modL || modR || modUp || modDn)
                                          c = Shade(hull * 0.88f, u, v, 1f);
            else if (armH || armV)        c = Shade(hull * 0.78f, u, v, 1f);

            if (c.a > 0f)
            {
                if (toLin) { var lc = c.linear; c = new Color(lc.r, lc.g, lc.b, c.a); }
                px[y * size + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Sprite CreateAsteroid(int size, float scale = 1f)
    {
        Color rock = new Color(0.42f, 0.40f, 0.38f);
        float cx = size / 2f, cy = size / 2f, baseR = (size / 2f - 4f) * scale;
        var tex = CreateTexture(size, (x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            float ang = Mathf.Atan2(dy, dx);
            float r = baseR * (0.74f + 0.13f * Mathf.Sin(ang * 3f + 1.3f) + 0.08f * Mathf.Sin(ang * 7f) + 0.05f * Mathf.Sin(ang * 11f + 2f));
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > r) return Color.clear;
            float spk = Hash(x, y, 23);
            float shade = 0.82f + (spk - 0.5f) * 0.30f;          // speckled surface
            if (dist > r - size * 0.07f) shade *= 0.55f;         // dark rim
            return new Color(rock.r * shade, rock.g * shade, rock.b * shade, 1f);
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / 1.28f);
    }

    public static Sprite CreateWhale(int size)
    {
        Color body = new Color(0.20f, 0.32f, 0.50f);
        Color belly = new Color(0.55f, 0.62f, 0.72f);
        var tex = CreateTexture(size, (x, y) =>
        {
            float u = (x + 0.5f) * 2f / size - 1f, v = (y + 0.5f) * 2f / size - 1f;
            bool b = InEllipse(u, v, 0f, 0.05f, 0.34f, 0.60f);
            bool fluke = InBox(u, v, 0f, -0.66f, 0.42f, 0.10f) && Mathf.Abs(u) > 0.05f; // tail flukes
            if (b || fluke)
            {
                if (InEllipse(u, v, -0.16f, 0.36f, 0.045f, 0.06f) || InEllipse(u, v, 0.16f, 0.36f, 0.045f, 0.06f))
                    return new Color(0.05f, 0.05f, 0.08f, 1f); // eyes
                if (InEllipse(u, v, 0f, -0.05f, 0.16f, 0.32f)) return Shade(belly, u, v, 1f); // belly
                return Shade(body, u, v, 1f);
            }
            return Color.clear;
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / 1.28f);
    }

    public static Sprite CreateDolphin(int size)
    {
        Color body = new Color(0.50f, 0.55f, 0.62f);
        var tex = CreateTexture(size, (x, y) =>
        {
            float u = (x + 0.5f) * 2f / size - 1f, v = (y + 0.5f) * 2f / size - 1f;
            bool b = InEllipse(u, v, 0f, 0f, 0.20f, 0.52f);
            bool snout = v > 0.45f && v <= 0.78f && Mathf.Abs(u) <= 0.20f * (1f - (v - 0.45f) / 0.33f);
            bool fin = InBox(u, v, 0f, 0.02f, 0.06f, 0.16f) && u > -0.02f; // small dorsal fin
            bool fluke = InBox(u, v, 0f, -0.56f, 0.26f, 0.07f);
            if (b || snout || fin || fluke)
            {
                if (InEllipse(u, v, -0.10f, 0.34f, 0.035f, 0.045f)) return new Color(0.05f, 0.05f, 0.08f, 1f);
                return Shade(body, u, v, 1f);
            }
            return Color.clear;
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / 1.28f);
    }

    // White rounded-rect for UI buttons/panels (tint via Image.color). Vertical gloss gradient.
    public static Sprite CreateRoundedRect(int w = 200, int h = 100, float cornerFrac = 0.22f)
    {
        float rad = Mathf.Min(w, h) * cornerFrac;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pxArr = new Color[w * h];
        float cx = w / 2f, cy = h / 2f;
        float halfW = w / 2f - 1f, halfH = h / 2f - 1f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx), dy = Mathf.Abs(y - cy);
                float qx = Mathf.Max(dx - (halfW - rad), 0f);
                float qy = Mathf.Max(dy - (halfH - rad), 0f);
                float sdf = Mathf.Sqrt(qx * qx + qy * qy) - rad;
                float alpha = Mathf.Clamp01(1f - sdf);
                // subtle top gloss: brighter near top
                float ty = (float)y / h;
                float shade = 0.88f + 0.12f * ty;
                pxArr[y * w + x] = new Color(shade, shade, shade, alpha);
            }
        tex.SetPixels(pxArr);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // sides 3-8 = polygon; sides==4 = rounded square; sides>=100 = star with (sides-100) points
    public static Sprite CreateVehicleShape(int sides, int size = 128, Color? color = null)
    {
        if (sides >= 100)
        {
            int points = sides - 100;
            float inner = Mathf.Lerp(0.5f, 0.38f, Mathf.Clamp01((points - 4) / 6f));
            return CreateStarPolygon(points, inner, size, color);
        }
        if (sides == 4) return CreateSquare(size, color);
        return CreateRegularPolygon(sides, size, color);
    }

    // ── Parking zones (glowing dashed outline) ───────────────────────────────────

    public static Sprite CreateParkingZone(int size = 128) => CreateParkingZoneShape(4, size);

    public static Sprite CreateParkingZoneForShape(int sides, int size = 256)
    {
        if (sides >= 100) return CreateStarZone(sides - 100, size);
        return CreateParkingZoneShape(sides, size);
    }

    public static Sprite CreateParkingZoneShape(int sides, int size = 256)
    {
        float cx = size / 2f, cy = size / 2f;
        float outerR = size / 2f - 4f;
        float borderThickness = size * 0.055f;
        Color glow = new Color(0.3f, 1f, 0.5f);   // green glow
        var tex = CreateTexture(size, (x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float polyR;
            if (sides <= 2) polyR = outerR;
            else
            {
                float angle = Mathf.Atan2(dy, dx);
                float sector = Mathf.PI * 2f / sides;
                float nearest = Mathf.Round(angle / sector) * sector;
                polyR = outerR * Mathf.Cos(Mathf.PI / sides) /
                        Mathf.Max(0.001f, Mathf.Cos(angle - nearest));
            }
            if (dist > polyR + 2f) return Color.clear;
            float distToEdge = polyR - dist;
            // Glowing border ring
            if (distToEdge <= borderThickness)
            {
                float a = Mathf.Clamp01((borderThickness - distToEdge) / borderThickness);
                return new Color(glow.r, glow.g, glow.b, 0.35f + 0.6f * a);
            }
            // Soft inner fill, fades toward center
            float innerFade = Mathf.Clamp01(distToEdge / (outerR * 0.7f));
            return new Color(glow.r, glow.g, glow.b, 0.16f * (1f - innerFade * 0.6f));
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    private static Sprite CreateStarZone(int points, int size)
    {
        float cx = size / 2f, cy = size / 2f;
        float outerR = size / 2f - 4f;
        float innerR = outerR * 0.45f;
        float borderT = size * 0.05f;
        int totalVerts = points * 2;
        Color glow = new Color(0.3f, 1f, 0.5f);
        var tex = CreateTexture(size, (x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            float angle = Mathf.Atan2(dy, dx);
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float sector = Mathf.PI * 2f / totalVerts;
            float nearest = Mathf.Round(angle / sector) * sector;
            float t = Mathf.Abs(angle - nearest) / sector;
            float polyR = Mathf.Lerp(outerR, innerR, t);
            if (dist > polyR + 2f) return Color.clear;
            float distToEdge = polyR - dist;
            if (distToEdge <= borderT)
            {
                float a = Mathf.Clamp01((borderT - distToEdge) / borderT);
                return new Color(glow.r, glow.g, glow.b, 0.35f + 0.6f * a);
            }
            return new Color(glow.r, glow.g, glow.b, 0.14f);
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    // ── Themed backgrounds ──────────────────────────────────────────────────────

    private static float Hash(int x, int y, int seed)
    {
        int n = x * 374761393 + y * 668265263 + seed * 1274126177;
        n = (n ^ (n >> 13)) * 1274126177;
        return ((n ^ (n >> 16)) & 0x7fffffff) / 2147483647f;
    }

    private static Sprite BuildBackground(int w, int h, System.Action<Color[], int, int> painter)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        painter(px, w, h);
        // Soft vignette — darken toward the corners for a designed, polished look.
        float vcx = w / 2f, vcy = h / 2f, vmax = Mathf.Sqrt(vcx * vcx + vcy * vcy);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x - vcx, dy = y - vcy;
                float dn = Mathf.Sqrt(dx * dx + dy * dy) / vmax;
                float vig = 1f - 0.38f * dn * dn;
                int i = y * w + x;
                var c = px[i];
                px[i] = new Color(c.r * vig, c.g * vig, c.b * vig, c.a);
            }
        // Project renders in Linear color space; runtime textures otherwise display gamma-lightened.
        // Convert authored (gamma) colors to linear so darks stay dark on screen.
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i].linear;
                px[i] = new Color(c.r, c.g, c.b, px[i].a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // Premium menu backdrop: deep navy gradient, faint stars, soft glow behind the title.
    // Cached: this ~2.25MB texture was regenerated on every overlay/menu open (SHOP/SETTINGS/etc),
    // leaking the previous one each time. Content is identical per (w,h), so cache and reuse.
    private static readonly System.Collections.Generic.Dictionary<(int, int), Sprite> _menuBgCache = new();

    public static Sprite CreateMenuBackground(int w = 1024, int h = 576)
    {
        var key = (w, h);
        if (_menuBgCache.TryGetValue(key, out var cached) && cached != null) return cached;
        var s = BuildMenuBackgroundImpl(w, h);
        _menuBgCache[key] = s;
        return s;
    }

    private static Sprite BuildMenuBackgroundImpl(int w, int h)
    {
        return BuildBackground(w, h, (px, ww, hh) =>
        {
            Color top = new Color(0.05f, 0.06f, 0.14f);
            Color bot = new Color(0.10f, 0.13f, 0.24f);
            float gcx = ww * 0.5f, gcy = hh * 0.78f;     // glow center, upper area (behind title)
            float gr = ww * 0.42f;
            Color glow = new Color(0.20f, 0.45f, 0.7f);
            for (int y = 0; y < hh; y++)
            {
                float ty = (float)y / hh;
                Color band = Color.Lerp(top, bot, ty);
                for (int x = 0; x < ww; x++)
                {
                    Color c = band;
                    // Soft radial glow
                    float dx = (x - gcx), dy = (y - gcy);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / gr;
                    float g = Mathf.Clamp01(1f - d);
                    g = g * g * 0.35f;
                    c = new Color(c.r + glow.r * g, c.g + glow.g * g, c.b + glow.b * g, 1f);
                    // Faint stars
                    float hsh = Hash(x, y, 51);
                    if (hsh > 0.9985f) { float b = 0.6f + 0.4f * Hash(x, y, 52); c = new Color(b, b, b, 1f); }
                    px[y * ww + x] = c;
                }
            }
        });
    }

    // Number of background variants per terrain (used to cycle/unlock by level).
    public static int BackgroundVariantCount(GameManager.TerrainType t) => t switch
    {
        GameManager.TerrainType.Space => 5,
        _ => 4,
    };

    // Cache the large (~2.25MB) background textures by their deterministic args. SetTerrain runs
    // every level; without this the old Texture2D leaks (Destroying the SpriteRenderer's GameObject
    // does NOT free a runtime-generated sprite/texture) -> unbounded GPU memory growth over a session.
    private static readonly System.Collections.Generic.Dictionary<(GameManager.TerrainType, int, int, int), Sprite> _bgCache = new();

    public static Sprite CreateBackground(GameManager.TerrainType terrain, int variant = 0, int w = 1024, int h = 576)
    {
        var key = (terrain, variant, w, h);
        if (_bgCache.TryGetValue(key, out var cached) && cached != null) return cached;
        var s = BuildBackgroundImpl(terrain, variant, w, h);
        _bgCache[key] = s;
        return s;
    }

    private static Sprite BuildBackgroundImpl(GameManager.TerrainType terrain, int variant, int w, int h)
    {
        return BuildBackground(w, h, (px, W, H) =>
        {
            switch (terrain)
            {
                case GameManager.TerrainType.Water: PaintWaterVariant(variant, px, W, H); break;
                case GameManager.TerrainType.Air:   PaintAirVariant(variant, px, W, H);   break;
                case GameManager.TerrainType.Space: PaintSpaceVariant(variant, px, W, H); break;
                default:                            PaintRoadVariant(variant, px, W, H);  break;
            }
        });
    }

    // ── Shared helpers ──────────────────────────────────────────────────────────
    private static void Stars(Color[] px, int w, int h, int seed, float density, int yMin = 0, int yMax = -1)
    {
        if (yMax < 0) yMax = h;
        for (int y = yMin; y < yMax; y++)
            for (int x = 0; x < w; x++)
            {
                float hsh = Hash(x, y, seed);
                if (hsh > 1f - density * 0.25f) { float b = 0.7f + 0.3f * Hash(x, y, seed + 1); px[y * w + x] = new Color(b, b, b, 1f); }
                else if (hsh > 1f - density)     px[y * w + x] = new Color(0.5f, 0.5f, 0.6f, 1f);
            }
    }

    private static void SoftBlob(Color[] px, int w, int h, float cx, float cy, float rx, float ry, Color col, float strength)
    {
        int x0 = Mathf.Max(0, (int)(cx - 1.3f * rx)), x1 = Mathf.Min(w - 1, (int)(cx + 1.3f * rx));
        int y0 = Mathf.Max(0, (int)(cy - 1.3f * ry)), y1 = Mathf.Min(h - 1, (int)(cy + 1.3f * ry));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float d = dx * dx + dy * dy;
                if (d > 1.6f) continue;
                float a = Mathf.Clamp01(1f - d / 1.6f) * strength;
                int i = y * w + x;
                px[i] = Color.Lerp(px[i], col, a);
            }
    }

    private static void Disc(Color[] px, int w, int h, float cx, float cy, float r, Color col, bool shade)
    {
        int x0 = Mathf.Max(0, (int)(cx - r)), x1 = Mathf.Min(w - 1, (int)(cx + r));
        int y0 = Mathf.Max(0, (int)(cy - r)), y1 = Mathf.Min(h - 1, (int)(cy + r));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > r) continue;
                float lit = shade ? Mathf.Clamp01(0.6f + 0.5f * ((-dx - dy) / (r * 2f))) : 1f;
                px[y * w + x] = new Color(col.r * lit, col.g * lit, col.b * lit, 1f);
            }
    }

    // ── ROAD variants: night lot / day lot / desert / snow ──────────────────────
    private static void PaintRoadVariant(int variant, Color[] px, int w, int h)
    {
        Color asphalt, lane, line; Color centre = new Color(0.92f, 0.80f, 0.18f);
        switch (variant % 4)
        {
            case 1: asphalt = new Color(0.42f, 0.43f, 0.47f); lane = new Color(0.34f, 0.35f, 0.39f); line = new Color(0.95f, 0.95f, 0.92f); break; // day
            case 2: asphalt = new Color(0.55f, 0.45f, 0.30f); lane = new Color(0.30f, 0.27f, 0.22f); line = new Color(0.95f, 0.92f, 0.7f);  break; // desert
            case 3: asphalt = new Color(0.80f, 0.84f, 0.90f); lane = new Color(0.42f, 0.44f, 0.50f); line = new Color(0.55f, 0.58f, 0.66f); centre = new Color(0.6f,0.6f,0.65f); break; // snow
            default: asphalt = new Color(0.27f, 0.28f, 0.32f); lane = new Color(0.21f, 0.22f, 0.26f); line = new Color(0.88f, 0.88f, 0.82f); break; // night
        }
        float laneTop = 0.60f, laneBot = 0.40f;
        for (int y = 0; y < h; y++)
        {
            float ty = (float)y / h; bool inLane = ty <= laneTop && ty >= laneBot;
            for (int x = 0; x < w; x++)
            {
                float grain = (Hash(x, y, 7) - 0.5f) * 0.035f;
                Color b = inLane ? lane : asphalt;
                px[y * w + x] = new Color(b.r + grain, b.g + grain, b.b + grain, 1f);
            }
        }
        int my = h / 2;
        for (int x = 0; x < w; x++) { if ((x / Mathf.Max(1, w / 20)) % 2 == 0) continue; for (int dy = -1; dy <= 1; dy++) px[(my + dy) * w + x] = centre; }
        int bays = 7, yTopS = (int)(laneTop * h) + 2, yTopE = h - 2, yBotS = 2, yBotE = (int)(laneBot * h) - 2;
        for (int bnum = 0; bnum <= bays; bnum++)
        {
            int lx = (int)(w * (bnum / (float)bays));
            for (int dx = 0; dx <= 1; dx++)
            {
                int xx = Mathf.Clamp(lx + dx, 0, w - 1);
                for (int y = yTopS; y < yTopE; y++) px[y * w + xx] = Color.Lerp(px[y * w + xx], line, 0.6f);
                for (int y = yBotS; y < yBotE; y++) px[y * w + xx] = Color.Lerp(px[y * w + xx], line, 0.6f);
            }
        }
        for (int x = 0; x < w; x++) { int yt = (int)(laneTop * h), yb = (int)(laneBot * h); px[yt * w + x] = Color.Lerp(px[yt * w + x], line, 0.5f); px[yb * w + x] = Color.Lerp(px[yb * w + x], line, 0.5f); }
    }

    // ── WATER variants: open sea / lagoon / night ocean / sunset ────────────────
    private static void PaintWaterVariant(int variant, Color[] px, int w, int h)
    {
        Color deep, shallow;
        switch (variant % 4)
        {
            case 1: deep = new Color(0.05f, 0.45f, 0.50f); shallow = new Color(0.22f, 0.78f, 0.78f); break; // lagoon
            case 2: deep = new Color(0.02f, 0.05f, 0.12f); shallow = new Color(0.05f, 0.14f, 0.28f); break; // night
            case 3: deep = new Color(0.25f, 0.10f, 0.32f); shallow = new Color(0.92f, 0.50f, 0.30f); break; // sunset
            default: deep = new Color(0.04f, 0.20f, 0.42f); shallow = new Color(0.10f, 0.45f, 0.72f); break; // sea
        }
        for (int y = 0; y < h; y++)
        {
            float ty = (float)y / h; Color band = Color.Lerp(deep, shallow, ty);
            for (int x = 0; x < w; x++)
            {
                float ripple = Mathf.Sin((y * 0.18f) + Mathf.Sin(x * 0.04f) * 2.5f) * 0.5f + 0.5f;
                float add = ripple * 0.06f;
                float sparkle = Hash(x, y, 13) > 0.992f ? 0.5f : 0f;
                px[y * w + x] = new Color(band.r + add + sparkle, band.g + add + sparkle, band.b + add * 1.3f + sparkle, 1f);
            }
        }
    }

    // ── AIR variants: day / sunset / night+moon / storm ─────────────────────────
    private static void PaintAirVariant(int variant, Color[] px, int w, int h)
    {
        Color top, horizon, cloud = Color.white; float cloudA = 0.55f; bool stars = false, moon = false;
        switch (variant % 4)
        {
            case 1: top = new Color(0.30f, 0.18f, 0.52f); horizon = new Color(1f, 0.55f, 0.30f); cloud = new Color(1f, 0.8f, 0.6f); break; // sunset
            case 2: top = new Color(0.02f, 0.03f, 0.10f); horizon = new Color(0.06f, 0.08f, 0.20f); stars = true; moon = true; cloudA = 0.25f; cloud = new Color(0.6f,0.6f,0.7f); break; // night
            case 3: top = new Color(0.28f, 0.30f, 0.36f); horizon = new Color(0.48f, 0.50f, 0.56f); cloud = new Color(0.35f, 0.36f, 0.4f); cloudA = 0.75f; break; // storm
            default: top = new Color(0.20f, 0.45f, 0.82f); horizon = new Color(0.62f, 0.80f, 0.95f); break; // day
        }
        for (int y = 0; y < h; y++) { Color band = Color.Lerp(horizon, top, (float)y / h); for (int x = 0; x < w; x++) px[y * w + x] = band; }
        if (stars) Stars(px, w, h, 71, 0.010f, (int)(h * 0.45f), h);
        if (moon) { Disc(px, w, h, w * 0.78f, h * 0.78f, w * 0.06f, new Color(0.9f, 0.9f, 0.82f), false); }
        int clouds = 7;
        for (int i = 0; i < clouds; i++)
            SoftBlob(px, w, h, Hash(i, 0, 31) * w, (0.35f + 0.6f * Hash(i, 1, 31)) * h, (0.10f + 0.10f * Hash(i, 2, 31)) * w, (0.05f + 0.05f * Hash(i, 2, 31)) * w, cloud, cloudA);
    }

    // ── SPACE variants: starfield / nebula / solar system / black hole / moon ───
    private static void PaintSpaceVariant(int variant, Color[] px, int w, int h)
    {
        Color black = new Color(0.02f, 0.02f, 0.06f);
        for (int i = 0; i < px.Length; i++) px[i] = black;

        switch (variant % 5)
        {
            case 1: // nebula
                SoftBlob(px, w, h, w * 0.35f, h * 0.55f, w * 0.4f, h * 0.4f, new Color(0.45f, 0.12f, 0.55f), 0.7f);
                SoftBlob(px, w, h, w * 0.65f, h * 0.4f, w * 0.35f, h * 0.35f, new Color(0.15f, 0.25f, 0.7f), 0.6f);
                SoftBlob(px, w, h, w * 0.55f, h * 0.65f, w * 0.3f, h * 0.3f, new Color(0.8f, 0.25f, 0.45f), 0.45f);
                Stars(px, w, h, 99, 0.012f);
                break;
            case 2: // solar system
                Stars(px, w, h, 99, 0.008f);
                SoftBlob(px, w, h, w * 0.12f, h * 0.5f, w * 0.22f, h * 0.22f, new Color(1f, 0.85f, 0.4f), 0.8f);
                Disc(px, w, h, w * 0.12f, h * 0.5f, w * 0.05f, new Color(1f, 0.95f, 0.7f), false);     // sun
                Disc(px, w, h, w * 0.42f, h * 0.62f, w * 0.022f, new Color(0.6f, 0.5f, 0.4f), true);   // planet
                Disc(px, w, h, w * 0.62f, h * 0.4f, w * 0.035f, new Color(0.4f, 0.6f, 0.85f), true);   // planet
                Disc(px, w, h, w * 0.82f, h * 0.58f, w * 0.03f, new Color(0.8f, 0.55f, 0.35f), true);  // planet
                break;
            case 3: // black hole
                Stars(px, w, h, 99, 0.010f);
                // accretion ring
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x - w * 0.5f), dy = (y - h * 0.5f) * 1.4f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float ringR = w * 0.16f;
                        float band = Mathf.Abs(dist - ringR);
                        if (band < w * 0.05f)
                        {
                            float a = 1f - band / (w * 0.05f);
                            Color ring = Color.Lerp(new Color(1f, 0.5f, 0.1f), new Color(1f, 0.95f, 0.7f), a);
                            px[y * w + x] = Color.Lerp(px[y * w + x], ring, a);
                        }
                    }
                Disc(px, w, h, w * 0.5f, h * 0.5f, w * 0.11f, new Color(0f, 0f, 0f), false); // event horizon
                break;
            case 4: // moon surface
                Stars(px, w, h, 99, 0.012f, (int)(h * 0.45f), h);
                Disc(px, w, h, w * 0.72f, h * 0.78f, w * 0.05f, new Color(0.35f, 0.55f, 0.85f), true); // distant Earth
                // grey cratered ground on the lower portion
                for (int y = 0; y < (int)(h * 0.45f); y++)
                    for (int x = 0; x < w; x++)
                    {
                        float g = 0.45f + (Hash(x, y, 5) - 0.5f) * 0.08f - (1f - (float)y / (h * 0.45f)) * 0.1f;
                        px[y * w + x] = new Color(g, g, g * 0.98f, 1f);
                    }
                for (int c = 0; c < 9; c++)
                {
                    float ccx = Hash(c, 0, 17) * w, ccy = Hash(c, 1, 17) * h * 0.4f, cr = (0.02f + 0.04f * Hash(c, 2, 17)) * w;
                    Disc(px, w, h, ccx, ccy, cr, new Color(0.33f, 0.33f, 0.35f), false);
                }
                break;
            default: // starfield
                SoftBlob(px, w, h, w * 0.5f, h * 0.6f, w * 0.5f, h * 0.4f, new Color(0.08f, 0.06f, 0.16f), 0.5f);
                Stars(px, w, h, 99, 0.014f);
                break;
        }
    }
}
