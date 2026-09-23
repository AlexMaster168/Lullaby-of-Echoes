using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Построитель карт: строки из символов тайлов
public class MB
{
    readonly char[,] g;
    readonly int w, h;

    public MB(int w, int h, char fill)
    {
        this.w = w; this.h = h;
        g = new char[w, h];
        F(0, 0, w, h, fill);
    }

    public MB F(int x, int y, int fw, int fh, char c)
    {
        for (int j = y; j < y + fh; j++) for (int i = x; i < x + fw; i++) if (i >= 0 && j >= 0 && i < w && j < h) g[i, j] = c;
        return this;
    }

    public MB P(int x, int y, char c) => F(x, y, 1, 1, c);

    // Стена поперёк прохода с одной щелью в ряду 7, где стоит Забытый
    public MB Choke(int x, char wall, char mob) { F(x, 2, 1, 10, wall); P(x, 7, mob); return this; }

    public MB Border() { F(0, 0, 1, h, '#'); F(w - 1, 0, 1, h, '#'); return this; }

    public string[] Rows()
    {
        var r = new string[h];
        for (int j = 0; j < h; j++)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < w; i++) sb.Append(g[i, j]);
            r[j] = sb.ToString();
        }
        return r;
    }
}

public static class Story
{
    static Game Gm => Game.I;
    static SaveData S => Game.I.S;
    static World Wd => Game.I.world;

    public static void Run(IEnumerator e) => Gm.StartCoroutine(Cut(e));

    static IEnumerator Cut(IEnumerator e)
    {
        Gm.busy++;
        yield return e;
        Gm.busy = Mathf.Max(0, Gm.busy - 1);
    }

    static IEnumerator Say(string who, params string[] l) => Gm.dlg.Say(who, l);

    public static int TapeCount
    {
        get { int n = 0; for (int i = 1; i <= 4; i++) if (S.Has("tape" + i)) n++; return n; }
    }

    public static string PlayerName
    {
        get
        {
            string n = Environment.UserName;
            return string.IsNullOrEmpty(n) ? "игрок" : n;
        }
    }

    // ===================== ОБЛАСТИ =====================
    static Dictionary<string, Area> areas;

    public static Area GetArea(string id)
    {
        if (areas == null) BuildAreas();
        return areas[id];
    }

