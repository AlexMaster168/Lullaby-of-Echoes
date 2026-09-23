using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Объект на карте: NPC, Забытый, точка сохранения, триггер
public class Ent
{
    public string id, sprite;
    public float x, y;                 // позиция «ног» в пикселях карты (тайл = 16)
    public bool solid = true, anim, flip, hidden, floaty;
    public float scale = 2f;
    public float tw = 10, th = 10;     // размер зоны касания
    public Func<IEnumerator> onTalk, onTouch;
    public bool touching;
    public Color tint = Color.white;
}

public class Area
{
    public string id, zone, music, title;
    public string[] map;
    public Dictionary<char, Func<float, float, Ent>> spawns = new Dictionary<char, Func<float, float, Ent>>();
    public Func<IEnumerator> onEnter;
}

public class World
{
    public const int T = 16;       // размер тайла в пикселях карты
    const float Z = 2f;            // масштаб на экране
    public Area area;
    public Vector2 pos;
    public char facing = 'd';
    public readonly List<Ent> ents = new List<Ent>();
    public bool skripFollow;
    public bool showStats;

    int mw, mh;
    char[,] grid;
    float walkT;
    bool moving;
    float camX, camY;
    float titleT;
    readonly List<Vector2> trail = new List<Vector2>();
    Vector2 skripPos;

    // ---------- Загрузка ----------
    public void Load(Area a, Vector2? at = null)
    {
        area = a;
        ents.Clear();
        trail.Clear();
        mh = a.map.Length;
        mw = 0;
        foreach (var r in a.map) mw = Mathf.Max(mw, r.Length);
        grid = new char[mw, mh];
        Vector2 start = new Vector2(mw * T / 2f, mh * T / 2f);
        for (int y = 0; y < mh; y++)
            for (int x = 0; x < mw; x++)
            {
                char ch = x < a.map[y].Length ? a.map[y][x] : ' ';
                float fx = x * T + T / 2f, fy = y * T + T - 2;
                if (ch == '@') { start = new Vector2(fx, fy); ch = '.'; }
                else if (a.spawns.TryGetValue(ch, out var f))
                {
                    var e = f(fx, fy);
                    if (e != null) ents.Add(e);
                    ch = '.';
                }
                grid[x, y] = ch;
            }
        pos = at ?? start;
        skripPos = pos;
        titleT = 0;
        Game.I.UpdateGray();
        SnapCamera();
    }

    public Ent Find(string id) => ents.Find(e => e.id == id);
    public void Remove(string id) => ents.RemoveAll(e => e.id == id);

    // ---------- Тайлы ----------
    string AltFloor => area.zone == "dock" ? "street" : area.zone == "garden" ? "grass" : area.zone == "neon" ? "road0" : "carpet";

    bool TileSolid(char ch)
    {
        switch (ch)
        {
            case '#': case 'W': case '~': case ' ': case 'L': case 'C': case 'o': case 'p': case 'n': case 'g': case 'P': return true;
        }
        return false;
    }

    void TileKeys(char ch, int x, int y, out string bas, out string deco)
    {
        string z = "t_" + area.zone + "_";
        int v = ((x * 7 + y * 13) & 0xff) % 3;
        int wf = ((int)(Time.time * 2)) % 2;
        bas = z + "floor" + v;
        deco = null;
        switch (ch)
        {
            case '#': bas = z + "wall0"; break;
            case 'W': bas = z + "wall1"; break;
            case '~': bas = area.zone == "arch" ? z + "void0" : z + "water" + wf; break;
            case ',': bas = z + AltFloor; break;
            case '=': bas = z + "road0"; break;
            case '|': bas = z + "road1"; break;
            case ' ': bas = null; break;
            case 'L': deco = "t_dock_lamp"; break;
            case 'C': bas = "t_dock_clock"; break;
            case 'l': deco = "t_dock_letters"; break;
            case 'o': deco = "t_dock_post"; break;
            case 'f': deco = "t_garden_flower0"; break;
            case 'F': deco = "t_garden_flower1"; break;
            case 'p': deco = "t_garden_pot"; break;
            case 'n': deco = "t_neon_sign"; break;
            case 'g': deco = "t_neon_gear" + ((int)(Time.time * 3) % 2); break;
            case 'b': deco = "t_arch_papers"; break;
            case 'P': deco = "t_arch_pedestal"; break;
        }
    }

    bool NearAlt(int x, int y) =>
        (x > 0 && grid[x - 1, y] == ',') || (x < mw - 1 && grid[x + 1, y] == ',') ||
        (y > 0 && grid[x, y - 1] == ',') || (y < mh - 1 && grid[x, y + 1] == ',');

    bool Solid(float x, float y)
    {
        int tx = Mathf.FloorToInt(x / T), ty = Mathf.FloorToInt(y / T);
        if (tx < 0 || ty < 0 || tx >= mw || ty >= mh) return true;
        if (TileSolid(grid[tx, ty])) return true;
        foreach (var e in ents)
        {
            if (!e.solid || e.hidden) continue;
            if (Mathf.Abs(x - e.x) < 9 && y > e.y - 9 && y < e.y + 2) return true;
        }
        return false;
    }

