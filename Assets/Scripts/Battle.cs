using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Act
{
    public string name;
    public int calm;
    public string[] text;
    public string reply;       // что ответит Забытый в этот ход
    public string need;        // требуется флаг, выставленный другим действием
    public string set;         // флаг, который выставит действие
    public bool once;
}

public class EnemyDef
{
    public string id, name, sprite, voice, bullet = "b_dot", music = "battle";
    public int hp = 30, atk = 3, minTurns;
    public bool boss;
    public string check, intro;
    public List<Act> acts = new List<Act>();
    public string[] flavor, talk, calmTalk, patterns = { "rain" };
    public string[] sparedText, erasedText, lastWords;
    public float boxW = 170, boxH = 140;
}

public enum BResult { Spared, Erased, Lost }

public class Bullet
{
    public Vector2 p, v, acc, size;
    public string spr;
    public float t, life = 8, delay, r = 4;
    public Action<Bullet> upd;
    public Color tint = Color.white;
    public bool dead;
}

public class Battle
{
    public EnemyDef E;
    public BResult result;
    int ehp, calm, turn, btn;
    bool eraseOnly;
    readonly HashSet<string> flags = new HashSet<string>();

    enum St { None, Menu, Sub, Attack, Dodge }
    St st;
    List<string> subItems = new List<string>();
    List<bool> subYellow = new List<bool>();
    int sub;

    string flavor = "";
    float flavorShown;
    string bubble;
    float bubbleShown;

    static readonly Rect Full = new Rect(32, 250, 576, 140);
    Rect box = Full, boxTarget = Full;

    Vector2 soul;
    float inv;
    readonly List<Bullet> bullets = new List<Bullet>();

    float atkCursor = -1, slashT = -1, dmgT = -1, hpShown, hpBarT = -1;
    string dmgText;
    float enemyAlpha = 1, dissolve = -1, enemyShake;
    float soulBreak = -1;

    // Автотест
    public static bool Autopilot, EraseAll;
    public bool InMenu => st == St.Menu;
    public bool InDodge => st == St.Dodge;
    public int BulletCount => bullets.Count;

    static readonly string[] Buttons = { "СТЕРЕТЬ", "СЛУШАТЬ", "ПЛЕЕР", "ОТПУСТИТЬ" };
    Game Gm => Game.I;
    SaveData S => Game.I.S;