    static void BuildAreas()
    {
        areas = new Dictionary<string, Area>();

        // ---------- АКТ 1: ТИХИЙ ПРИЧАЛ ----------
        var m = new MB(46, 15, '.');
        m.F(0, 0, 46, 2, '#');
        for (int x = 2; x < 46; x += 4) m.P(x, 1, 'W');
        m.F(0, 2, 46, 3, ',');
        m.F(0, 12, 46, 3, '~');
        m.Border();
        m.P(4, 2, 'L').P(17, 2, 'L').P(30, 2, 'L').P(42, 2, 'L');
        foreach (var x in new[] { 2, 7, 15, 20, 28, 33, 41 }) m.P(x, 11, 'o');
        m.P(8, 9, 'l').P(20, 5, 'l').P(34, 5, 'l');
        m.P(10, 13, 'C').P(27, 12, 'C').P(36, 13, 'C');
        m.Choke(12, '#', '1').Choke(24, '~', '2').Choke(38, '#', 'K');
        m.P(3, 7, '@').P(7, 4, 'N').P(17, 5, 'S').P(31, 10, 'T').P(20, 8, 'y').P(32, 7, 'x').P(43, 7, 'E');
        var dock = new Area { id = "dock", zone = "dock", music = "dock", title = "Тихий Причал", map = m.Rows() };
        Common(dock, "letter", "clock", "keeper", 1, "garden");
        dock.spawns['N'] = (x, y) => Npc("npc_bg", x, y, NpcBg);
        dock.spawns['y'] = (x, y) => Trigger("echo_dock", x, y, EchoDock);
        dock.spawns['x'] = (x, y) => Trigger("oculus_dock", x, y, OculusDock);
        dock.onEnter = DockIntro;
        areas["dock"] = dock;

        // ---------- АКТ 2: ЗАБЫТЫЙ САД ----------
        m = new MB(46, 15, '.');
        m.F(0, 0, 46, 2, '#');
        for (int x = 1; x < 46; x += 5) m.P(x, 1, 'W');
        m.F(0, 12, 46, 3, '#');
        for (int x = 3; x < 46; x += 6) m.P(x, 12, 'W');
        m.Border();
        m.F(1, 2, 10, 3, ',').F(14, 9, 9, 3, ',').F(26, 2, 10, 3, ',').F(40, 9, 5, 3, ',');
        m.P(3, 3, 'f').P(6, 10, 'F').P(9, 4, 'f').P(16, 3, 'F').P(19, 10, 'f').P(22, 4, 'F').P(28, 10, 'f').P(34, 3, 'F').P(41, 4, 'f').P(43, 10, 'F');
        m.P(15, 5, 'p').P(35, 9, 'p').P(9, 9, 'p');
        m.F(27, 9, 3, 2, '~');
        m.Choke(12, '#', '1').Choke(24, '#', '2').Choke(38, '#', 'K');
        m.P(3, 7, '@').P(6, 4, 'N').P(18, 5, 'S').P(31, 4, 'T').P(15, 7, 'y').P(33, 7, 'x').P(43, 7, 'E');
        var garden = new Area { id = "garden", zone = "garden", music = "garden", title = "Забытый Сад", map = m.Rows() };
        Common(garden, "tape", "hope", "thorn", 2, "neon");
        garden.spawns['N'] = (x, y) => Npc("npc_garden", x, y, NpcGarden);
        garden.spawns['y'] = (x, y) => Trigger("echo_garden", x, y, EchoGarden);
        garden.spawns['x'] = (x, y) => Trigger("oculus_garden", x, y, OculusGarden);
        garden.onEnter = GardenIntro;
        areas["garden"] = garden;

        // ---------- АКТ 3: НЕОНОВЫЙ АРХИПЕЛАГ ----------
        m = new MB(50, 15, '.');
        m.F(0, 0, 50, 2, '#');
        for (int x = 1; x < 50; x += 3) m.P(x, 1, 'W');
        m.F(0, 12, 50, 1, 'W');
        m.F(0, 13, 50, 2, '~');
        m.F(0, 7, 50, 1, '=');
        m.Border();
        m.P(5, 2, 'n').P(16, 2, 'n').P(27, 2, 'n').P(41, 2, 'n').P(47, 2, 'n');
        m.P(9, 10, 'g').P(20, 10, 'g').P(33, 10, 'g').P(46, 10, 'g');
        m.F(18, 2, 1, 5, '|');
        m.Choke(12, '#', '1').Choke(24, '#', '2').Choke(36, '#', 'K').Choke(44, '#', 'O');
        m.P(3, 7, '@').P(7, 4, 'N').P(17, 9, 'T').P(29, 4, 'S').P(40, 5, 's').P(48, 7, 'E');
        var neon = new Area { id = "neon", zone = "neon", music = "neon", title = "Неоновый Архипелаг", map = m.Rows() };
        Common(neon, "moth", "token", "merchant", 3, "archive");
        neon.spawns['N'] = (x, y) => Npc("npc_neon", x, y, NpcNeon);
        neon.spawns['O'] = (x, y) => S.Done("oculus") ? (S.erased.Contains("oculus") ? Enemy("oculus", x, y) : null) : new Ent
        {
            id = "oculus", sprite = "oculus", x = x, y = y, tw = 16, th = 14, onTouch = OculusFinal
        };
        neon.onEnter = NeonIntro;
        areas["neon"] = neon;

        // ---------- БЕСКОНЕЧНЫЙ АРХИВ ----------
        m = new MB(40, 15, '.');
        m.F(0, 0, 40, 2, '#').F(0, 12, 40, 3, '#').Border();
        m.F(1, 6, 38, 3, ',');
        m.F(4, 3, 3, 1, '#').F(4, 10, 3, 1, '#').F(18, 3, 4, 1, '#').F(18, 10, 4, 1, '#').F(26, 3, 3, 1, '#').F(26, 10, 3, 1, '#');
        m.P(9, 4, 'b').P(15, 10, 'b').P(23, 5, 'b').P(29, 9, 'b').P(33, 3, 'b');
        m.Choke(12, '#', '1');
        m.F(37, 2, 2, 10, '~');
        m.P(3, 7, '@').P(7, 4, 'N').P(16, 4, 'S').P(30, 7, 'Z').P(35, 7, 'P');
        var arch = new Area { id = "archive", zone = "arch", music = "archive", title = "Бесконечный Архив", map = m.Rows() };
        arch.spawns['1'] = (x, y) => Enemy("page", x, y);
        arch.spawns['S'] = SavePoint;
        arch.spawns['N'] = (x, y) => Npc("npc_arch", x, y, NpcArch);
        arch.spawns['Z'] = (x, y) => Trigger("finale", x, y, Finale);
        arch.onEnter = ArchiveIntro;
        areas["archive"] = arch;
    }

