using System;
using System.Collections.Generic;
using UnityEngine;

// Маленький растровый холст для процедурного пиксель-арта. Ось Y направлена вниз.
public class Canvas
{
    public readonly int w, h;
    public readonly Color32[] px;
    static readonly Color32 Clear = new Color32(0, 0, 0, 0);

    public Canvas(int w, int h) { this.w = w; this.h = h; px = new Color32[w * h]; }

    public bool In(int x, int y) => x >= 0 && y >= 0 && x < w && y < h;
    public Color32 Get(int x, int y) => In(x, y) ? px[y * w + x] : Clear;
    public void Set(int x, int y, Color32 c) { if (In(x, y)) px[y * w + x] = c; }

    public void Fill(Color32 c) { for (int i = 0; i < px.Length; i++) px[i] = c; }

    public void Rect(int x, int y, int rw, int rh, Color32 c)
    {
        for (int j = y; j < y + rh; j++) for (int i = x; i < x + rw; i++) Set(i, j, c);
    }

    public void Rect(int x, int y, int rw, int rh, Func<int, int, Color32> paint)
    {
        for (int j = y; j < y + rh; j++) for (int i = x; i < x + rw; i++) Set(i, j, paint(i, j));
    }

    public void Ellipse(float cx, float cy, float rx, float ry, Color32 c) => Ellipse(cx, cy, rx, ry, (x, y) => c);

    public void Ellipse(float cx, float cy, float rx, float ry, Func<int, int, Color32> paint)
    {
        for (int y = (int)(cy - ry - 1); y <= cy + ry + 1; y++)
            for (int x = (int)(cx - rx - 1); x <= cx + rx + 1; x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Set(x, y, paint(x, y));
            }
    }

    public void Circle(float cx, float cy, float r, Color32 c) => Ellipse(cx, cy, r, r, c);

    public void Ring(float cx, float cy, float r, float thick, Color32 c)
    {
        for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
            for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                if (d <= r && d >= r - thick) Set(x, y, c);
            }
    }

    public void Line(float x0, float y0, float x1, float y1, Color32 c)
    {
        int n = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0))) + 1;
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            Set(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), c);
        }
    }

    public void Tri(float ax, float ay, float bx, float by, float cx, float cy, Color32 c) => Tri(ax, ay, bx, by, cx, cy, (x, y) => c);

    public void Tri(float ax, float ay, float bx, float by, float cx, float cy, Func<int, int, Color32> paint)
    {
        int minX = (int)Mathf.Floor(Mathf.Min(ax, Mathf.Min(bx, cx))), maxX = (int)Mathf.Ceil(Mathf.Max(ax, Mathf.Max(bx, cx)));
        int minY = (int)Mathf.Floor(Mathf.Min(ay, Mathf.Min(by, cy))), maxY = (int)Mathf.Ceil(Mathf.Max(ay, Mathf.Max(by, cy)));
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float px_ = x + 0.5f, py_ = y + 0.5f;
                float d1 = (px_ - bx) * (ay - by) - (ax - bx) * (py_ - by);
                float d2 = (px_ - cx) * (by - cy) - (bx - cx) * (py_ - cy);
                float d3 = (px_ - ax) * (cy - ay) - (cx - ax) * (py_ - ay);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) Set(x, y, paint(x, y));
            }
    }

    // Строковый арт: символ -> цвет из палитры, '.' прозрачный. Строки разной длины допустимы.
    public void Art(int ox, int oy, string[] rows, Dictionary<char, Color32> pal)
    {
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (pal.TryGetValue(rows[y][x], out var c)) Set(ox + x, oy + y, c);
    }

    // Обводка по контуру непрозрачных пикселей
    public void Outline(Color32 c)
    {
        var copy = (Color32[])px.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (copy[y * w + x].a > 0) continue;
                bool n = false;
                if (x > 0 && copy[y * w + x - 1].a > 0) n = true;
                else if (x < w - 1 && copy[y * w + x + 1].a > 0) n = true;
                else if (y > 0 && copy[(y - 1) * w + x].a > 0) n = true;
                else if (y < h - 1 && copy[(y + 1) * w + x].a > 0) n = true;
                if (n) px[y * w + x] = c;
            }
    }

    public void Speckle(int seed, float chance, Color32 c, int x0 = 0, int y0 = 0, int rw = -1, int rh = -1)
    {
        var r = new System.Random(seed);
        if (rw < 0) rw = w; if (rh < 0) rh = h;
        for (int y = y0; y < y0 + rh; y++)
            for (int x = x0; x < x0 + rw; x++)
                if (r.NextDouble() < chance && Get(x, y).a > 0) Set(x, y, c);
    }

    public Texture2D ToTex(float gray)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var o = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = px[y * w + x];
                if (gray > 0 && c.a > 0)
                {
                    byte l = (byte)(0.3f * c.r + 0.59f * c.g + 0.11f * c.b);
                    c = new Color32((byte)Mathf.Lerp(c.r, l, gray), (byte)Mathf.Lerp(c.g, l, gray), (byte)Mathf.Lerp(c.b, l, gray), c.a);
                }
                o[(h - 1 - y) * w + x] = c; // текстура хранится снизу вверх
            }
        t.SetPixels32(o);
        t.Apply();
        return t;
    }
}

public static class Pix
{
    // Насколько мир «выцвел» из-за Стирания: 0 — цветной, 1 — полностью серый
    public static float Gray;
    static float cachedGray = -1;
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, Texture2D> colorCache = new Dictionary<string, Texture2D>();

    public static Color32 C(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    public static Color32 Shade(Color32 c, float k) =>
        new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), c.a);

    // Спрайт мира (зависит от серости мира)
    public static Texture2D Get(string key)
    {
        if (!Mathf.Approximately(cachedGray, Gray))
        {
            foreach (var t in cache.Values) UnityEngine.Object.Destroy(t);
            cache.Clear();
            cachedGray = Gray;
        }
        if (cache.TryGetValue(key, out var tex)) return tex;
        var cv = Art.Draw(key);
        tex = cv != null ? cv.ToTex(Gray) : null;
        cache[key] = tex;
        return tex;
    }

    // Спрайт, который всегда цветной (UI, концовки)
    public static Texture2D Color(string key)
    {
        if (colorCache.TryGetValue(key, out var tex)) return tex;
        var cv = Art.Draw(key);
        tex = cv != null ? cv.ToTex(0) : null;
        colorCache[key] = tex;
        return tex;
    }

    static Texture2D white;
    public static Texture2D White
    {
        get
        {
            if (white == null)
            {
                white = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                white.SetPixel(0, 0, UnityEngine.Color.white);
                white.Apply();
            }
            return white;
        }
    }
}
