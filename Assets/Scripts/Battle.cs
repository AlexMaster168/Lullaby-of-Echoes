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
    // Фазы босса: наборы атак. Префиксы: "dark:" — тьма вокруг сердца, "shrink:" — рамка сжимается
    public string[][] phases;
    public string[] phaseText;
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

class Particle
{
    public Vector2 p, v;
    public float t, life, size;
    public Color c;
    public float grav;
}

public class Battle
{
    public EnemyDef E;
    public BResult result;
    int ehp, emax, eatk, calm, turn, btn, lastPhase;
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
    readonly List<Particle> parts = new List<Particle>();

    float atkCursor = -1, slashT = -1, dmgT = -1, hpShown, hpBarT = -1;
    string dmgText;
    float enemyAlpha = 1, dissolve = -1, enemyShake, flashT, hurtFlash;
    float soulBreak = -1;
    float dark, darkTarget;            // тьма вокруг сердца
    float introT = -1;                 // вход в бой: сердце вылетает из героя
    Vector2 introFrom;
    string zone = "dock";

    // Автотест
    public static bool Autopilot, EraseAll;
    public bool InMenu => st == St.Menu;
    public bool InDodge => st == St.Dodge;
    public bool InDark => st == St.Dodge && dark > 0.9f && bullets.Count > 2;
    public bool Shrinking => st == St.Dodge && box.width < E.boxW * 0.75f;
    public int BulletCount => bullets.Count;

    static readonly string[] Buttons = { "СТЕРЕТЬ", "СЛУШАТЬ", "ПЛЕЕР", "ОТПУСТИТЬ" };
    static readonly Vector2 MenuSoul = new Vector2(50, 449);
    Game Gm => Game.I;
    SaveData S => Game.I.S;

    // ===================== ПОТОК БОЯ =====================
    public IEnumerator Fight(EnemyDef e, bool onlyErase = false)
    {
        E = e;
        eraseOnly = onlyErase;
        emax = Mathf.Max(1, Mathf.RoundToInt(e.hp * Diff.EnemyHp));
        eatk = e.atk <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(e.atk * Diff.EnemyAtk));
        ehp = emax; hpShown = emax;
        calm = 0; turn = 0; lastPhase = 0; btn = eraseOnly ? 0 : 1;
        flags.Clear(); bullets.Clear(); parts.Clear();
        enemyAlpha = 1; dissolve = -1; soulBreak = -1; slashT = -1; dmgT = -1; hpBarT = -1; atkCursor = -1;
        dark = darkTarget = 0; flashT = 0; hurtFlash = 0;
        box = boxTarget = Full;
        st = St.None;
        bubble = null;
        zone = Gm.world.area != null ? Gm.world.area.zone : "dock";

        // Вход в бой как в Undertale: экран гаснет, сердце мигает на герое и летит к меню
        Gm.music.Stop(0.05f);
        introFrom = Gm.mode == Mode.World ? Gm.world.ToScreen(Gm.world.pos.x, Gm.world.pos.y - 14) : new Vector2(320, 240);
        introT = 0;
        Gm.mode = Mode.Battle;
        Gm.fade = 0;
        for (int i = 0; i < 3; i++) { Gm.music.Sfx("select", 0.8f, 0.7f); yield return Game.Wait(0.16f); }
        Gm.music.Sfx("encounter");
        for (float t = 0; t < 0.45f; t += Time.deltaTime) { introT = 0.5f + t / 0.45f; yield return null; }
        introT = -1;
        Gm.fade = 1;
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
                    yield return Gm.dlg.Say("", $"{E.name.ToUpper()} — АТК {eatk}.\n{E.check}");
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
                if (Diff.Novice)
                {
                    NoviceHeal();
                    yield return Gm.dlg.Say("", "Ты перематываешь плёнку к самому началу.", "ПАМЯТЬ полностью восстановлена.");
                }
                else if (S.hums > 0)
                {
                    S.hums--;
                    int heal = Mathf.Min(12, S.maxHp - S.hp);
                    S.hp += heal;
                    Gm.music.Sfx("heal");
                    Burst(MenuSoul + new Vector2(270, -40), 16, new Color(1f, 0.95f, 0.5f), 80, -60);
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

            // ---------- СМЕНА ФАЗЫ БОССА ----------
            int ph = Phase;
            if (ph != lastPhase)
            {
                lastPhase = ph;
                Gm.shake = 0.5f;
                Gm.music.Sfx("rumble", 0.7f);
                if (E.id == "oculus" && ph >= 1) Gm.music.Play("oculus_boss", 0.8f);
                if (E.phaseText != null && ph < E.phaseText.Length && E.phaseText[ph] != null)
                    yield return Gm.dlg.Say("", E.phaseText[ph]);
            }

            // ---------- ХОД ПРОТИВНИКА ----------
            string line = reply ?? (calm >= 100 && E.calmTalk != null ? Pick(E.calmTalk, turn) : Pick(E.talk, turn));
            if (line != null) yield return Bubble(line);
            yield return Dodge();
            if (S.hp <= 0) { yield return Die(); result = BResult.Lost; break; }
            turn++;
            SetFlavor(Spareable ? $"{E.name} ждёт, когда ты его отпустишь." : Pick(E.flavor, turn));
        }