    static void Common(Area a, string e1, string e2, string boss, int tape, string next)
    {
        a.spawns['1'] = (x, y) => Enemy(e1, x, y);
        a.spawns['2'] = (x, y) => Enemy(e2, x, y);
        a.spawns['K'] = (x, y) => Enemy(boss, x, y);
        a.spawns['S'] = SavePoint;
        a.spawns['s'] = SavePoint;
        a.spawns['T'] = (x, y) => S.Has("tape" + tape) ? null : new Ent
        {
            id = "tape" + tape, sprite = "tape", x = x, y = y, solid = false, floaty = true, onTouch = () => TapeScene(tape)
        };
        a.spawns['E'] = (x, y) => new Ent { id = "exit", sprite = "exit", x = x, y = y + 2, solid = false, tw = 10, th = 20, onTouch = () => Go(next) };
    }

    // ===================== ОБЪЕКТЫ =====================
    static Ent Enemy(string id, float x, float y)
    {
        var d = Enemies.Get(id);
        if (S.erased.Contains(id))
            return new Ent
            {
                id = id, sprite = "static", anim = true, solid = false, x = x, y = y,
                onTalk = () => Say("", "Здесь только шум.", "Кажется, когда-то тут кто-то был.")
            };
        if (S.spared.Contains(id))
            return new Ent
            {
                id = id, sprite = d.sprite, x = x, y = y, solid = false, floaty = true, tint = new Color(1, 1, 1, 0.8f),
                onTalk = () => Say(d.voice, Thanks(id))
            };
        return new Ent { id = id, sprite = d.sprite, x = x, y = y, tw = 16, th = 14, floaty = !d.boss, onTouch = () => Encounter(id) };
    }

    static string Thanks(string id)
    {
        switch (id)
        {
            case "letter": return "Меня прочитали. Спасибо, Ноа.";
            case "clock": return "Тик-так! Я снова иду. И ты иди.";
            case "keeper": return "Свет горит. Иди на него, малыш.";
            case "tape": return "Я доиграла песню! Хочешь, ещё раз? ...Шучу.";
            case "hope": return "Бутон растёт. Медленно, но растёт.";
            case "thorn": return "Я больше не колюсь. Почти.";
            case "moth": return "Луна красивая, правда?";
            case "token": return "Звяк!";
            case "merchant": return "Лавочка закрыта. Навсегда. И это хорошо.";
            case "page": return "Первая строчка уже есть. Дальше — твоя очередь.";
        }
        return "...";
    }

    static Ent SavePoint(float x, float y) => new Ent
    {
        id = "save", sprite = "save", anim = true, x = x, y = y - 4, onTalk = SaveScene
    };

    static Ent Npc(string sprite, float x, float y, Func<IEnumerator> talk) =>
        new Ent { id = sprite, sprite = sprite, x = x, y = y, onTalk = talk, flip = true };

    static Ent Trigger(string flag, float x, float y, Func<IEnumerator> f)
    {
        if (S.Has(flag)) return null;
        return new Ent { id = flag, x = x, y = y, solid = false, tw = 6, th = 120, onTouch = () => Once(flag, f) };
    }

    static IEnumerator Once(string flag, Func<IEnumerator> f)
    {
        if (S.Has(flag)) yield break;
        S.Set(flag);
        Wd.Remove(flag);
        yield return f();
    }

    // ===================== ПЕРЕХОДЫ =====================
    public static IEnumerator NewGame()
    {
        Gm.S = new SaveData();
        Gm.UpdateGray();
        Gm.mode = Mode.Scene;
        Gm.music.Stop(0.5f);
        // Режим Scene и так рисует только чёрный фон; затемнение снимаем, иначе текст интро не виден
        Gm.fade = 0;
        yield return Game.Wait(1f);
        for (int i = 0; i < 3; i++) { Gm.music.Sfx("beep", 0.5f); yield return Game.Wait(1f); }
        yield return Gm.dlg.Memory("doctor", "...всё ещё без изменений?", "Он нас слышит. Я уверен, что слышит.", "Говорите с ним. Говорите как можно больше.");
        yield return Game.Wait(1f);
        yield return Go("dock", null);
    }

    public static IEnumerator Continue()
    {
        Gm.LoadGame();
        Vector2? at = S.px >= 0 ? new Vector2(S.px, S.py) : (Vector2?)null;
        yield return Go(S.area, at, false);
    }

    public static IEnumerator Go(string id, Vector2? at = null, bool autosave = true)
    {
        yield return Gm.Fade(1, 0.4f);
        Gm.mode = Mode.World;
        S.area = id;
        var a = GetArea(id);
        Wd.Load(a, at);
        Wd.skripFollow = S.Has("met_skrip") && !S.Has("skrip_gone");
        Gm.music.pitchMul = 1f - Pix.Gray * 0.25f;
        Gm.music.Play(S.AllErased ? "oculus" : a.music, 0.8f);
        if (autosave) { S.px = -1; Gm.SaveGame(); }
        yield return Gm.Fade(0, 0.5f);
        if (!S.Has("enter_" + id) && a.onEnter != null)
        {
            S.Set("enter_" + id);
            yield return a.onEnter();
        }
        yield return SkipNag();
    }

