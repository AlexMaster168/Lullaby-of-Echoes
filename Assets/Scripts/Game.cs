using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Mode { None, Title, World, Battle, Scene }

[System.Serializable]
public class SaveData
{
    public string area = "dock";
    public float px = -1, py = -1;
    public int hp = 20, maxHp = 20, hums = 3;
    public List<string> flags = new List<string>();
    public List<string> spared = new List<string>();
    public List<string> erased = new List<string>();
    public int skips;
    public float time;
    public int diff = 2;                             // 0 новичок, 1 легко, 2 нормально, 3 трудно

    public bool Has(string f) => flags.Contains(f);
    public void Set(string f) { if (!flags.Contains(f)) flags.Add(f); }
    public bool Done(string id) => spared.Contains(id) || erased.Contains(id);
    public int Void => erased.Count;                 // «ПУСТОТА» — аналог LV
    public int Atk => 6 + erased.Count * 2;          // растёт от стирания, но без «ваншотов»
    public bool AllErased => erased.Count >= Game.TotalEnemies;
}

// Корень игры: создаётся автоматически при запуске любой сцены
public class Game : MonoBehaviour
{
    public static Game I;
    public const float W = 640, H = 480;
    public const int TotalEnemies = 11;
    const string SaveKey = "LoE_save";
    public const string VoidKey = "LoE_void";

    public Mode mode;
    public SaveData S = new SaveData();
    public World world;
    public Battle battle;
    public Dialogue dlg;
    public Music music;
    public float fade = 1f;
    public Color fadeColor = Color.black;
    public float shake;
    public int busy;                    // >0 — идёт катсцена, игрок не управляет
    public System.Action onDraw;        // доп. отрисовка (катсцены, концовки)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I != null) return;
        var go = new GameObject("Game");
        DontDestroyOnLoad(go);
        go.AddComponent<Game>();
    }

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        var cam = Camera.main;
        if (cam == null)
        {
            var cg = new GameObject("Camera");
            DontDestroyOnLoad(cg);
            cam = cg.AddComponent<Camera>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask = 0;
        if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();

        music = gameObject.AddComponent<Music>();
        dlg = new Dialogue();
        world = new World();
        battle = new Battle();
        G.Init();
    }

    void Start()
    {
        AutoTest.ReadArgs();
        if (AutoTest.Mode != null) StartCoroutine(AutoTest.Run());
        StartCoroutine(Screens.Boot());
    }

    void Update()
    {
        In.Tick();
        if (shake > 0) shake -= Time.deltaTime;
        if (mode == Mode.World || mode == Mode.Battle) S.time += Time.deltaTime;
        dlg.Update();
        if (mode == Mode.World) world.Tick(busy == 0 && !dlg.Active);
        else if (mode == Mode.Battle) battle.Tick();
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        G.Begin();
        if (mode == Mode.World) world.Draw();
        else if (mode == Mode.Battle) battle.Draw();
        onDraw?.Invoke();
        dlg.Draw();
        if (fade > 0.001f) G.Rect(-10, -10, W + 20, H + 20, new Color(fadeColor.r, fadeColor.g, fadeColor.b, fade));
    }

    public IEnumerator Fade(float to, float time)
    {
        float from = fade;
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            fade = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        fade = to;
    }

    public static IEnumerator Wait(float s)
    {
        for (float t = 0; t < s; t += Time.deltaTime) yield return null;
    }

    public static IEnumerator WaitOk()
    {
        yield return null;
        while (!In.Ok) yield return null;
        In.Eat();
    }

    // ---- Сохранения ----
    public bool HasSave => PlayerPrefs.HasKey(SaveKey);
    public void SaveGame() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(S)); PlayerPrefs.Save(); }
    public void LoadGame() { S = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)); UpdateGray(); }
    public void DeleteSave() { PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); }

    public void UpdateGray() => Pix.Gray = Mathf.Clamp01(S.erased.Count / (float)TotalEnemies);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

// ---------- Сложность ----------
public static class Diff
{
    public static readonly string[] Names = { "Новичок", "Легко", "Нормально", "Трудно" };
    public static readonly string[] Desc =
    {
        "Бесконечное восстановление ПАМЯТИ: ПЛЕЕР в бою\nили клавиша H в любой момент. Пройдёт каждый.",
        "Больше ПАМЯТИ, слабее и реже атаки.\nДля тех, кто пришёл за историей.",
        "Так, как задумано.\nБоссы заставят попотеть.",
        "Меньше ПАМЯТИ, быстрые и плотные атаки,\nу врагов больше здоровья. Удачи."
    };
    static int D => Mathf.Clamp(Game.I.S.diff, 0, 3);
    static float Pick(float n, float a, float b, float c) => D == 0 ? n : D == 1 ? a : D == 2 ? b : c;
    public static bool Novice => D == 0;

