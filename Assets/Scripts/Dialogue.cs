using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Speaker
{
    public string name, sprite;
    public float pitch = 1f;
    public Speaker(string name, string sprite, float pitch) { this.name = name; this.sprite = sprite; this.pitch = pitch; }
}

// Диалоговое окно: печать по буквам, голосовые блипы, выбор вариантов
public class Dialogue
{
    public static readonly Dictionary<string, Speaker> Sp = new Dictionary<string, Speaker>
    {
        { "", new Speaker(null, null, 1f) },
        { "noa", new Speaker("Ноа", "noa_d0", 1.25f) },
        { "skrip", new Speaker("Скрип", "skrip0", 1.6f) },
        { "echo", new Speaker("Мадам Эхо", "echo", 0.7f) },
        { "echo_nf", new Speaker("Мадам Эхо", "echo_noface", 0.7f) },
        { "oculus", new Speaker("???", "oculus", 0.45f) },
        { "oculus2", new Speaker("Ноа", "oculus_face", 1.1f) },
        { "mom", new Speaker("Мама", null, 0.95f) },
        { "bro", new Speaker("Тим", null, 0.8f) },
        { "doctor", new Speaker("Голос", null, 0.75f) },
        { "npc", new Speaker("Фоновый", "npc_bg", 1f) },
        { "gardener", new Speaker("Садовница", "npc_garden", 1.1f) },
        { "neonguy", new Speaker("Прохожий", "npc_neon", 1.3f) },
        { "archivist", new Speaker("Библиотекарь", "npc_arch", 0.9f) },
        { "letter", new Speaker("Письмо", "e_letter", 1.3f) },
        { "clock", new Speaker("Часы", "e_clock", 0.9f) },
        { "keeper", new Speaker("Смотритель", "e_keeper", 0.6f) },
        { "tape", new Speaker("Кассета", "e_tape", 1.4f) },
        { "hope", new Speaker("Надежда", "e_hope", 1.2f) },
        { "thorn", new Speaker("Терновая", "e_thorn", 0.8f) },
        { "moth", new Speaker("Мотылёк", "e_moth", 1.7f) },
        { "token", new Speaker("Жетон", "e_token", 1.5f) },
        { "merchant", new Speaker("Торговец", "e_merchant", 0.65f) },
        { "page", new Speaker("Страница", "e_page", 1f) },
        { "system", new Speaker(null, null, 0.5f) },
    };

    public bool Active;
    public int Choice;
    public Rect? forceRect;       // бой задаёт своё окно
    public bool top;              // окно сверху, если игрок внизу экрана

    readonly Queue<string> queue = new Queue<string>();
    Speaker sp;
    string who;
    string text = "";
    float shown;
    float pauseT;
    string[] options;
    bool asking;
    bool italic;

    public IEnumerator Say(string who, params string[] lines)
    {
        Begin(who, lines, null);
        while (Active) yield return null;
    }

    public IEnumerator Ask(string who, string question, params string[] opts)
    {
        Begin(who, new[] { question }, opts);
        while (Active) yield return null;
    }

    // Воспоминание с кассеты: тот же диалог, но без «звёздочки»
    public IEnumerator Memory(string who, params string[] lines)
    {
        italic = true;
        Begin(who, lines, null);
        while (Active) yield return null;
        italic = false;
    }

    void Begin(string who, string[] lines, string[] opts)
    {
        this.who = who;
        sp = Sp.TryGetValue(who, out var s) ? s : Sp[""];
        queue.Clear();
        foreach (var l in lines) queue.Enqueue(l);
        options = opts;
        Choice = 0;
        Active = true;
        Next();
    }

    void Next()
    {
        if (queue.Count == 0) { Active = false; asking = false; return; }
        string raw = queue.Dequeue();
        asking = options != null && queue.Count == 0;
        string prefix = (italic || who == "system") ? "" : "* ";
        text = Wrap(prefix + raw, Portrait != null ? 36 : 46);
        shown = 0;
        pauseT = 0;
    }

    Texture2D Portrait => sp.sprite != null ? Pix.Color(sp.sprite) : null;