    static IEnumerator SkipNag()
    {
        if (S.skips >= 12 && !S.Has("skipnag") && Wd.skripFollow)
        {
            S.Set("skipnag");
            yield return Say("skrip", "Слушай... ты вообще читаешь, шо я говорю?", "Я вижу, как ты жмёшь X. Не притворяйся.", "Ладно-ладно. Я журавлик, мне не обидно. Почти.");
        }
    }

    // ===================== БОЙ =====================
    static IEnumerator Encounter(string id)
    {
        var ent = Wd.Find(id);
        yield return PreBattle(id);
        yield return Gm.battle.Fight(Enemies.Get(id));
        var r = Gm.battle.result;
        if (r == BResult.Lost) { yield return Screens.GameOver(); yield break; }

        Gm.fade = 1;
        Gm.mode = Mode.World;
        if (ent != null)
        {
            Wd.ents.Remove(ent);
            var ne = Enemy(id, ent.x, ent.y);
            if (ne != null) Wd.ents.Add(ne);
        }
        Gm.UpdateGray();
        Gm.music.pitchMul = 1f - Pix.Gray * 0.25f;
        Gm.music.Play(Wd.area.music, 0.5f);
        yield return Gm.Fade(0, 0.4f);
        yield return PostBattle(id, r);
    }

    static IEnumerator PreBattle(string id)
    {
        switch (id)
        {
            case "keeper":
                yield return Say("keeper", "Стой! Дальше нельзя!", "Там... ничего нет. Совсем ничего!");
                if (Wd.skripFollow) yield return Say("skrip", "Это Смотритель Маяка. Он никого не пускает дальше. Никогда.");
                break;
            case "thorn":
                yield return Say("thorn", "ТЫ! Ты тоже пришёл топтать мой сад?!");
                break;
            case "merchant":
                yield return Say("merchant", "Добро пожаловать в лавку «Если бы»!", "У меня есть именно то, что ты ищешь. Прошлое. Без царапин.");
                break;
        }
    }

    static IEnumerator PostBattle(string id, BResult r)
    {
        bool sk = Wd.skripFollow;
        if (r == BResult.Spared && !S.Has("first_spare") && sk)
        {
            S.Set("first_spare");
            yield return Say("skrip", "Видал? Оно просто хотело, чтобы его выслушали.", "Иногда это всё, что нужно.");
        }
        if (r == BResult.Erased && sk)
        {
            int n = S.erased.Count;
            if (n == 1 && S.Set2("erase1"))
                yield return Say("skrip", "...Ну. Так тоже можно. Наверное.", "Быстро, по крайней мере.");
            else if (n == 4 && S.Set2("erase4"))
                yield return Say("skrip", "Ноа... тебе не кажется, что туман стал серее?", "И музыка как будто... тише.");
            else if (n == 7 && S.Set2("erase7"))
                yield return Say("skrip", "Я больше не буду шутить, ладно?", "Просто... иди.");
        }
        if (id == "keeper" && r == BResult.Spared && sk)
            yield return Say("skrip", "Свет... Смотри, туман расходится!", "Первый шаг сделан, Ноа.");
        if (id == "thorn" && r == BResult.Spared && sk)
            yield return Say("skrip", "«Из-за меня он поехал»... Кто — он?", "Ноа, у тебя есть брат?");
    }

    // ===================== ТОЧКА СОХРАНЕНИЯ =====================
    static IEnumerator SaveScene()
    {
        string line;
        if (Pix.Gray > 0.6f) line = "Тишина. Серость. Больше ничего.";
        else switch (Wd.area.zone)
            {
                case "dock": line = "Туман пахнет морем и чернилами.\nЭто наполняет тебя НАДЕЖДОЙ."; break;
                case "garden": line = "Кассеты-цветы тихо шуршат плёнкой.\nЭто наполняет тебя НАДЕЖДОЙ."; break;
                case "neon": line = "Неон гудит колыбельную на одной ноте.\nЭто наполняет тебя НАДЕЖДОЙ."; break;
                default: line = "Бесконечные полки. Где-то здесь хранится твоё имя.\nЭто наполняет тебя НАДЕЖДОЙ."; break;
            }
        Gm.music.Sfx("save");
        S.hp = S.maxHp;
        S.hums = 3;
        yield return Say("", line, "ПАМЯТЬ восстановлена. Напевы восстановлены.");
        yield return Gm.dlg.Ask("", "Сохранить игру?", "Сохранить", "Не надо");
        if (Gm.dlg.Choice == 0)
        {
            S.px = Wd.pos.x; S.py = Wd.pos.y;
            Gm.SaveGame();
            Gm.music.Sfx("save");
            yield return Say("system", "Сохранено.");
        }
    }