    bool Blocked(float x, float y) =>
        Solid(x - 5, y - 5) || Solid(x + 5, y - 5) || Solid(x - 5, y) || Solid(x + 5, y);

    // ---------- Логика ----------
    public void Tick(bool control)
    {
        titleT += Time.deltaTime;
        moving = false;
        if (control)
        {
            if (Input.GetKeyDown(KeyCode.C)) { showStats = !showStats; Game.I.music.Sfx("select"); }
            if (showStats) { if (In.Back || In.Ok) { showStats = false; In.Eat(); } return; }

            var ax = In.Axis;
            if (ax != Vector2.zero)
            {
                float sp = 72f * Time.deltaTime;
                if (ax.x != 0 && !Blocked(pos.x + ax.x * sp, pos.y)) pos.x += ax.x * sp;
                if (ax.y != 0 && !Blocked(pos.x, pos.y + ax.y * sp)) pos.y += ax.y * sp;
                if (ax.y > 0) facing = 'd'; else if (ax.y < 0) facing = 'u';
                if (ax.x > 0) facing = 'r'; else if (ax.x < 0) facing = 'l';
                moving = true;
                walkT += Time.deltaTime;
                trail.Add(pos);
                if (trail.Count > 60) trail.RemoveAt(0);
            }
            if (In.Ok) TryTalk();
            CheckTouch();
        }
        if (skripFollow)
        {
            Vector2 target = trail.Count > 16 ? trail[trail.Count - 16] : pos + new Vector2(-14, 0);
            skripPos = Vector2.Lerp(skripPos, target, Time.deltaTime * 6f);
        }
        UpdateCamera(false);
        Game.I.dlg.top = (pos.y - camY) * Z > 300;
    }

    void TryTalk()
    {
        // Ось Y карты направлена вниз
        Vector2 d = facing == 'd' ? new Vector2(0, 1) : facing == 'u' ? new Vector2(0, -1) : facing == 'l' ? Vector2.left : Vector2.right;
        Vector2 probe = pos + d * 13 + new Vector2(0, -3);
        foreach (var e in ents)
        {
            if (e.hidden || e.onTalk == null) continue;
            float hw = e.solid ? 12 : 10;
            if (Mathf.Abs(probe.x - e.x) < hw && probe.y > e.y - 20 && probe.y < e.y + 8)
            {
                In.Eat();
                Story.Run(e.onTalk());
                return;
            }
        }
    }

    void CheckTouch()
    {
        foreach (var e in ents.ToArray())
        {
            if (e.hidden || e.onTouch == null) continue;
            bool t = Mathf.Abs(pos.x - e.x) < e.tw && Mathf.Abs(pos.y - e.y) < e.th;
            if (t && !e.touching)
            {
                e.touching = true;
                Story.Run(e.onTouch());
                return;
            }
            if (!t) e.touching = false;
        }
    }

    void SnapCamera() => UpdateCamera(true);

    void UpdateCamera(bool snap)
    {
        float vw = Game.W / Z, vh = Game.H / Z;
        float tx = pos.x - vw / 2, ty = pos.y - 12 - vh / 2;
        tx = mw * T <= vw ? (mw * T - vw) / 2 : Mathf.Clamp(tx, 0, mw * T - vw);
        ty = mh * T <= vh ? (mh * T - vh) / 2 : Mathf.Clamp(ty, 0, mh * T - vh);
        if (snap) { camX = tx; camY = ty; }
        else { camX = Mathf.Lerp(camX, tx, Time.deltaTime * 10); camY = Mathf.Lerp(camY, ty, Time.deltaTime * 10); }
    }

    public Vector2 ToScreen(float x, float y) => new Vector2(Mathf.Round((x - camX) * Z), Mathf.Round((y - camY) * Z));