    static string Wrap(string s, int max)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var para in s.Split('\n'))
        {
            int col = 0;
            bool first = true;
            foreach (var word in para.Split(' '))
            {
                if (!first && col + 1 + word.Length > max) { sb.Append("\n  "); col = 2; }
                else if (!first) { sb.Append(' '); col++; }
                sb.Append(word);
                col += word.Length;
                first = false;
            }
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }

    bool Full => shown >= text.Length;

    bool Shouting
    {
        get
        {
            int up = 0, letters = 0;
            foreach (char ch in text) if (char.IsLetter(ch)) { letters++; if (char.IsUpper(ch)) up++; }
            return letters > 6 && up > letters * 0.6f;
        }
    }

    public void Update()
    {
        if (!Active) return;
        if (!Full)
        {
            if (pauseT > 0) { pauseT -= Time.deltaTime; }
            else
            {
                int before = (int)shown;
                shown += Time.deltaTime * (who == "system" ? 18f : 38f);
                int after = Mathf.Min((int)shown, text.Length);
                for (int i = before; i < after; i++)
                {
                    char ch = text[i];
                    if (ch == ',' ) pauseT = 0.12f;
                    else if (ch == '.' || ch == '!' || ch == '?' || ch == '…') pauseT = 0.22f;
                    if (i % 2 == 0 && !char.IsWhiteSpace(ch)) Game.I.music.Sfx("blip", 0.55f, sp.pitch * Random.Range(0.94f, 1.06f));
                    if (pauseT > 0) { shown = i + 1; break; }
                }
            }
            if (In.Back && who != "system")
            {
                if (shown < text.Length * 0.5f) Game.I.S.skips++;
                shown = text.Length;
                In.Eat();
            }
            if (In.Ok) In.Eat(); // Z не проматывает, как в Undertale
            return;
        }
        if (asking && In.auto) Choice = Mathf.Min(In.autoChoice, options.Length - 1);
        if (asking)
        {
            if (In.Left || In.Up) { Choice = (Choice + options.Length - 1) % options.Length; Game.I.music.Sfx("select"); }
            if (In.Right || In.Down) { Choice = (Choice + 1) % options.Length; Game.I.music.Sfx("select"); }
        }
        if (In.Ok)
        {
            In.Eat();
            if (asking) Game.I.music.Sfx("confirm");
            Next();
        }
    }

    public void Draw()
    {
        if (!Active) return;
        Rect r = forceRect ?? (top ? new Rect(32, 16, 576, 150) : new Rect(32, 314, 576, 150));
        G.Box(r.x, r.y, r.width, r.height);
        float tx = r.x + 26;
        var p = Portrait;
        if (p != null)
        {
            float s = Mathf.Max(1, Mathf.Floor(Mathf.Min(110f / p.height, 110f / p.width)));
            float talk = Full ? 0 : Mathf.Abs(Mathf.Sin(Time.time * 16)) * -3; // портрет «говорит»
            G.TexC(p, r.x + 70, r.y + r.height / 2 + talk, s);
            tx = r.x + 136;
        }
        if (sp.name != null && !italic)
            G.Text(sp.name, r.x + 14, r.y - 12, 14, new Color(1f, 0.9f, 0.3f));
        Color col = italic ? new Color(0.75f, 0.85f, 1f) : Color.white;
        // Крик (почти всё капслоком) трясётся
        float jx = 0, jy = 0;
        if (Shouting) { jx = Random.Range(-1.5f, 1.5f); jy = Random.Range(-1.5f, 1.5f); }
        G.Text(text.Substring(0, Mathf.Min((int)shown, text.Length)), tx + jx, r.y + 20 + jy, 20, col, TextAnchor.UpperLeft, r.width - (tx - r.x) - 10);
        // Мигающая подсказка «жми Z», когда реплика допечатана
        if (Full && !asking && (int)(Time.unscaledTime * 2) % 2 == 0)
            G.Text("[Z]", r.xMax - 46, r.yMax - 26, 14, new Color(1f, 1f, 1f, 0.6f));
        if (asking && Full)
        {
            float ox = tx + 20;
            float oy = r.y + r.height - 38;
            for (int i = 0; i < options.Length; i++)
            {
                bool sel = i == Choice;
                if (sel) G.Tex(Pix.Color("soul"), ox - 22, oy + 5, 2);
                G.Text(options[i], ox, oy, 20, sel ? new Color(1f, 0.9f, 0.3f) : Color.white);
                ox += 30 + options[i].Length * 12;
            }
        }
    }
}