    // ===================== КАССЕТЫ =====================
    static IEnumerator TapeScene(int n)
    {
        Wd.Remove("tape" + n);
        S.Set("tape" + n);
        string[] labels = { "", "ДЫШИ", "ДЛЯ НОА", "17:42" };
        yield return Say("", $"Ты находишь кассету.\nНа наклейке детским почерком: «{labels[n]}».", "Ты вставляешь её в плеер и нажимаешь PLAY.");
        yield return PlayTape(n);
        switch (n)
        {
            case 1:
                yield return Say("", "Звук кажется очень знакомым.\nОт него холодеет внутри.");
                if (Wd.skripFollow) yield return Say("skrip", "Это... что за писк такой? Как будто будильник, только грустный.");
                break;
            case 2:
                yield return Say("", "Ты смотришь на свою куртку.\nРукава свисают почти до колен.");
                if (Wd.skripFollow) yield return Say("skrip", "Слушай... а ведь куртка правда не твоего размера.", "Чья она, Ноа?");
                break;
            case 3:
                yield return Say("", "Ты вспоминаешь.", "Машина. Дождь. Тим за рулём.\nВы возвращались из зала игровых автоматов.", "Стигия — не сказка.\nЭто кома.");
                if (Wd.skripFollow) yield return Say("skrip", "...Ноа. Кажется, я понял, где мы. Где ТЫ.", "Ты лежишь где-то там, наверху.\nИ твоё сердце пищит в аппарате.", "Нам надо торопиться.");
                break;
        }
    }

    public static IEnumerator PlayTape(int n)
    {
        string prev = Gm.music.current;
        Gm.music.Stop(0.4f);
        Gm.music.Sfx("click");
        yield return Game.Wait(0.5f);
        float len = Gm.music.PlayClip("tape" + n, 0.9f);
        float start = Time.time;
        switch (n)
        {
            case 1:
                yield return Gm.dlg.Memory("doctor", "...пульс стабильный.", "Мам, он нас слышит?", "Врачи говорят, что да.\nГоворите с ним. Говорите больше.");
                break;
            case 2:
                yield return Gm.dlg.Memory("mom", "Жил-был мальчик, который очень боялся темноты...", "И тогда старший брат отдал ему свою куртку.\nБольшую-пребольшую.", "«В ней не страшно, — сказал он. — Она волшебная».", "...Ноа, солнышко. Возвращайся. Пожалуйста.");
                break;
            case 3:
                yield return Gm.dlg.Memory("bro", "Эй, мелкий, пристегнись. И не крути радио!", "...Ха, опять эта песня? Ладно, подпевай.");
                while (Time.time - start < 5.2f) yield return null;
                Gm.shake = 0.6f;
                yield return Gm.dlg.Memory("system", "Свет фар. Визг тормозов. Удар.", "...", "Тишина.");
                break;
            case 4:
                yield return Gm.dlg.Memory("bro",
                    "Эй, мелкий. Если ты это слушаешь — значит, я опять записал всякую чушь поверх твоих мультиков.",
                    "Короче. Куртку можешь забрать.\nОна тебе велика, но ты дорастёшь.",
                    "И это... что бы ни случилось — живи на полную, ладно?\nЗа двоих, если надо.",
                    "Люблю тебя, мелкий.\nТолько никому не говори.");
                break;
        }
        while (Time.time - start < len) { if (In.Back) { In.Eat(); break; } yield return null; }
        Gm.music.StopSfx();
        Gm.music.Sfx("click");
        if (prev != null && n != 4) Gm.music.Play(prev, 1f);
    }

    // ===================== АКТ 1 =====================
    static IEnumerator DockIntro()
    {
        yield return Say("", "Ты открываешь глаза.", "Вокруг — туман, старые письма и тишина.", "Ты помнишь только своё имя.\nНоа.");
        var sk = new Ent { id = "skrip_intro", sprite = "skrip", anim = true, floaty = true, solid = false, x = Wd.pos.x + 200, y = Wd.pos.y - 20 };
        Wd.ents.Add(sk);
        for (float t = 0; t < 1.2f; t += Time.deltaTime) { sk.x = Mathf.Lerp(Wd.pos.x + 200, Wd.pos.x + 30, t / 1.2f); yield return null; }
        yield return Say("skrip", "О! Живой! Ну, относительно.", "Не пугайся. Я Скрип. Журавлик. Из газеты.\nДа, из газеты, не спрашивай.",
            "Ты в Стигии. Сюда сваливается всё, что люди забыли. Мысли. Мечты. Носки.",
            "А ты... ты вообще не должен тут быть. Ты ещё не забыт.");
        yield return Say("skrip", "А что это у тебя? Плеер? Выключенный.", "Слушай внимательно. Чтобы вернуться НАВЕРХ, надо пройти всю Стигию до Бесконечного Архива. Там Выход.",
            "По пути будут Забытые. Они не злые. Просто... потерянные.",
            "С ними можно по-разному. Можно ВЫСЛУШАТЬ.\nА можно СТЕРЕТЬ. Быстро и без соплей.",
            "Решать тебе. Но учти — Стигия запоминает всё.", "Пошли. И держись поближе, тут туман.");
        Wd.Remove("skrip_intro");
        S.Set("met_skrip");
        Wd.skripFollow = true;
        yield return Say("system", "[Стрелки] идти   [Z] действие\n[X] пропустить текст   [C] статы");
    }