    public static float EnemyHp => Pick(0.6f, 0.7f, 1f, 1.35f);
    public static float EnemyAtk => Pick(0.5f, 0.6f, 1f, 1.5f);
    public static float Density => Pick(0.65f, 0.72f, 1f, 1.3f);   // как часто появляются снаряды
    public static float Speed => Pick(0.8f, 0.85f, 1f, 1.15f);    // как быстро летят
    public static float Invuln => Pick(1.6f, 1.4f, 1f, 0.75f);    // неуязвимость после удара
    public static int MaxHp => D == 0 ? 40 : D == 1 ? 30 : D == 2 ? 20 : 16;
    public static int Hums => D == 0 ? 9 : D == 1 ? 4 : D == 2 ? 3 : 2;
    public static string Name => Names[D];
}

// ---------- Ввод (стрелки/WASD, Z/Enter — ок, X/Shift — назад) ----------
public static class In
{
    static int eaten = -1;
    public static bool auto;          // автотест: бот «нажимает» кнопки
    public static int autoChoice;
    static bool AutoOk => auto && Time.frameCount % 5 == 0;
    static bool AutoBack => auto && Time.frameCount % 5 == 2;
    public static void Tick() { }
    public static void Eat() => eaten = Time.frameCount;
    static bool Free => eaten != Time.frameCount;

    public static bool Ok => Free && (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || AutoOk);
    public static bool Back => Free && (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.Backspace) || AutoBack);
    public static bool BackHeld => Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public static bool Up => Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
    public static bool Down => Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
    public static bool Left => Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
    public static bool Right => Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);

    public static Vector2 Axis
    {
        get
        {
            float x = 0, y = 0;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) y -= 1;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y += 1;
            return new Vector2(x, y);
        }
    }
}

// ---------- Отрисовка в виртуальном экране 640x480 ----------
public static class G
{
    public static float scale = 1, ox, oy;
    static GUIStyle style;
    static Font font;

    public static void Init()
    {
        font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Lucida Console", "Arial" }, 32);
        style = new GUIStyle { font = font, wordWrap = false, richText = false, clipping = TextClipping.Overflow };
    }

    public static void Begin()
    {
        scale = Mathf.Min(Screen.width / Game.W, Screen.height / Game.H);
        ox = (Screen.width - Game.W * scale) / 2f;
        oy = (Screen.height - Game.H * scale) / 2f;
        if (Game.I.shake > 0)
        {
            ox += Random.Range(-4f, 4f) * scale;
            oy += Random.Range(-4f, 4f) * scale;
        }
        GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));
    }

    public static void Rect(float x, float y, float w, float h, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(x, y, w, h), Pix.White);
        GUI.color = Color.white;
    }

    public static void Tex(Texture2D t, float x, float y, float s = 2, bool flip = false, Color? tint = null)
    {
        if (t == null) return;
        GUI.color = tint ?? Color.white;
        var r = new Rect(x, y, t.width * s, t.height * s);
        if (flip) GUI.DrawTextureWithTexCoords(r, t, new Rect(1, 0, -1, 1));
        else GUI.DrawTexture(r, t);
        GUI.color = Color.white;
    }

    // По центру по X, низом к точке y
    public static void TexFoot(Texture2D t, float cx, float footY, float s = 2, bool flip = false, Color? tint = null)
    {
        if (t == null) return;
        Tex(t, Mathf.Round(cx - t.width * s / 2f), Mathf.Round(footY - t.height * s), s, flip, tint);
    }

    // Растяжение по X/Y отдельно — «дыхание», сплющивание при ударе
    public static void TexFootXY(Texture2D t, float cx, float footY, float sx, float sy, bool flip = false, Color? tint = null)
    {
        if (t == null) return;
        float w = t.width * sx, h = t.height * sy;
        GUI.color = tint ?? Color.white;
        var r = new Rect(cx - w / 2f, footY - h, w, h);
        if (flip) GUI.DrawTextureWithTexCoords(r, t, new Rect(1, 0, -1, 1));
        else GUI.DrawTexture(r, t);
        GUI.color = Color.white;
    }

    // Поворот вокруг центра. groupOffset — смещение GUI.BeginGroup, если рисуем внутри группы
    public static void TexRot(Texture2D t, float cx, float cy, float s, float deg, Color? tint = null, Vector2 groupOffset = default)
    {
        if (t == null) return;
        var m = GUI.matrix;
        Vector3 p = new Vector3(cx + groupOffset.x, cy + groupOffset.y, 0);
        GUI.matrix = m * Matrix4x4.Translate(p) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, deg)) * Matrix4x4.Translate(-p);
        TexC(t, cx, cy, s, tint);
        GUI.matrix = m;
    }

    // Толстая линия (для разрезов, лучей)
    public static void Line(float x0, float y0, float x1, float y1, float thick, Color c)
    {
        float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        float deg = Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg;
        var m = GUI.matrix;
        Vector3 p = new Vector3(x0, y0, 0);
        GUI.matrix = m * Matrix4x4.Translate(p) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, deg)) * Matrix4x4.Translate(-p);
        Rect(x0, y0 - thick / 2, len, thick, c);
        GUI.matrix = m;
    }

    // Мягкое свечение
    public static void Glow(float cx, float cy, float r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(cx - r, cy - r, r * 2, r * 2), Pix.GlowTex);
        GUI.color = Color.white;
    }

    public static void TexC(Texture2D t, float cx, float cy, float s = 2, Color? tint = null)
    {
        if (t == null) return;
        Tex(t, cx - t.width * s / 2f, cy - t.height * s / 2f, s, false, tint);
    }

    // Текст рисуется в экранных пикселях, чтобы оставаться чётким
    public static void Text(string s, float x, float y, int size, Color c, TextAnchor a = TextAnchor.UpperLeft, float w = 600, float h = 300)
    {
        var m = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        style.fontSize = Mathf.Max(1, Mathf.RoundToInt(size * scale));
        style.normal.textColor = c;
        style.alignment = a;
        GUI.Label(new Rect(m.m03 + x * scale, m.m13 + y * scale, w * scale, h * scale), s, style);
        GUI.matrix = m;
    }

    public static void Center(string s, float y, int size, Color c) => Text(s, 0, y, size, c, TextAnchor.UpperCenter, Game.W);

    public static void Box(float x, float y, float w, float h, float border = 3)
    {
        Rect(x, y, w, h, Color.white);
        Rect(x + border, y + border, w - border * 2, h - border * 2, Color.black);
    }
}