    // ===================== ПОТОК БОЯ =====================
    public IEnumerator Fight(EnemyDef e, bool onlyErase = false)
    {
        E = e;
        eraseOnly = onlyErase;
        ehp = e.hp; hpShown = e.hp;
        calm = 0; turn = 0; btn = eraseOnly ? 0 : 1;
        flags.Clear(); bullets.Clear();
        enemyAlpha = 1; dissolve = -1; soulBreak = -1; slashT = -1; dmgT = -1; hpBarT = -1; atkCursor = -1;
        box = boxTarget = Full;
        st = St.None;
        bubble = null;

        // Вспышка встречи
        Gm.music.Stop(0.1f);
        Gm.music.Sfx("encounter");
        for (int i = 0; i < 3; i++)
        {
            Gm.fade = 1; yield return Game.Wait(0.07f);
            Gm.fade = 0; yield return Game.Wait(0.07f);
        }
        Gm.fade = 1;
        Gm.mode = Mode.Battle;
        Gm.dlg.forceRect = Full;
        yield return Gm.Fade(0, 0.3f);
        if (e.music != null) Gm.music.Play(e.music, 0.2f);
        SetFlavor(e.intro);

        while (true)
        {
            // ---------- ХОД ИГРОКА ----------
            int action = -1, pick = -1;
            if (Autopilot)
            {
                st = St.Menu;
                yield return Game.Wait(1.5f);
                if (EraseAll || eraseOnly) { action = 0; pick = 0; }
                else if (Spareable) { action = 3; pick = 0; }
                else if (AvailableActs().Count > 0) { action = 1; pick = 1; }
                else { action = 1; pick = 0; }
                btn = action;
            }
            while (action < 0)
            {
                st = St.Menu;
                yield return null;
                while (!In.Ok)
                {
                    if (In.Left) { btn = (btn + 3) % 4; Gm.music.Sfx("select"); }
                    if (In.Right) { btn = (btn + 1) % 4; Gm.music.Sfx("select"); }
                    yield return null;
                }
                In.Eat();
                if (eraseOnly && btn != 0) { Gm.music.Sfx("hurt", 0.4f); continue; }
                Gm.music.Sfx("confirm");
                BuildSub();
                st = St.Sub; sub = 0;
                yield return null;
                while (true)
                {
                    if (In.Up) { sub = (sub + subItems.Count - 1) % subItems.Count; Gm.music.Sfx("select"); }
                    if (In.Down) { sub = (sub + 1) % subItems.Count; Gm.music.Sfx("select"); }
                    if (In.Back) { In.Eat(); break; }
                    if (In.Ok) { In.Eat(); Gm.music.Sfx("confirm"); action = btn; pick = sub; break; }
                    yield return null;
                }
            }
            st = St.None;

            string reply = null;
            if (action == 0)
            {
                yield return Attack();
                if (ehp <= 0) { yield return EraseEnemy(); result = BResult.Erased; break; }
            }
            else if (action == 1)
            {
                if (pick == 0)
                {
                    yield return Gm.dlg.Say("", $"{E.name.ToUpper()} — АТК {E.atk}.\n{E.check}");
                }
                else
                {
                    var act = AvailableActs()[pick - 1];
                    yield return Gm.dlg.Say("", act.text);
                    int before = calm;
                    calm = Mathf.Min(100, calm + act.calm);
                    if (act.set != null) flags.Add(act.set);
                    if (act.once) flags.Add("done:" + act.name);
                    reply = act.reply;
                    if (before < 100 && calm >= 100 && turn + 1 >= E.minTurns)
                        yield return Gm.dlg.Say("", $"{E.name} больше не боится.\nЕго можно ОТПУСТИТЬ.");
                }
            }
            else if (action == 2)
            {
                if (S.hums > 0)
                {
                    S.hums--;
                    int heal = Mathf.Min(12, S.maxHp - S.hp);
                    S.hp += heal;
                    Gm.music.Sfx("heal");
                    yield return Gm.dlg.Say("", "Ты нажимаешь PLAY и тихо напеваешь колыбельную.", $"Восстановлено {heal} ПАМЯТИ.");
                }
                else yield return Gm.dlg.Say("", "Плеер щёлкает вхолостую.\nНапевы кончились. Найди точку сохранения.");
            }
            else if (action == 3)
            {
                if (Spareable)
                {
                    yield return SpareEnemy();
                    result = BResult.Spared;
                    break;
                }
                yield return Gm.dlg.Say("", $"{E.name} ещё не готов(о) уйти.\nСначала его нужно ВЫСЛУШАТЬ.");
            }

            // ---------- ХОД ПРОТИВНИКА ----------
            string line = reply ?? (calm >= 100 && E.calmTalk != null ? Pick(E.calmTalk, turn) : Pick(E.talk, turn));
            if (line != null) yield return Bubble(line);
            yield return Dodge();
            if (S.hp <= 0) { yield return Die(); result = BResult.Lost; break; }
            turn++;
            SetFlavor(Spareable ? $"{E.name} ждёт, когда ты его отпустишь." : Pick(E.flavor, turn));
        }

        st = St.None;
        bullets.Clear();
        Gm.dlg.forceRect = null;
        Gm.UpdateGray();
    }

    bool Spareable => calm >= 100 && turn >= E.minTurns;

    static string Pick(string[] arr, int i) => arr == null || arr.Length == 0 ? null : arr[Mathf.Min(i, arr.Length - 1)];

    List<Act> AvailableActs() =>
        E.acts.FindAll(a => (a.need == null || flags.Contains(a.need)) && !flags.Contains("done:" + a.name));