    static IEnumerator NpcBg()
    {
        if (S.erased.Count >= 3)
        {
            yield return Say("npc", "...ты стираешь их, да?", "Пожалуйста. Не подходи ко мне.");
            yield break;
        }
        int n = S.Has("bg3") ? 4 : S.Has("bg2") ? 3 : S.Has("bg1") ? 2 : 1;
        switch (n)
        {
            case 1:
                S.Set("bg1");
                yield return Say("npc", "О. Ты со мной говоришь?", "Обычно меня просто обходят. Я фоновый персонаж.\nСтою тут для атмосферы.");
                break;
            case 2:
                S.Set("bg2");
                yield return Say("npc", "Ты опять пришёл? Два раза подряд?..", "Никто никогда не говорил со мной дважды.\nЯ даже реплики не подготовил.");
                break;
            case 3:
                S.Set("bg3");
                yield return Say("npc", "Три раза! Ты... правда считаешь меня важным?", "Я запомню тебя, Ноа.\nДаже если меня самого все забудут.");
                break;
            default:
                yield return Say("npc", "Спасибо, что заметил меня.\nЭто много значит. Для фона.");
                break;
        }
    }

    static IEnumerator EchoDock()
    {
        var e = new Ent { id = "echo", sprite = "echo", x = Wd.pos.x + 70, y = Wd.pos.y - 40, solid = false };
        Wd.ents.Add(e);
        Gm.music.Play("echo", 0.8f);
        yield return Say("echo", "Какой милый малыш. И совсем один.", "Я — Мадам Эхо. Хранительница этого места.",
            "Скрип наверняка уже наплёл тебе про Выход.\nНе слушай его.",
            "Наверху — дождь, больничные коридоры и боль.", "А здесь никто никогда не плачет.\nОставайся. Сколько захочешь.");
        yield return Say("skrip", "Не слушай её, Ноа.");
        yield return Say("echo", "Как грубо. Ничего.\nЯ подожду. Я всегда жду.");
        for (float t = 0; t < 1; t += Time.deltaTime) { e.tint = new Color(1, 1, 1, 1 - t); yield return null; }
        Wd.Remove("echo");
        Gm.music.Play(Wd.area.music, 1f);
    }

    static IEnumerator OculusDock()
    {
        Gm.music.Stop(0.3f);
        var o = new Ent { id = "oc", sprite = "oculus", x = Wd.pos.x + 110, y = Wd.pos.y, solid = false };
        Wd.ents.Add(o);
        yield return Game.Wait(0.8f);
        yield return Say("oculus", "...", "...плеер...");
        yield return Say("skrip", "НОА! Назад! Это Окулюс!", "Он охотится за такими, как ты.\nНе дай ему забрать плеер!");
        for (float t = 0; t < 1; t += Time.deltaTime) { o.tint = new Color(1, 1, 1, 1 - t); yield return null; }
        Wd.Remove("oc");
        Gm.music.Play(Wd.area.music, 1f);
        yield return Say("skrip", "...Ушёл. Жуть.", "У него даже лица нет. Представляешь? Совсем.");
    }

    // ===================== АКТ 2 =====================
    static IEnumerator GardenIntro()
    {
        if (!Wd.skripFollow) yield break;
        yield return Say("skrip", "Забытый Сад. Сюда попадают несбывшиеся надежды.", "Видишь, вместо цветов — кассеты?\nКто-то очень долго слушал одно и то же.");
    }

    static IEnumerator NpcGarden()
    {
        if (S.erased.Count >= 3)
        {
            yield return Say("gardener", "Твой плеер... от него веет холодом.", "Что ты сделал с теми, кого встретил?");
            yield break;
        }
        yield return Say("gardener", "Я поливаю эти цветы уже сто лет.", "Они не растут. Но если перестать поливать — завянут совсем.",
            "Знаешь, иногда держаться за что-то — это и есть жить.");
    }