        // Пережитый босс делает Ноа сильнее
        if (result != BResult.Lost && E.boss)
        {
            S.maxHp += 4;
            S.hp = Mathf.Min(S.maxHp, S.hp + 4);
            yield return Gm.dlg.Say("", "Ты вспоминаешь о себе чуть больше.\nМАКС. ПАМЯТЬ +4.");
        }
        Debug.Log($"[BATTLE] {E.id} {result} turns={turn} hp={emax} atk={eatk} diff={S.diff}");

        st = St.None;
        bullets.Clear();
        parts.Clear();
        dark = darkTarget = 0;
        Gm.dlg.forceRect = null;
        Gm.UpdateGray();
    }

    bool Spareable => calm >= 100 && turn >= E.minTurns;

    int Phase
    {
        get
        {
            if (E.phases == null) return 0;
            int byTurn = turn / 2;
            int byHp = Mathf.FloorToInt((1f - (float)ehp / emax) * 3f);
            return Mathf.Clamp(Mathf.Max(byTurn, byHp), 0, E.phases.Length - 1);
        }
    }

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
            case 2:
                if (Diff.Novice) { subItems.Add("Перемотать память (∞)"); subYellow.Add(true); }
                else { subItems.Add($"Напеть колыбельную ({S.hums})"); subYellow.Add(false); }
                break;
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
            // Точный удар — до ×1.3 от силы; урон растёт от стирания плавно
            int dmg = Mathf.Max(1, Mathf.RoundToInt(S.Atk * (0.4f + 0.9f * acc) * UnityEngine.Random.Range(0.9f, 1.1f)));
            ehp = Mathf.Max(0, ehp - dmg);
            dmgText = dmg.ToString(); dmgT = 0; hpBarT = 0;
            enemyShake = 0.5f;
            flashT = 0.22f;
            Burst(new Vector2(320, 150), 22, new Color(1f, 0.9f, 0.6f), 220, 200);
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
        for (float t = 0; t < 1.4f; t += Time.deltaTime)
        {
            dissolve = t / 1.4f;
            if (UnityEngine.Random.value < 0.5f) Burst(new Vector2(320 + UnityEngine.Random.Range(-60, 60), 80 + UnityEngine.Random.Range(0, 150)), 1, new Color(0.6f, 0.6f, 0.6f), 30, -30);
            yield return null;
        }
        dissolve = 1;
        S.erased.Add(E.id);
        yield return Gm.dlg.Say("", E.erasedText ?? new[] { $"{E.name} рассыпается в белый шум." });
        yield return Gm.dlg.Say("", $"ПУСТОТА выросла до {S.Void}.");
    }

    IEnumerator SpareEnemy()
    {
        Gm.music.Sfx("spare");
        for (float t = 0; t < 0.8f; t += Time.deltaTime)
        {
            enemyAlpha = Mathf.Lerp(1, 0.4f, t / 0.8f);
            if (UnityEngine.Random.value < 0.6f) Burst(new Vector2(320 + UnityEngine.Random.Range(-70, 70), 220), 1, new Color(1f, 0.95f, 0.4f), 20, -90);
            yield return null;
        }
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
        string pat = E.phases != null ? E.phases[Phase][turn % E.phases[Phase].Length] : Pick(E.patterns, turn);
        bool shrink = false;
        darkTarget = 0;
        if (pat.StartsWith("dark:")) { darkTarget = 1; pat = pat.Substring(5); }
        if (pat.StartsWith("shrink:")) { shrink = true; darkTarget = 0.55f; pat = pat.Substring(7); }
        if (calm >= 100) { pat = "gentle"; shrink = false; darkTarget = 0; }

        var start = new Rect(320 - E.boxW / 2, 390 - E.boxH, E.boxW, E.boxH);
        boxTarget = start;
        yield return Game.Wait(0.3f);
        box = boxTarget;
        soul = new Vector2(box.center.x, box.yMax - 24);
        inv = 0;
        bullets.Clear();
        var running = new List<Coroutine>();
        foreach (var p in pat.Split('+')) running.Add(Gm.StartCoroutine(Pattern(p)));
        float dur = E.id == "oculus" ? 8f : E.boss ? 7f : 5.5f;
        for (float t = 0; t < dur && S.hp > 0; t += Time.deltaTime)
        {
            if (shrink)
            {
                // Рамка медленно сжимается к центру снизу
                float k = Mathf.Lerp(1f, 0.55f, t / dur);
                boxTarget = new Rect(320 - start.width * k / 2, 390 - start.height * k, start.width * k, start.height * k);
                box = boxTarget;
            }
            MoveSoul();
            StepBullets();
            yield return null;
        }
        foreach (var c in running) Gm.StopCoroutine(c);
        bullets.Clear();
        darkTarget = 0;
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
            if (b.t > b.life || !new Rect(box.x - 160, box.y - 160, box.width + 320, box.height + 320).Contains(b.p)) b.dead = true;
            if (b.dead) { bullets.Remove(b); continue; }
            if (b.t < b.delay || inv > 0 || eatk <= 0) continue;
            bool hit = b.size != Vector2.zero
                ? new Rect(b.p.x - b.size.x / 2, b.p.y - b.size.y / 2, b.size.x, b.size.y).Contains(soul)
                : (b.p - soul).sqrMagnitude < (b.r + 5) * (b.r + 5);
            if (hit && !Autopilot)
            {
                S.hp = Mathf.Max(0, S.hp - eatk);
                inv = Diff.Invuln;
                hurtFlash = 0.3f;
                Burst(soul, 8, new Color(1f, 0.2f, 0.3f), 120, 0);
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
        float boss = E.boss ? 1.15f : 1f;
        float sp = boss * Diff.Speed;      // скорость снарядов
        float dn = boss * Diff.Density;    // частота появления
        yield return Game.Wait(0.3f);
        while (true)
        {
            switch (name)
            {
                case "rain":
                    Spawn(R(box.xMin, box.xMax), box.yMin - 6, R(-10, 10), R(90, 140) * sp);
                    yield return Game.Wait(0.2f / dn);
                    break;
                case "drip": // мягкие капли часов
                    Spawn(R(box.xMin + 6, box.xMax - 6), box.yMin - 6, 0, R(60, 85) * sp, "b_drop");
                    yield return Game.Wait(0.45f / dn);
                    break;
                case "tears": // слёзы Окулюса разгоняются
                    {
                        var b = Spawn(R(box.xMin, box.xMax), box.yMin - 6, R(-8, 8), 40 * sp, "b_tear");
                        b.acc = new Vector2(0, 260 * sp);
                        b.r = 3;
                        yield return Game.Wait(0.16f / dn);
                    }
                    break;
                case "sway": // падающие листы/лепестки качаются
                    {
                        var b = Spawn(R(box.xMin, box.xMax), box.yMin - 6, 0, R(60, 90) * sp);
                        float x0 = b.p.x, ph = R(0, 6);
                        b.upd = bb => bb.p.x = x0 + Mathf.Sin(bb.t * 3 + ph) * 22;
                        yield return Game.Wait(0.22f / dn);
                    }
                    break;
                case "side":
                    {
                        bool left = UnityEngine.Random.value < 0.5f;
                        Spawn(left ? box.xMin - 6 : box.xMax + 6, R(box.yMin + 6, box.yMax - 6), (left ? 120 : -120) * sp, 0);
                        yield return Game.Wait(0.28f / dn);
                    }
                    break;
                case "aimed":
                    {
                        var e = Edge();
                        var d = (soul - e).normalized * 125 * sp;
                        Spawn(e.x, e.y, d.x, d.y);
                        yield return Game.Wait(0.45f / dn);
                    }
                    break;
                case "ring":
                    {
                        float cx = R(box.xMin + 20, box.xMax - 20), cy = box.yMin + 6;
                        float off = R(0, 1);
                        for (int i = 0; i < 12; i++)
                        {
                            float a = (i + off) / 12f * Mathf.PI * 2;
                            Spawn(cx, cy, Mathf.Cos(a) * 80 * sp, Mathf.Sin(a) * 80 * sp);
                        }
                        yield return Game.Wait(1.1f / dn);
                    }
                    break;
                case "ringin": // кольцо смыкается вокруг сердца, в нём есть просвет
                    {
                        var c = soul;
                        int n = 18, gap = UnityEngine.Random.Range(0, n);
                        for (int i = 0; i < n; i++)
                        {
                            if (i == gap || i == (gap + 1) % n || i == (gap + 2) % n) continue;
                            float a = i / (float)n * Mathf.PI * 2;
                            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                            var b = Spawn(c.x + dir.x * 130, c.y + dir.y * 130, -dir.x * 62 * sp, -dir.y * 62 * sp, "b_dark");
                            b.life = 2.4f / sp;
                        }
                        yield return Game.Wait(1.7f / dn);
                    }
                    break;
                case "eyes": // в темноте открываются глаза и стреляют в сердце
                    {
                        var at = new Vector2(R(box.xMin + 14, box.xMax - 14), R(box.yMin + 10, box.yMin + box.height * 0.4f));
                        var eye = Spawn(at.x, at.y, 0, 0, "b_eye");
                        eye.delay = 0.8f / sp;
                        eye.life = eye.delay + 0.6f;
                        eye.r = 0;
                        bool fired = false;
                        eye.upd = bb =>
                        {
                            if (fired || bb.t < bb.delay) return;
                            fired = true;
                            var d = (soul - bb.p).normalized;
                            for (int k = -1; k <= 1; k++)
                            {
                                var dd = Quaternion.Euler(0, 0, k * 14) * d;
                                Spawn(bb.p.x, bb.p.y, dd.x * 150 * sp, dd.y * 150 * sp, "b_dark");
                            }
                        };
                        yield return Game.Wait(0.75f / dn);
                    }
                    break;
                case "wall": // стена с просветом
                    {
                        float gap = R(box.xMin + 24, box.xMax - 24);
                        for (float x = box.xMin + 4; x < box.xMax; x += 15)
                            if (Mathf.Abs(x - gap) > 22) Spawn(x, box.yMin - 6, 0, 75 * sp);
                        yield return Game.Wait(1.3f / dn);
                    }
                    break;
                case "sweep": // стрелка часов: короче и медленнее, чем раньше
                    {
                        var c = box.center;
                        float dir = UnityEngine.Random.value < 0.5f ? 1 : -1;
                        for (int i = 1; i <= 4; i++)
                        {
                            float rr = i * 14;
                            var b = Spawn(c.x, c.y, 0, 0);
                            b.life = 20;
                            b.upd = bb => { float a = -Mathf.PI / 2 + bb.t * 0.9f * sp * dir; bb.p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr; };
                        }
                        while (true)
                        {
                            yield return Game.Wait(1.5f / dn);
                            Spawn(R(box.xMin, box.xMax), box.yMin - 6, 0, 60 * sp, "b_drop");
                        }
                    }
                case "rise": // шипы снизу с предупреждением
                    {
                        float x = UnityEngine.Random.value < 0.5f ? soul.x : R(box.xMin + 8, box.xMax - 8);
                        var b = Spawn(x, box.yMax - 5, 0, 0, "b_thorn");
                        b.delay = 0.55f;
                        b.upd = bb => { if (bb.t >= bb.delay) bb.v = new Vector2(0, -280 * sp); };
                        yield return Game.Wait(0.4f / dn);
                    }
                    break;
                case "zigzag":
                    {
                        bool left = UnityEngine.Random.value < 0.5f;
                        float y0 = R(box.yMin + 15, box.yMax - 15), ph = R(0, 6);
                        var b = Spawn(left ? box.xMin - 6 : box.xMax + 6, y0, (left ? 95 : -95) * sp, 0);
                        b.upd = bb => bb.p.y = y0 + Mathf.Sin(bb.t * 5 + ph) * 22;
                        yield return Game.Wait(0.33f / dn);
                    }
                    break;
                case "bounce":
                    {
                        var b = Spawn(R(box.xMin + 10, box.xMax - 10), box.yMin + 4, R(-70, 70) * sp, 0, "b_coin");
                        b.acc = new Vector2(0, 320 * sp);
                        b.life = 4.5f;
                        b.upd = bb =>
                        {
                            if (bb.p.y > box.yMax - 5 && bb.v.y > 0) bb.v.y = -Mathf.Max(bb.v.y * 0.9f, 170);
                            if ((bb.p.x < box.xMin + 4 && bb.v.x < 0) || (bb.p.x > box.xMax - 4 && bb.v.x > 0)) bb.v.x = -bb.v.x;
                        };
                        yield return Game.Wait(0.6f / dn);
                    }
                    break;
                case "hands": // тёмные сгустки тянутся к душе
                    {
                        var e = Edge();
                        var b = Spawn(e.x, e.y, 0, 0, "b_dark");
                        b.life = 4.5f;
                        b.r = 5;
                        b.upd = bb => bb.v = Vector2.Lerp(bb.v, (soul - bb.p).normalized * 80 * sp, Time.deltaTime * 1.6f);
                        yield return Game.Wait(0.6f / dn);
                    }
                    break;
                case "lasers":
                    {
                        bool hor = UnityEngine.Random.value < 0.5f;
                        var b = hor ? Spawn(box.center.x, soul.y, 0, 0, "") : Spawn(soul.x, box.center.y, 0, 0, "");
                        b.size = hor ? new Vector2(box.width, 14) : new Vector2(14, box.height);
                        b.delay = 0.7f / sp;
                        b.life = b.delay + 0.35f;
                        b.tint = new Color(0.25f, 0.9f, 1f);
                        yield return Game.Wait(0.95f / dn);
                    }
                    break;
                case "static":
                    Spawn(R(box.xMin, box.xMax), box.yMin - 6, R(-30, 30), R(110, 160) * sp, "b_static");
                    if (UnityEngine.Random.value < 0.4f)
                    {
                        bool left = UnityEngine.Random.value < 0.5f;
                        Spawn(left ? box.xMin - 6 : box.xMax + 6, R(box.yMin, box.yMax), (left ? 150 : -150) * sp, 0, "b_static");
                    }
                    yield return Game.Wait(0.16f / dn);
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
        darkTarget = 0;
        st = St.Dodge;
        yield return Game.Wait(0.6f);
        soulBreak = 0;
        Gm.music.Sfx("shatter", 0.6f);
        for (float t = 0; t < 1.6f; t += Time.deltaTime) { soulBreak = t; yield return null; }
        st = St.None;
        Gm.dlg.forceRect = null;
    }

    // Новичок: мгновенно восстановить ПАМЯТЬ до максимума
    void NoviceHeal()
    {
        S.hp = S.maxHp;
        Gm.music.Sfx("heal");
        Burst(st == St.Dodge ? soul : MenuSoul + new Vector2(270, -40), 16, new Color(0.5f, 1f, 0.6f), 90, -60);
    }

    // ===================== ЧАСТИЦЫ =====================
    void Burst(Vector2 at, int n, Color c, float speed, float up)
    {
        for (int i = 0; i < n; i++)
        {
            float a = UnityEngine.Random.value * Mathf.PI * 2;
            float s = UnityEngine.Random.Range(0.3f, 1f) * speed;
            parts.Add(new Particle
            {
                p = at,
                v = new Vector2(Mathf.Cos(a) * s, Mathf.Sin(a) * s + up),
                life = UnityEngine.Random.Range(0.4f, 0.9f),
                size = UnityEngine.Random.Range(2f, 5f),
                c = c,
                grav = up > 0 ? 300 : 0
            });
        }
    }

    // ===================== ЛОГИКА КАДРА =====================
    public void Tick()
    {
        float dt = Time.deltaTime;
        if (Diff.Novice && Input.GetKeyDown(KeyCode.H) && S.hp > 0 && S.hp < S.maxHp && soulBreak < 0) NoviceHeal();
        box = new Rect(
            Mathf.Lerp(box.x, boxTarget.x, dt * 14),
            Mathf.Lerp(box.y, boxTarget.y, dt * 14),
            Mathf.Lerp(box.width, boxTarget.width, dt * 14),
            Mathf.Lerp(box.height, boxTarget.height, dt * 14));
        if (flavorShown < flavor.Length)
        {
            int b = (int)flavorShown;
            flavorShown += dt * 40;
            if ((int)flavorShown != b && b % 3 == 0 && st == St.Menu) Gm.music.Sfx("blip", 0.35f);
        }
        if (slashT >= 0) slashT += dt;
        if (dmgT >= 0) dmgT += dt;
        if (hpBarT >= 0) { hpBarT += dt; hpShown = Mathf.MoveTowards(hpShown, ehp, dt * emax); }
        if (enemyShake > 0) enemyShake -= dt;
        if (flashT > 0) flashT -= dt;
        if (hurtFlash > 0) hurtFlash -= dt;
        dark = Mathf.MoveTowards(dark, darkTarget, dt * 1.5f);
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            var p = parts[i];
            p.t += dt;
            p.v.y += p.grav * dt;
            p.p += p.v * dt;
            if (p.t > p.life) parts.RemoveAt(i);
        }
    }

    // ===================== ОТРИСОВКА =====================
    public void Draw()
    {
        if (E == null) return;
        if (introT >= 0) { DrawIntro(); return; }

        DrawBackground();
        DrawEnemy();
        DrawBox();
        DrawHud();

        foreach (var p in parts)
        {
            var c = p.c; c.a = 1 - p.t / p.life;
            G.Rect(p.p.x - p.size / 2, p.p.y - p.size / 2, p.size, p.size, c);
        }

        if (bubble != null)
        {
            float bx = 400, by = 50;
            G.Rect(bx, by, 210, 110, Color.white);
            G.Rect(bx - 10, by + 30, 10, 10, Color.white);
            G.Text(Wrap(bubble.Substring(0, Mathf.Min((int)bubbleShown, bubble.Length)), 19), bx + 10, by + 8, 16, Color.black, TextAnchor.UpperLeft, 190);
        }
    }

    void DrawIntro()
    {
        G.Rect(0, 0, 640, 480, Color.black);
        var soulTex = Pix.Color("soul");
        if (introT < 0.5f)
        {
            // мигает на герое
            if (((int)(Time.time * 12)) % 2 == 0) G.TexC(soulTex, introFrom.x, introFrom.y, 2);
            return;
        }
        float k = Mathf.SmoothStep(0, 1, introT - 0.5f);
        var p = Vector2.Lerp(introFrom, MenuSoul, k);
        G.Glow(p.x, p.y, 20, new Color(1, 0.2f, 0.3f, 0.4f));
        G.TexC(soulTex, p.x, p.y, 2);
    }

    // Цвет с учётом «выцветания» мира
    static Color Z(float r, float g, float b, float a = 1)
    {
        float l = 0.3f * r + 0.59f * g + 0.11f * b;
        float k = Pix.Gray;
        return new Color(Mathf.Lerp(r, l, k), Mathf.Lerp(g, l, k), Mathf.Lerp(b, l, k), a);
    }

    // Анимированный фон по зоне
    void DrawBackground()
    {
        float t = Time.time;
        var r = new System.Random(7);
        if (E.id == "oculus" || E.id == "echo" || E.id == "skrip")
        {
            int ph = E.id == "oculus" ? Phase : 0;
            for (int y = 0; y < 245; y += 7) G.Rect(0, y, 640, 7, Color.Lerp(Z(0.06f, 0.03f, 0.1f), Color.black, y / 245f + ph * 0.2f));
            // извивающиеся тени
            for (int i = 0; i < 9; i++)
            {
                float bx = 40 + i * 70 + Mathf.Sin(t * 0.7f + i) * 12;
                for (int s = 0; s < 12; s++)
                {
                    float y = 240 - s * 18;
                    float x = bx + Mathf.Sin(t * 1.3f + s * 0.5f + i) * (6 + s * 1.5f);
                    G.Rect(x, y, 6 - s * 0.3f, 18, Z(0.18f, 0.12f, 0.26f, 0.5f));
                }
            }
            // глаза во тьме
            if (E.id == "oculus")
                for (int i = 0; i < 3 + ph * 3; i++)
                {
                    float open = Mathf.Clamp01(Mathf.Sin(t * 0.8f + i * 2.1f) * 2);
                    if (open <= 0) continue;
                    float ex = 30 + (float)r.NextDouble() * 580, ey = 20 + (float)r.NextDouble() * 200;
                    G.Rect(ex - 8, ey - 2 * open, 16, 4 * open, Z(0.8f, 0.75f, 0.9f, 0.6f));
                    G.Rect(ex - 2, ey - 2 * open, 4, 4 * open, Z(0.9f, 0.1f, 0.2f, 0.8f));
                }
            return;
        }
        switch (zone)
        {
            case "dock":
                for (int y = 0; y < 245; y += 7) G.Rect(0, y, 640, 7, Color.Lerp(Z(0.05f, 0.07f, 0.14f), Z(0.1f, 0.14f, 0.22f), y / 245f));
                for (int i = 0; i < 6; i++)
                {
                    float wy = 190 + i * 9;
                    for (int x = -40; x < 680; x += 40)
                    {
                        float xx = x + ((t * (10 + i * 6)) % 40);
                        G.Rect(xx, wy + Mathf.Sin(xx * 0.05f + t * 2 + i) * 2, 22, 2, Z(0.3f, 0.45f, 0.65f, 0.25f + i * 0.05f));
                    }
                }
                G.Glow(560, 60, 50, Z(1f, 0.9f, 0.6f, 0.25f)); // далёкий маяк
                for (int i = 0; i < 5; i++)
                    G.Rect((i * 173 + t * 14) % 760 - 120, 60 + i * 25, 180, 22, Z(0.8f, 0.85f, 1f, 0.05f));
                break;
            case "garden":
                for (int y = 0; y < 245; y += 7) G.Rect(0, y, 640, 7, Color.Lerp(Z(0.04f, 0.1f, 0.06f), Z(0.1f, 0.18f, 0.1f), y / 245f));
                for (int i = 0; i < 10; i++)
                {
                    float x = 20 + i * 64;
                    G.Rect(x, 150 + (i % 3) * 20, 3, 100, Z(0.15f, 0.3f, 0.15f, 0.6f));
                }
                for (int i = 0; i < 26; i++)
                {
                    float x = ((float)r.NextDouble() * 640 + Mathf.Sin(t + i) * 20);
                    float y = ((float)r.NextDouble() * 245 + t * (12 + i % 4 * 6)) % 245;
                    G.Rect(x, y, 4, 3, i % 3 == 0 ? Z(0.9f, 0.6f, 0.7f, 0.6f) : Z(1f, 0.95f, 0.6f, 0.5f));
                }
                break;
            case "neon":
                for (int y = 0; y < 245; y += 7) G.Rect(0, y, 640, 7, Color.Lerp(Z(0.08f, 0.02f, 0.15f), Z(0.25f, 0.05f, 0.3f), y / 245f));
                for (int i = 0; i < 16; i++)
                {
                    float w = 30 + (float)r.NextDouble() * 40, h = 60 + (float)r.NextDouble() * 120;
                    float x = ((i * 60 - t * 8) % 720 + 720) % 720 - 60;
                    G.Rect(x, 245 - h, w, h, Z(0.06f, 0.03f, 0.1f));
                    for (int wy = 0; wy < h - 10; wy += 12)
                        for (int wx = 4; wx < w - 6; wx += 10)
                        {
                            bool on = ((i * 7 + wx + wy + (int)(t * 2)) % 9) < 5;
                            if (on) G.Rect(x + wx, 245 - h + 6 + wy, 4, 5, (wx + wy) % 3 == 0 ? Z(1f, 0.3f, 0.85f, 0.7f) : Z(0.25f, 0.9f, 1f, 0.6f));
                        }
                }
                for (int y = 0; y < 245; y += 4) G.Rect(0, y, 640, 1, new Color(0, 0, 0, 0.15f));
                break;
            default: // архив
                for (int y = 0; y < 245; y += 7) G.Rect(0, y, 640, 7, Color.Lerp(Z(0.12f, 0.12f, 0.12f), Z(0.25f, 0.25f, 0.25f), y / 245f));
                for (int i = 0; i < 8; i++) G.Rect(i * 84, 20, 50, 225, Z(0.16f, 0.16f, 0.16f));
                for (int i = 0; i < 18; i++)
                {
                    float x = (float)r.NextDouble() * 640 + Mathf.Sin(t * 0.8f + i) * 15;
                    float y = 245 - ((float)r.NextDouble() * 245 + t * (10 + i % 3 * 5)) % 245;
                    G.Rect(x, y, 7, 9, Z(0.95f, 0.95f, 0.92f, 0.35f));
                }
                break;
        }
    }

    void DrawEnemy()
    {
        bool anim = E.sprite.StartsWith("e_");
        int frame = (int)(Time.time * (E.id == "oculus" ? 6 : 4)) % 4;
        string key = anim ? E.sprite + "@" + frame : E.sprite;
        var tex = Pix.Get(key);
        if (tex == null) return;
        float s = tex.height > 44 ? 3.5f : 4f;
        if (E.id == "oculus") s = 3.4f;
        float sx = 320 + (enemyShake > 0 ? Mathf.Sin(Time.time * 60) * 8 * enemyShake : 0);
        // «дыхание» и сплющивание при ударе
        float br = Mathf.Sin(Time.time * 2.2f);
        float kx = s * (1 - 0.02f * br), ky = s * (1 + 0.03f * br);
        if (enemyShake > 0) { kx *= 1.06f; ky *= 0.92f; }
        float foot = 240;

        // тень под врагом
        G.Rect(sx - tex.width * s * 0.35f, foot - 4, tex.width * s * 0.7f, 6, new Color(0, 0, 0, 0.35f));

        if (dissolve >= 0)
        {
            G.TexFootXY(tex, sx, foot, kx, ky, false, new Color(1, 1, 1, 1 - dissolve));
            var r = new System.Random((int)(Time.time * 20));
            float w = tex.width * s, h = tex.height * s;
            int n = (int)(200 * (1 - Mathf.Abs(dissolve - 0.5f) * 2));
            for (int i = 0; i < n; i++)
            {
                float g = (float)r.NextDouble();
                G.Rect(sx - w / 2 + (float)r.NextDouble() * w, foot - h + (float)r.NextDouble() * h - dissolve * 40, 4, 4, new Color(g, g, g, 1 - dissolve));
            }
            return;
        }
        if (E.id == "oculus") G.Glow(sx, foot - tex.height * s * 0.6f, 150, new Color(0.35f, 0.2f, 0.6f, 0.25f + 0.1f * Mathf.Sin(Time.time * 3)));
        G.TexFootXY(tex, sx, foot, kx, ky, false, new Color(1, 1, 1, enemyAlpha));
        if (flashT > 0) G.TexFootXY(Pix.Silhouette(key), sx, foot, kx, ky, false, new Color(1, 1, 1, flashT / 0.22f));

        if (slashT >= 0 && slashT < 0.45f)
        {
            // разрез по диагонали: красная полоса с белой сердцевиной
            float k = Mathf.Clamp01(slashT / 0.25f), fadeK = 1 - Mathf.Clamp01((slashT - 0.25f) / 0.2f);
            Vector2 a = new Vector2(sx + 70, 70), b = new Vector2(sx - 70, 210);
            var end = Vector2.Lerp(a, b, k);
            G.Line(a.x, a.y, end.x, end.y, 10, new Color(1, 0.15f, 0.2f, fadeK));
            G.Line(a.x, a.y, end.x, end.y, 3, new Color(1, 1, 1, fadeK));
        }
        if (dmgT >= 0 && dmgT < 1.1f)
        {
            float bounce = Mathf.Abs(Mathf.Sin(dmgT * 9)) * 22 * Mathf.Exp(-dmgT * 3.5f);
            G.Text(dmgText, sx - 100, 55 - bounce, 30,
                dmgText == "ПРОМАХ" ? new Color(0.7f, 0.7f, 0.7f) : new Color(1, 0.25f, 0.25f), TextAnchor.UpperCenter, 200);
        }
        if (hpBarT >= 0 && hpBarT < 1.3f)
        {
            G.Rect(sx - 61, 99, 122, 14, Color.black);
            G.Rect(sx - 60, 100, 120, 12, new Color(0.45f, 0.1f, 0.1f));
            G.Rect(sx - 60, 100, 120 * hpShown / emax, 12, new Color(0.2f, 0.9f, 0.3f));
        }
    }

    void DrawBox()
    {
        var border = Color.Lerp(Color.white, new Color(1, 0.2f, 0.25f), Mathf.Clamp01(hurtFlash / 0.3f));
        G.Rect(box.x, box.y, box.width, box.height, border);
        G.Rect(box.x + 3, box.y + 3, box.width - 6, box.height - 6, Color.black);
        switch (st)
        {
            case St.Menu:
                G.Text(Wrap(flavor.Substring(0, Mathf.Min((int)flavorShown, flavor.Length)), 44), box.x + 24, box.y + 18, 20, Color.white, TextAnchor.UpperLeft, box.width - 30);
                break;
            case St.Sub:
                for (int i = 0; i < subItems.Count; i++)
                {
                    float x = box.x + 60 + (i % 2) * 260, y = box.y + 18 + (i / 2) * 34;
                    if (i == sub) G.Tex(Pix.Color("soul"), x - 26, y + 6, 2);
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
                DrawArena();
                break;
        }
    }

    static bool Directional(string spr) => spr == "b_thorn" || spr == "b_tear";

    void DrawArena()
    {
        var groupOff = new Vector2(box.x + 3, box.y + 3);
        GUI.BeginGroup(new Rect(box.x + 3, box.y + 3, box.width - 6, box.height - 6));
        float ox = -groupOff.x, oy = -groupOff.y;
        // сначала обычные снаряды, потом тьма, потом предупреждения и сердце (их видно в темноте)
        foreach (var b in bullets)
        {
            if (b.size != Vector2.zero || b.t < b.delay) continue;
            var t = Pix.Get(b.spr);
            float x = b.p.x + ox, y = b.p.y + oy;
            G.Glow(x, y, 11, b.spr == "b_dark" ? new Color(0.6f, 0.3f, 1f, 0.3f) : new Color(1, 1, 1, 0.12f));
            if (Directional(b.spr) && b.v.sqrMagnitude > 1)
            {
                float deg = Mathf.Atan2(b.v.y, b.v.x) * Mathf.Rad2Deg;
                G.TexRot(t, x, y, 2, b.spr == "b_thorn" ? deg + 90 : deg - 90, b.tint, groupOff);
            }
            else if (b.spr == "b_letter" || b.spr == "b_page" || b.spr == "b_moth" || b.spr == "b_tape" || b.spr == "b_petal")
                G.TexRot(t, x, y, 2, Mathf.Sin(b.t * 6 + b.p.x) * 18, b.tint, groupOff);
            else G.TexC(t, x, y, 2, b.tint);
        }
        foreach (var b in bullets)
        {
            if (b.size == Vector2.zero) continue;
            bool warn = b.t < b.delay;
            var c = b.tint; c.a = warn ? 0.25f + 0.2f * Mathf.Sin(b.t * 40) : 1;
            float sw = warn ? b.size.x * (b.size.x < 20 ? 0.4f : 1) : b.size.x;
            float sh = warn ? b.size.y * (b.size.y < 20 ? 0.4f : 1) : b.size.y;
            if (!warn) G.Rect(b.p.x - sw / 2 - 4 + ox, b.p.y - sh / 2 - 4 + oy, sw + 8, sh + 8, new Color(c.r, c.g, c.b, 0.25f));
            G.Rect(b.p.x - sw / 2 + ox, b.p.y - sh / 2 + oy, sw, sh, c);
        }

        if (dark > 0.01f && soulBreak < 0)
        {
            // Тьма: видно только пятно вокруг сердца
            float R = 44 / 0.55f;
            float cx = soul.x + ox, cy = soul.y + oy;
            var dc = new Color(0, 0, 0, dark);
            GUI.color = dc;
            GUI.DrawTexture(new Rect(cx - R, cy - R, R * 2, R * 2), Pix.HoleTex);
            GUI.color = Color.white;
            G.Rect(-10, -10, cx - R + 10, 400, dc);
            G.Rect(cx + R, -10, 800, 400, dc);
            G.Rect(cx - R, -10, R * 2, cy - R + 10, dc);
            G.Rect(cx - R, cy + R, R * 2, 400, dc);
        }

        foreach (var b in bullets)
        {
            if (b.size != Vector2.zero || b.t >= b.delay) continue;
            float x = b.p.x + ox, y = b.p.y + oy;
            if (b.spr == "b_eye")
            {
                // глаз медленно открывается
                float k = b.t / b.delay;
                var tex = Pix.Get("b_eye");
                G.Glow(x, y, 16, new Color(1, 0.2f, 0.3f, 0.35f * k));
                G.TexFootXY(tex, x, y + tex.height * k, 2, 2 * k, false, Color.white);
            }
            else G.Rect(x - 6, y - 10, 12, 12, new Color(1, 0.2f, 0.2f, 0.4f + 0.3f * Mathf.Sin(b.t * 30)));
        }

        if (soulBreak < 0)
        {
            bool blink = inv > 0 && (int)(inv * 12) % 2 == 0;
            G.Glow(soul.x + ox, soul.y + oy, 14, new Color(1, 0.2f, 0.3f, 0.25f));
            if (!blink) G.TexC(Pix.Color("soul"), soul.x + ox, soul.y + oy, 2);
        }
        GUI.EndGroup();

        if (soulBreak >= 0)
        {
            var st2 = Pix.Color("soul");
            if (soulBreak < 0.5f)
            {
                G.TexC(st2, soul.x - 3, soul.y, 2);
                G.TexC(st2, soul.x + 3, soul.y, 2, new Color(1, 1, 1, 0.8f));
                G.Line(soul.x - 1, soul.y - 6, soul.x + 1, soul.y + 6, 2, Color.black);
            }
            else
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 1.05f + 0.3f, tt = soulBreak - 0.5f;
                    G.Rect(soul.x + Mathf.Cos(a) * tt * 90, soul.y - Mathf.Sin(a) * tt * 60 + tt * tt * 200, 4, 4, new Color(1, 0.15f, 0.25f));
                }
        }
    }

    void DrawHud()
    {
        G.Text("НОА", 34, 398, 18, Color.white);
        G.Text($"ПУСТ {S.Void}", 110, 398, 18, S.Void > 0 ? new Color(0.75f, 0.75f, 0.75f) : Color.white);
        G.Text("ПАМ", 230, 400, 14, Color.white);
        float bw = Mathf.Min(S.maxHp, 48) * 2.5f;
        G.Rect(270, 400, bw, 18, new Color(0.8f, 0.1f, 0.1f));
        G.Rect(270, 400, bw * S.hp / Mathf.Max(1, S.maxHp), 18, Color.Lerp(new Color(1f, 0.9f, 0.2f), Color.white, Mathf.Clamp01(hurtFlash / 0.3f)));
        G.Text($"{S.hp} / {S.maxHp}", 285 + bw, 398, 18, Color.white);
        if (Diff.Novice) G.Text("[H] ПАМЯТЬ ∞", 500, 400, 14, new Color(0.5f, 1f, 0.6f));

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