    // ---------- Отрисовка ----------
    public void Draw()
    {
        if (area == null) return;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(camX / T)), y0 = Mathf.Max(0, Mathf.FloorToInt(camY / T));
        int x1 = Mathf.Min(mw - 1, x0 + 21), y1 = Mathf.Min(mh - 1, y0 + 16);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                TileKeys(grid[x, y], x, y, out var b, out var d);
                var s = ToScreen(x * T, y * T);
                if (b != null)
                {
                    if (d != null) // под декором — такой же пол, как у соседей
                        G.Tex(Pix.Get(NearAlt(x, y) ? "t_" + area.zone + "_" + AltFloor : "t_" + area.zone + "_floor" + (((x * 7 + y * 13) & 0xff) % 3)), s.x, s.y, Z);
                    else G.Tex(Pix.Get(b), s.x, s.y, Z);
                }
                if (d != null) G.Tex(Pix.Get(d), s.x, s.y, Z);
            }

        // Объекты сортируются по Y
        var order = new List<(float y, Action draw)>();
        foreach (var e in ents)
        {
            if (e.hidden || e.sprite == null) continue;
            var ee = e;
            order.Add((e.y, () => DrawEnt(ee)));
        }
        order.Add((pos.y, DrawPlayer));
        if (skripFollow) order.Add((skripPos.y - 1, DrawSkrip));
        order.Sort((a, b) => a.y.CompareTo(b.y));
        foreach (var o in order) o.draw();

        DrawOverlay();

        if (titleT < 3.5f && area.title != null)
        {
            float a = Mathf.Clamp01(titleT < 1 ? titleT : (3.5f - titleT) / 1.5f);
            G.Center(area.title, 40, 26, new Color(1, 1, 1, a));
        }
        if (showStats) DrawStats();
    }

    void DrawEnt(Ent e)
    {
        string key = e.anim ? e.sprite + ((int)(Time.time * 4) % (e.sprite == "static" ? 3 : 2)) : e.sprite;
        var tex = Pix.Get(key);
        if (tex == null) return;
        float bob = e.floaty ? Mathf.Sin(Time.time * 2.5f + e.x) * 3 - 6 : 0;
        var s = ToScreen(e.x, e.y);
        // тень
        G.Rect(s.x - 10, s.y - 3, 20, 4, new Color(0, 0, 0, 0.3f));
        G.TexFoot(tex, s.x, s.y + bob, e.scale, e.flip, e.tint);
    }

    void DrawPlayer()
    {
        int f = moving ? ((int)(walkT * 6) % 2) : 0;
        char dir = facing == 'l' || facing == 'r' ? 's' : facing;
        var s = ToScreen(pos.x, pos.y);
        G.TexFoot(Pix.Get("noa_" + dir + f), s.x, s.y + 2, Z, facing == 'l');
    }

    void DrawSkrip()
    {
        var s = ToScreen(skripPos.x, skripPos.y);
        int f = (int)(Time.time * 4) % 2;
        G.TexFoot(Pix.Get("skrip" + f), s.x, s.y - 18 + Mathf.Sin(Time.time * 3) * 4, Z, pos.x < skripPos.x);
    }

    static Texture2D fog;

    void DrawOverlay()
    {
        float t = Time.time;
        switch (area.zone)
        {
            case "dock":
                if (fog == null) fog = MakeFog();
                GUI.color = new Color(0.8f, 0.85f, 1f, 0.22f * (1 - Pix.Gray * 0.5f));
                float ox = -(t * 12) % 256;
                for (int i = 0; i < 4; i++) GUI.DrawTexture(new Rect(ox + i * 256, 0, 256, 480), fog);
                GUI.color = Color.white;
                break;
            case "garden":
                for (int i = 0; i < 24; i++)
                {
                    float px = (i * 97 + t * (8 + i % 5)) % 660 - 10;
                    float py = (i * 53 + Mathf.Sin(t + i) * 20 - t * 6 * (1 + i % 3)) % 480;
                    if (py < 0) py += 480;
                    G.Rect(px, py, 3, 3, new Color(1f, 0.95f, 0.6f, 0.35f));
                }
                break;
            case "neon":
                for (int y = 0; y < 480; y += 4) G.Rect(0, y, 640, 1, new Color(0, 0, 0, 0.12f));
                break;
            case "arch":
                G.Rect(0, 0, 640, 40, new Color(0, 0, 0, 0.35f));
                G.Rect(0, 440, 640, 40, new Color(0, 0, 0, 0.35f));
                break;
        }
        if (Pix.Gray > 0.05f)
        {
            // Мир теряет детали: помехи
            var r = new System.Random((int)(t * 10));
            int n = (int)(Pix.Gray * 30);
            for (int i = 0; i < n; i++)
                G.Rect(r.Next(640), r.Next(480), r.Next(2, 30), 2, new Color(0.5f, 0.5f, 0.5f, 0.25f));
        }
    }

    static Texture2D MakeFog()
    {
        var tex = new Texture2D(128, 240) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var px = new Color[128 * 240];
        for (int y = 0; y < 240; y++)
            for (int x = 0; x < 128; x++)
            {
                // бесшовность по X: смешиваем шум с его копией, сдвинутой на период
                float k = x / 128f;
                float v = FogNoise(x, y) * (1 - k) + FogNoise(x - 128, y) * k;
                px[y * 128 + x] = new Color(1, 1, 1, Mathf.Clamp01((v - 0.35f) * 2.2f));
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static float FogNoise(float x, float y) =>
        Mathf.PerlinNoise(x / 32f + 50, y / 24f) * 0.7f + Mathf.PerlinNoise(x / 9f + 55, y / 9f) * 0.3f;

    void DrawStats()
    {
        var S = Game.I.S;
        G.Box(40, 40, 260, 200);
        G.Text("НОА", 64, 60, 24, Color.white);
        G.Text($"ПАМЯТЬ  {S.hp} / {S.maxHp}", 64, 100, 18, Color.white);
        G.Text($"ПУСТОТА {S.Void}", 64, 128, 18, S.Void > 0 ? new Color(0.7f, 0.7f, 0.7f) : Color.white);
        G.Text($"КАССЕТЫ {Story.TapeCount}/4", 64, 156, 18, Color.white);
        G.Text($"НАПЕВЫ  {S.hums}", 64, 184, 18, Color.white);
        G.Text("[C] закрыть", 64, 212, 14, new Color(0.6f, 0.6f, 0.6f));
    }
}