    void BuildSub()
    {
        subItems.Clear(); subYellow.Clear();
        switch (btn)
        {
            case 0: subItems.Add(E.name); subYellow.Add(Spareable); break;
            case 1:
                subItems.Add("Изучить"); subYellow.Add(false);
                foreach (var a in AvailableActs()) { subItems.Add(a.name); subYellow.Add(false); }
                break;
            case 2: subItems.Add($"Напеть колыбельную ({S.hums})"); subYellow.Add(false); break;
            case 3: subItems.Add("Отпустить"); subYellow.Add(Spareable); break;
        }
    }

    void SetFlavor(string s) { flavor = s == null ? "" : "* " + s; flavorShown = 0; }

    IEnumerator Attack()
    {
        st = St.Attack;
        atkCursor = 0;
        bool hit = false;
        float acc = 0;
        yield return null;
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            atkCursor = t / 1.5f;
            if (Autopilot ? atkCursor >= 0.5f : In.Ok) { In.Eat(); hit = true; acc = 1 - Mathf.Abs(atkCursor - 0.5f) * 2; break; }
            yield return null;
        }
        if (!hit)
        {
            dmgText = "ПРОМАХ"; dmgT = 0;
            yield return Game.Wait(0.9f);
        }
        else
        {
            slashT = 0;
            Gm.music.Sfx("slash");
            yield return Game.Wait(0.35f);
            int dmg = Mathf.Max(1, Mathf.RoundToInt(S.Atk * (0.5f + 1.5f * acc) * UnityEngine.Random.Range(0.9f, 1.1f)));
            ehp = Mathf.Max(0, ehp - dmg);
            dmgText = dmg.ToString(); dmgT = 0; hpBarT = 0;
            enemyShake = 0.5f;
            Gm.music.Sfx("hurt", 0.6f, 1.4f);
            yield return Game.Wait(1.1f);
        }
        atkCursor = -1;
        slashT = -1;
        st = St.None;
    }

    IEnumerator EraseEnemy()
    {
        if (E.lastWords != null) foreach (var l in E.lastWords) yield return Bubble(l);
        Gm.music.Stop(0.3f);
        Gm.music.Sfx("erase");
        for (float t = 0; t < 1.4f; t += Time.deltaTime) { dissolve = t / 1.4f; yield return null; }
        dissolve = 1;
        S.erased.Add(E.id);
        yield return Gm.dlg.Say("", E.erasedText ?? new[] { $"{E.name} рассыпается в белый шум." });
        yield return Gm.dlg.Say("", $"ПУСТОТА выросла до {S.Void}.");
    }

    IEnumerator SpareEnemy()
    {
        Gm.music.Sfx("spare");
        for (float t = 0; t < 0.6f; t += Time.deltaTime) { enemyAlpha = Mathf.Lerp(1, 0.4f, t / 0.6f); yield return null; }
        S.spared.Add(E.id);
        yield return Gm.dlg.Say("", E.sparedText ?? new[] { $"Ты отпускаешь {E.name}." });
    }

    IEnumerator Bubble(string line)
    {
        bubble = line;
        bubbleShown = 0;
        float pitch = Dialogue.Sp.TryGetValue(E.voice ?? "", out var sp) ? sp.pitch : 1f;
        yield return null;
        while (true)
        {
            if (bubbleShown < bubble.Length)
            {
                int b = (int)bubbleShown;
                bubbleShown += Time.deltaTime * 32f;
                if ((int)bubbleShown != b && b % 2 == 0) Gm.music.Sfx("blip", 0.5f, pitch * UnityEngine.Random.Range(0.94f, 1.06f));
                if (In.Back) { bubbleShown = bubble.Length; In.Eat(); }
            }
            else if (In.Ok) { In.Eat(); break; }
            if (In.Ok) In.Eat();
            yield return null;
        }
        bubble = null;
    }

    // ===================== УКЛОНЕНИЕ =====================
    IEnumerator Dodge()
    {
        st = St.Dodge;
        boxTarget = new Rect(320 - E.boxW / 2, 390 - E.boxH, E.boxW, E.boxH);
        yield return Game.Wait(0.3f);
        box = boxTarget;
        soul = new Vector2(box.center.x, box.yMax - 24);
        inv = 0;
        bullets.Clear();
        string pat = Pick(E.patterns, turn);
        if (calm >= 100) pat = "gentle";
        var running = new List<Coroutine>();
        foreach (var p in pat.Split('+')) running.Add(Gm.StartCoroutine(Pattern(p)));
        float dur = E.boss ? 7f : 5.5f;
        for (float t = 0; t < dur && S.hp > 0; t += Time.deltaTime)
        {
            MoveSoul();
            StepBullets();
            yield return null;
        }
        foreach (var c in running) Gm.StopCoroutine(c);
        bullets.Clear();
        if (S.hp > 0)
        {
            boxTarget = Full;
            yield return Game.Wait(0.3f);
            box = Full;
        }
        st = St.None;
    }

    void MoveSoul()
    {
        var ax = In.Axis;
        float sp = (In.BackHeld ? 80f : 150f) * Time.deltaTime;
        soul += ax * sp;
        soul.x = Mathf.Clamp(soul.x, box.xMin + 9, box.xMax - 9);
        soul.y = Mathf.Clamp(soul.y, box.yMin + 9, box.yMax - 9);
        if (inv > 0) inv -= Time.deltaTime;
    }

    void StepBullets()
    {
        float dt = Time.deltaTime;
        foreach (var b in bullets.ToArray())
        {
            b.t += dt;
            b.v += b.acc * dt;
            b.p += b.v * dt;
            b.upd?.Invoke(b);
            if (b.t > b.life || !new Rect(box.x - 120, box.y - 120, box.width + 240, box.height + 240).Contains(b.p)) b.dead = true;
            if (b.dead) { bullets.Remove(b); continue; }
            if (b.t < b.delay || inv > 0) continue;
            bool hit = b.size != Vector2.zero
                ? new Rect(b.p.x - b.size.x / 2, b.p.y - b.size.y / 2, b.size.x, b.size.y).Contains(soul)
                : (b.p - soul).sqrMagnitude < (b.r + 5) * (b.r + 5);
            if (hit && !Autopilot)
            {
                S.hp = Mathf.Max(0, S.hp - E.atk);
                inv = 1f;
                Gm.music.Sfx("hurt");
                Gm.shake = 0.15f;
            }
        }
    }

    Bullet Spawn(float x, float y, float vx, float vy, string spr = null)
    {
        var b = new Bullet { p = new Vector2(x, y), v = new Vector2(vx, vy), spr = spr ?? E.bullet };
        bullets.Add(b);
        return b;
    }

    static float R(float a, float b) => UnityEngine.Random.Range(a, b);

    Vector2 Edge()
    {
        switch (UnityEngine.Random.Range(0, 4))
        {
            case 0: return new Vector2(R(box.xMin, box.xMax), box.yMin - 6);
            case 1: return new Vector2(R(box.xMin, box.xMax), box.yMax + 6);
            case 2: return new Vector2(box.xMin - 6, R(box.yMin, box.yMax));
            default: return new Vector2(box.xMax + 6, R(box.yMin, box.yMax));
        }
    }

    IEnumerator Pattern(string name)
    {
        float k = E.boss ? 1.2f : 1f;
        yield return Game.Wait(0.3f);
        while (true)
        {
            switch (name)
            {
                case "rain":
                    Spawn(R(box.xMin, box.xMax), box.yMin - 6, R(-10, 10), R(90, 140) * k);
                    yield return Game.Wait(0.2f / k);
                    break;
                case "sway": // падающие листы/лепестки качаются
                    {
                        var b = Spawn(R(box.xMin, box.xMax), box.yMin - 6, 0, R(60, 90) * k);
                        float x0 = b.p.x, ph = R(0, 6);
                        b.upd = bb => bb.p.x = x0 + Mathf.Sin(bb.t * 3 + ph) * 22;
                        yield return Game.Wait(0.22f / k);
                    }
                    break;
                case "side":
                    {
                        bool left = UnityEngine.Random.value < 0.5f;
                        Spawn(left ? box.xMin - 6 : box.xMax + 6, R(box.yMin + 6, box.yMax - 6), (left ? 120 : -120) * k, 0);
                        yield return Game.Wait(0.28f / k);
                    }
                    break;
                case "aimed":
                    {
                        var e = Edge();
                        var d = (soul - e).normalized * 125 * k;
                        Spawn(e.x, e.y, d.x, d.y);
                        yield return Game.Wait(0.45f / k);
                    }
                    break;
                case "ring":
                    {
                        float cx = R(box.xMin + 20, box.xMax - 20), cy = box.yMin + 6;
                        float off = R(0, 1);
                        for (int i = 0; i < 12; i++)
                        {
                            float a = (i + off) / 12f * Mathf.PI * 2;
                            Spawn(cx, cy, Mathf.Cos(a) * 80 * k, Mathf.Sin(a) * 80 * k);
                        }
                        yield return Game.Wait(1.1f / k);
                    }
                    break;
                case "wall": // стена с просветом
                    {
                        float gap = R(box.xMin + 24, box.xMax - 24);
                        for (float x = box.xMin + 4; x < box.xMax; x += 15)
                            if (Mathf.Abs(x - gap) > 22) Spawn(x, box.yMin - 6, 0, 75 * k);
                        yield return Game.Wait(1.3f / k);
                    }
                    break;
                case "sweep": // стрелка часов
                    {
                        var c = box.center;
                        float dir = UnityEngine.Random.value < 0.5f ? 1 : -1;
                        for (int i = 1; i <= 6; i++)
                        {
                            float rr = i * 12;
                            var b = Spawn(c.x, c.y, 0, 0);
                            b.life = 20;
                            b.upd = bb => { float a = -Mathf.PI / 2 + bb.t * 1.3f * k * dir; bb.p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr; };
                        }
                        while (true)
                        {
                            yield return Game.Wait(0.9f);
                            Spawn(R(box.xMin, box.xMax), box.yMin - 6, 0, 70, "b_drop");
                        }
                    }
                case "rise": // шипы снизу с предупреждением
                    {
                        float x = UnityEngine.Random.value < 0.5f ? soul.x : R(box.xMin + 8, box.xMax - 8);
                        var b = Spawn(x, box.yMax - 5, 0, 0, "b_thorn");
                        b.delay = 0.55f;
                        b.upd = bb => { if (bb.t >= bb.delay) bb.v = new Vector2(0, -280 * k); };
                        yield return Game.Wait(0.4f / k);
                    }
                    break;
                case "zigzag":
                    {
                        bool left = UnityEngine.Random.value < 0.5f;
                        float y0 = R(box.yMin + 15, box.yMax - 15), ph = R(0, 6);
                        var b = Spawn(left ? box.xMin - 6 : box.xMax + 6, y0, (left ? 95 : -95) * k, 0);
                        b.upd = bb => bb.p.y = y0 + Mathf.Sin(bb.t * 5 + ph) * 22;
                        yield return Game.Wait(0.33f / k);
                    }
                    break;
                case "bounce":
                    {
                        var b = Spawn(R(box.xMin + 10, box.xMax - 10), box.yMin + 4, R(-70, 70), 0, "b_coin");
                        b.acc = new Vector2(0, 320);
                        b.life = 4.5f;
                        b.upd = bb =>
                        {
                            if (bb.p.y > box.yMax - 5 && bb.v.y > 0) bb.v.y = -Mathf.Max(bb.v.y * 0.9f, 170);
                            if ((bb.p.x < box.xMin + 4 && bb.v.x < 0) || (bb.p.x > box.xMax - 4 && bb.v.x > 0)) bb.v.x = -bb.v.x;
                        };
                        yield return Game.Wait(0.6f / k);
                    }
                    break;
                case "hands": // тёмные сгустки тянутся к душе
                    {
                        var e = Edge();
                        var b = Spawn(e.x, e.y, 0, 0, "b_dark");
                        b.life = 4.5f;
                        b.r = 5;
                        b.upd = bb => bb.v = Vector2.Lerp(bb.v, (soul - bb.p).normalized * 75 * k, Time.deltaTime * 1.6f);
                        yield return Game.Wait(0.65f / k);
                    }
                    break;
                case "lasers":
                    {
                        bool hor = UnityEngine.Random.value < 0.5f;
                        var b = hor ? Spawn(box.center.x, soul.y, 0, 0, "") : Spawn(soul.x, box.center.y, 0, 0, "");
                        b.size = hor ? new Vector2(box.width, 14) : new Vector2(14, box.height);
                        b.delay = 0.7f;
                        b.life = 1.05f;
                        b.tint = new Color(0.25f, 0.9f, 1f);
                        yield return Game.Wait(0.95f / k);
                    }
                    break;
                case "static":
                    Spawn(R(box.xMin, box.xMax), box.yMin - 6, R(-30, 30), R(110, 160), "b_static");
                    if (UnityEngine.Random.value < 0.4f)
                    {
                        bool left = UnityEngine.Random.value < 0.5f;
                        Spawn(left ? box.xMin - 6 : box.xMax + 6, R(box.yMin, box.yMax), left ? 150 : -150, 0, "b_static");
                    }
                    yield return Game.Wait(0.16f);
                    break;
                case "none":
                    yield return Game.Wait(1f);
                    break;
                default: // gentle
                    Spawn(R(box.xMin, box.xMax), box.yMin - 6, 0, 55, "b_note");
                    yield return Game.Wait(0.7f);
                    break;
            }
        }
    }

    IEnumerator Die()
    {
        Gm.music.Stop(0.05f);
        bullets.Clear();
        st = St.Dodge;
        yield return Game.Wait(0.6f);
        soulBreak = 0;
        Gm.music.Sfx("shatter", 0.6f);
        for (float t = 0; t < 1.6f; t += Time.deltaTime) { soulBreak = t; yield return null; }
        st = St.None;
        Gm.dlg.forceRect = null;
    }

    // ===================== ЛОГИКА КАДРА =====================
    public void Tick()
    {
        box = new Rect(
            Mathf.Lerp(box.x, boxTarget.x, Time.deltaTime * 14),
            Mathf.Lerp(box.y, boxTarget.y, Time.deltaTime * 14),
            Mathf.Lerp(box.width, boxTarget.width, Time.deltaTime * 14),
            Mathf.Lerp(box.height, boxTarget.height, Time.deltaTime * 14));
        if (flavorShown < flavor.Length)
        {
            int b = (int)flavorShown;
            flavorShown += Time.deltaTime * 40;
            if ((int)flavorShown != b && b % 3 == 0 && st == St.Menu) Gm.music.Sfx("blip", 0.35f);
        }
        if (slashT >= 0) slashT += Time.deltaTime;
        if (dmgT >= 0) dmgT += Time.deltaTime;
        if (hpBarT >= 0) { hpBarT += Time.deltaTime; hpShown = Mathf.MoveTowards(hpShown, ehp, Time.deltaTime * E.hp); }
        if (enemyShake > 0) enemyShake -= Time.deltaTime;
    }

    // ===================== ОТРИСОВКА =====================
    public void Draw()
    {
        if (E == null) return;
        // Фон: едва заметная сетка
        var gc = new Color(0.25f, 0.15f, 0.35f, 0.35f * (1 - Pix.Gray));
        for (int x = 32; x <= 608; x += 48) G.Rect(x, 20, 1, 220, gc);
        for (int y = 20; y <= 240; y += 44) G.Rect(32, y, 576, 1, gc);

        DrawEnemy();
        DrawBox();
        DrawHud();

        if (bubble != null)
        {
            float bx = 400, by = 50;
            G.Rect(bx, by, 210, 110, Color.white);
            G.Rect(bx - 10, by + 30, 10, 10, Color.white);
            G.Text(Wrap(bubble.Substring(0, Mathf.Min((int)bubbleShown, bubble.Length)), 19), bx + 10, by + 8, 16, Color.black, TextAnchor.UpperLeft, 190);
        }
    }

    void DrawEnemy()
    {
        var tex = Pix.Get(E.sprite);
        if (tex == null) return;
        float s = tex.height > 44 ? 3.5f : 4f;
        float sx = 320 + (enemyShake > 0 ? Mathf.Sin(Time.time * 60) * 8 * enemyShake : 0);
        float bob = Mathf.Sin(Time.time * 2) * 4;
        if (dissolve >= 0)
        {
            G.TexFoot(tex, sx, 238, s, false, new Color(1, 1, 1, 1 - dissolve));
            var r = new System.Random((int)(Time.time * 20));
            float w = tex.width * s, h = tex.height * s;
            int n = (int)(200 * (1 - Mathf.Abs(dissolve - 0.5f) * 2));
            for (int i = 0; i < n; i++)
            {
                float g = (float)r.NextDouble();
                G.Rect(sx - w / 2 + (float)r.NextDouble() * w, 238 - h + (float)r.NextDouble() * h - dissolve * 40, 4, 4, new Color(g, g, g, 1 - dissolve));
            }
            return;
        }
        G.TexFoot(tex, sx, 238 + bob, s, false, new Color(1, 1, 1, enemyAlpha));

        if (slashT >= 0 && slashT < 0.4f)
        {
            float k = slashT / 0.4f;
            for (int i = 0; i < 5; i++)
                G.Rect(sx - 60 + k * 120 - i * 6, 90 + k * 110 + i * 6, 10, 10, new Color(1, 0.2f, 0.2f, 1 - k));
        }
        if (dmgT >= 0 && dmgT < 1.1f)
        {
            G.Text(dmgText, sx - 100, 60 - Mathf.Sin(Mathf.Min(1, dmgT * 3) * Mathf.PI) * 20, 30,
                dmgText == "ПРОМАХ" ? new Color(0.7f, 0.7f, 0.7f) : new Color(1, 0.25f, 0.25f), TextAnchor.UpperCenter, 200);
        }
        if (hpBarT >= 0 && hpBarT < 1.3f)
        {
            G.Rect(sx - 60, 100, 120, 12, new Color(0.4f, 0.4f, 0.4f));
            G.Rect(sx - 60, 100, 120 * hpShown / E.hp, 12, new Color(0.2f, 0.9f, 0.3f));
        }
    }

    void DrawBox()
    {
        G.Box(box.x, box.y, box.width, box.height);
        switch (st)
        {
            case St.Menu:
                G.Text(Wrap(flavor.Substring(0, Mathf.Min((int)flavorShown, flavor.Length)), 44), box.x + 24, box.y + 18, 20, Color.white, TextAnchor.UpperLeft, box.width - 30);
                break;
            case St.Sub:
                for (int i = 0; i < subItems.Count; i++)
                {
                    float x = box.x + 60 + (i % 2) * 260, y = box.y + 18 + (i / 2) * 34;
                    if (i == sub) G.Tex(Pix.Get("soul"), x - 26, y + 6, 2);
                    G.Text("* " + subItems[i], x, y, 20, subYellow[i] ? new Color(1, 1, 0.25f) : Color.white);
                }
                break;
            case St.Attack:
                // Шкала точности
                for (int i = 0; i < 12; i++)
                {
                    float x = box.x + 20 + i * (box.width - 40) / 11f;
                    float hh = 20 + (1 - Mathf.Abs(i - 5.5f) / 5.5f) * 70;
                    G.Rect(x - 2, box.center.y - hh / 2, 4, hh, Color.Lerp(new Color(0.2f, 0.5f, 0.2f), new Color(0.3f, 1f, 0.4f), 1 - Mathf.Abs(i - 5.5f) / 5.5f));
                }
                G.Rect(box.center.x - 3, box.y + 10, 6, box.height - 20, new Color(1, 1, 1, 0.25f));
                if (atkCursor >= 0)
                {
                    float cx = box.x + 10 + atkCursor * (box.width - 20);
                    G.Rect(cx - 5, box.y + 8, 10, box.height - 16, Color.white);
                    G.Rect(cx - 2, box.y + 11, 4, box.height - 22, Color.black);
                }
                break;
            case St.Dodge:
                GUI.BeginGroup(new Rect(box.x + 3, box.y + 3, box.width - 6, box.height - 6));
                float ox = -(box.x + 3), oy = -(box.y + 3);
                foreach (var b in bullets)
                {
                    if (b.size != Vector2.zero)
                    {
                        bool warn = b.t < b.delay;
                        var c = b.tint; c.a = warn ? 0.25f + 0.2f * Mathf.Sin(b.t * 40) : 1;
                        float sw = warn ? b.size.x * (b.size.x < 20 ? 0.4f : 1) : b.size.x;
                        float sh = warn ? b.size.y * (b.size.y < 20 ? 0.4f : 1) : b.size.y;
                        G.Rect(b.p.x - sw / 2 + ox, b.p.y - sh / 2 + oy, sw, sh, c);
                        continue;
                    }
                    var t = Pix.Get(b.spr);
                    if (b.t < b.delay)
                    {
                        G.Rect(b.p.x - 6 + ox, b.p.y - 10 + oy, 12, 12, new Color(1, 0.2f, 0.2f, 0.4f + 0.3f * Mathf.Sin(b.t * 30)));
                        continue;
                    }
                    G.TexC(t, b.p.x + ox, b.p.y + oy, 2, b.tint);
                }
                if (soulBreak < 0)
                {
                    bool blink = inv > 0 && (int)(inv * 12) % 2 == 0;
                    if (!blink) G.TexC(Pix.Color("soul"), soul.x + ox, soul.y + oy, 2);
                }
                GUI.EndGroup();
                if (soulBreak >= 0)
                {
                    var st2 = Pix.Color("soul");
                    if (soulBreak < 0.5f) G.TexC(st2, soul.x - 3, soul.y, 2);
                    if (soulBreak < 0.5f) G.TexC(st2, soul.x + 3, soul.y, 2, new Color(1, 1, 1, 0.8f));
                    else
                        for (int i = 0; i < 6; i++)
                        {
                            float a = i * 1.05f + 0.3f, tt = soulBreak - 0.5f;
                            G.Rect(soul.x + Mathf.Cos(a) * tt * 90, soul.y - Mathf.Sin(a) * tt * 60 + tt * tt * 200, 4, 4, new Color(1, 0.15f, 0.25f));
                        }
                }
                break;
        }
    }

    void DrawHud()
    {
        G.Text("НОА", 34, 398, 18, Color.white);
        G.Text($"ПУСТ {S.Void}", 110, 398, 18, S.Void > 0 ? new Color(0.75f, 0.75f, 0.75f) : Color.white);
        G.Text("ПАМ", 230, 400, 14, Color.white);
        G.Rect(270, 400, S.maxHp * 2.5f, 18, new Color(0.8f, 0.1f, 0.1f));
        G.Rect(270, 400, S.hp * 2.5f, 18, new Color(1f, 0.9f, 0.2f));
        G.Text($"{S.hp} / {S.maxHp}", 285 + S.maxHp * 2.5f, 398, 18, Color.white);

        var orange = new Color(1f, 0.5f, 0.15f);
        var yellow = new Color(1f, 1f, 0.25f);
        for (int i = 0; i < 4; i++)
        {
            float x = 32 + i * 148;
            bool sel = i == btn && (st == St.Menu || st == St.Sub);
            bool off = eraseOnly && i != 0;
            var c = off ? new Color(0.3f, 0.3f, 0.3f) : sel ? yellow : orange;
            G.Rect(x, 428, 132, 42, c);
            G.Rect(x + 3, 431, 126, 36, Color.black);
            if (sel && st == St.Menu) G.Tex(Pix.Color("soul"), x + 10, 442, 2);
            G.Text(off ? "- - -" : Buttons[i], x + 26, 438, 17, c, TextAnchor.UpperCenter, 104);
        }
    }

    static string Wrap(string s, int max)
    {
        var sb = new System.Text.StringBuilder();
        int col = 0;
        foreach (var w in s.Split(' '))
        {
            if (col > 0 && col + w.Length + 1 > max) { sb.Append('\n'); col = 0; }
            else if (col > 0) { sb.Append(' '); col++; }
            sb.Append(w);
            col += w.Length;
            int nl = w.LastIndexOf('\n');
            if (nl >= 0) col = w.Length - nl - 1;
        }
        return sb.ToString();
    }
}