    static IEnumerator EchoGarden()
    {
        var e = new Ent { id = "echo", sprite = "echo", x = Wd.pos.x + 60, y = Wd.pos.y - 30, solid = false };
        Wd.ents.Add(e);
        Gm.music.Play("echo", 0.8f);
        yield return Say("echo", "Снова ты. Устал?", "Посмотри на этот сад. Эти надежды никогда не сбудутся.\nНо и не умрут. Разве это не прекрасно?",
            "Наверху тебя ждёт только правда.\nА правда колется, как тот терновник.");
        if (S.erased.Count > 0) yield return Say("echo", "Я вижу, ты уже учишься забывать.\nХорошо. Это правильно.");
        yield return Gm.dlg.Ask("echo", "Хочешь отдохнуть здесь? Совсем чуть-чуть?", "Нет", "...");
        yield return Say("echo", Gm.dlg.Choice == 0 ? "Упрямый. Весь в... впрочем, неважно." : "Молчание — тоже ответ. Я подожду.");
        for (float t = 0; t < 1; t += Time.deltaTime) { e.tint = new Color(1, 1, 1, 1 - t); yield return null; }
        Wd.Remove("echo");
        Gm.music.Play(Wd.area.music, 1f);
    }

    static IEnumerator OculusGarden()
    {
        Gm.music.Stop(0.2f);
        var o = new Ent { id = "oc", sprite = "oculus", x = Wd.pos.x - 90, y = Wd.pos.y, solid = false };
        Wd.ents.Add(o);
        float x0 = o.x;
        for (float t = 0; t < 1.5f; t += Time.deltaTime) { o.x = Mathf.Lerp(x0, Wd.pos.x - 30, t / 1.5f); yield return null; }
        yield return Say("oculus", "...отдай...", "...тебе... нельзя... слушать...");
        yield return Say("skrip", "Он догоняет! Ноа, не стой столбом!");
        Gm.shake = 0.3f;
        for (float t = 0; t < 0.6f; t += Time.deltaTime) { o.tint = new Color(1, 1, 1, 1 - t / 0.6f); yield return null; }
        Wd.Remove("oc");
        Gm.music.Play(Wd.area.music, 1f);
        yield return Say("skrip", "Фух. Растворился.", "Странно... Он ведь мог схватить тебя. Но не схватил.");
    }

    // ===================== АКТ 3 =====================
    static IEnumerator NeonIntro()
    {
        if (!Wd.skripFollow) yield break;
        yield return Say("skrip", "Неоновый Архипелаг! Город, который работает на ностальгии.", "Каждая лампочка тут — чьё-то «а помнишь, как...».");
    }

    static IEnumerator NpcNeon()
    {
        if (S.erased.Count >= 3)
        {
            yield return Say("neonguy", "Слышал, кто-то стирает Забытых.\nСерость ползёт от самого причала.", "Это не ты, случайно? ...Лучше не отвечай.");
            yield break;
        }
        yield return Say("neonguy", "Эй, малой, хочешь сон? Свежий, только что из детства!", "Нет? Ну и ладно.", "Тут все чем-то торгуют. Воспоминаниями, в основном.\nА Торговцу на площади лучше вообще ничего не продавай.");
    }

    static IEnumerator OculusFinal()
    {
        var ent = Wd.Find("oculus");
        Gm.music.Stop(0.5f);
        yield return Say("oculus", "...", "...отдай...");
        if (Wd.skripFollow) yield return Say("skrip", "Ноа, беги! ...Ноа?");
        yield return Say("", "Бежать больше некуда.\nПозади — весь пройденный путь.");
        yield return Gm.battle.Fight(Enemies.Get("oculus"));
        var r = Gm.battle.result;
        if (r == BResult.Lost) { yield return Screens.GameOver(); yield break; }

        Gm.fade = 1;
        Gm.mode = Mode.World;
        Gm.UpdateGray();
        if (r == BResult.Erased)
        {
            if (ent != null) { Wd.ents.Remove(ent); Wd.ents.Add(Enemy("oculus", ent.x, ent.y)); }
            Gm.music.pitchMul = 1f - Pix.Gray * 0.25f;
            Gm.music.Play(Wd.area.music, 0.5f);
            yield return Gm.Fade(0, 0.4f);
            if (Wd.skripFollow) yield return Say("skrip", "...Он исчез.", "Почему мне кажется, что мы потеряли что-то очень важное?");
            yield break;
        }

        // Правда
        if (ent != null) ent.sprite = "oculus_face";
        Gm.music.Play("oculus", 1f);
        yield return Gm.Fade(0, 0.8f);
        yield return Say("oculus2", "Ты не убежал.", "Я не хотел забрать плеер.\nЯ хотел, чтобы ты его дослушал.",
            "Ты ведь уже понял, да?\nЯ — Ноа. Настоящий.",
            "А ты — тот, кем я хотел быть.\nТот, кто ничего не помнит. Кому не больно.",
            "Мы попали в аварию. Я и Тим.\nТим... не выжил.",
            "А я решил не просыпаться.\nПотому что проснуться — значит жить в мире, где его нет.",
            "Мадам Эхо — это моё желание всё забыть.\nОна не злая. Она просто очень хочет, чтобы мне не было больно.");
        yield return Say("", "Он протягивает тебе последнюю кассету.\nНа ней написано: «ТИМ».");
        S.Set("tape4");
        S.Set("oculus_truth");
        yield return Say("oculus2", "Я не могу её дослушать. Я пробовал. Много раз.", "Может, у тебя получится.\nИди в Архив. Я буду рядом.");
        for (float t = 0; t < 1.2f; t += Time.deltaTime) { if (ent != null) ent.tint = new Color(1, 1, 1, 1 - t / 1.2f); yield return null; }
        Wd.Remove("oculus");
        if (Wd.skripFollow)
            yield return Say("skrip", "...Ого.", "Знаешь, я ведь тоже, наверное, кусочек тебя.\nГазета... Тим читал тебе комиксы из газеты. По воскресеньям.",
                "Ладно. Не раскисаем. Пошли. Нас ждут наверху.");
        Gm.music.Play(Wd.area.music, 1f);
    }