// ---------- Музыка и звуки ----------
public class Music : MonoBehaviour
{
    AudioSource a, b, cur;
    readonly List<AudioSource> sfx = new List<AudioSource>();
    int sfxIdx;
    public string current;
    public float musicVol = 0.8f;
    public float pitchMul = 1f;    // чем больше стёрто, тем ниже и «тягучее» музыка

    AudioSource Make()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        return s;
    }

    void Awake()
    {
        a = Make(); b = Make(); cur = a;
        for (int i = 0; i < 6; i++) sfx.Add(Make());
    }

    public AudioSource Cur => cur;

    public void Play(string key, float fadeTime = 0.6f)
    {
        if (key == current && cur.isPlaying) return;
        current = key;
        StopAllCoroutines();
        StartCoroutine(Cross(key, fadeTime));
    }

    IEnumerator Cross(string key, float time)
    {
        var old = cur;
        var next = cur == a ? b : a;
        next.clip = Tracks.Get(key);
        next.loop = true;
        next.pitch = pitchMul;
        next.volume = 0;
        next.Play();
        cur = next;
        float oldVol = old.volume;
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            next.volume = Mathf.Lerp(0, musicVol, t / time);
            old.volume = Mathf.Lerp(oldVol, 0, t / time);
            yield return null;
        }
        next.volume = musicVol;
        old.Stop();
    }

    public void Stop(float time = 0.5f)
    {
        current = null;
        StopAllCoroutines();
        StartCoroutine(FadeOut(time));
    }

    IEnumerator FadeOut(float time)
    {
        var s = cur;
        float v = s.volume;
        for (float t = 0; t < time; t += Time.deltaTime) { s.volume = Mathf.Lerp(v, 0, t / time); yield return null; }
        s.Stop();
        (s == a ? b : a).Stop();
    }

    public void Sfx(string key, float vol = 1f, float pitch = 1f)
    {
        var s = sfx[sfxIdx = (sfxIdx + 1) % sfx.Count];
        s.pitch = pitch;
        s.PlayOneShot(Tracks.Get(key), vol);
    }

    // Проигрывает звук целиком и возвращает его длительность
    public float PlayClip(string key, float vol = 1f)
    {
        var clip = Tracks.Get(key);
        var s = sfx[sfxIdx = (sfxIdx + 1) % sfx.Count];
        s.pitch = 1;
        s.PlayOneShot(clip, vol);
        return clip.length;
    }

    public void StopSfx() { foreach (var s in sfx) s.Stop(); }
}