    // ===================== АРХИВ И ФИНАЛ =====================
    static IEnumerator ArchiveIntro()
    {
        if (!Wd.skripFollow) yield break;
        yield return Say("skrip", "Бесконечный Архив.\nЗдесь хранится всё, что было по-настоящему важно.");
        if (S.erased.Count >= 8)
            yield return Say("skrip", "...Тут всё серое. Раньше Архив был цветным, я помню.", "Ноа... что мы наделали?");
    }

    static IEnumerator NpcArch()
    {
        if (S.erased.Count > 0)
        {
            yield return Say("archivist", "Тише.", $"Твои полки горят серым. {S.erased.Count} пустых мест.", "Ты стираешь себя, знаешь?");
            yield break;
        }
        yield return Say("archivist", "Тише. Здесь хранят память.", "Каждая полка — чей-то прожитый день.\nТвои полки, кстати, снова начали заполняться.");
    }

    static IEnumerator Finale()
    {
        Gm.music.Stop(1f);
        yield return Game.Wait(1f);
        if (S.AllErased) { yield return Screens.VoidPath(); yield break; }

        var e = new Ent { id = "echo", sprite = "echo", x = 35 * 16 + 8, y = 6 * 16 + 14, solid = false };
        Wd.ents.Add(e);
        Gm.music.Play("echo", 1.5f);
        yield return Say("echo", "Ты дошёл. Никто не доходил так далеко.", "За мной — Выход. Дверь наверх.", "Но сначала послушай меня, Ноа.");
        if (S.Has("oculus_truth"))
            yield return Say("echo", "Ты ведь уже всё знаешь. Про Тима. Про ту дорогу. Про 17:42.");
        else
            yield return Say("echo", "Ты ведь чувствуешь: наверху случилось что-то страшное.\nТебе не нужно знать, что именно.");
        yield return Say("echo", "Я могу сделать этот мир идеальным.\nНикакой боли. Никаких кассет. Вечный праздник.",
            "Все, кого ты здесь встретил, будут рядом. Навсегда.");
        yield return Gm.dlg.Ask("echo", "Останься со мной, Ноа.", "Остаться", "Проснуться");
        if (Gm.dlg.Choice == 0) { yield return Screens.IllusionEnding(); yield break; }

        if (S.Has("tape4") && S.erased.Count == 0) { yield return Screens.TrueEnding(); yield break; }

        if (S.Has("tape4"))
        {
            yield return Say("echo", "Проснуться? С дырами в памяти?", "Ты стёр слишком много, Ноа. Посмотри вокруг.");
            yield return Say("", "Ты вставляешь кассету «ТИМ».\nПлёнка шуршит... и обрывается.", "Не хватает кусочков.\nТех, что ты стёр.");
        }
        else
        {
            yield return Say("echo", "Проснуться? Но чтобы проснуться, нужно дослушать.", "А тебе нечего слушать.");
            yield return Say("", "Плеер пуст.\nПоследняя кассета осталась у того, кого ты не выслушал.");
        }
        yield return Say("echo", "Тише, тише. Ничего.\nЯ же говорила — здесь никто не плачет.");
        yield return Screens.IllusionEnding();
    }
}

public static class SaveExt
{
    // Выставляет флаг и возвращает true, если его ещё не было
    public static bool Set2(this SaveData s, string f)
    {
        if (s.Has(f)) return false;
        s.Set(f);
        return true;
    }
}
