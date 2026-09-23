using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Титульный экран, Game Over и все три концовки
public static class Screens
{
    static Game Gm => Game.I;
    static SaveData S => Game.I.S;
    const string TrueKey = "LoE_true";

    static IEnumerator Say(string who, params string[] l) => Gm.dlg.Say(who, l);

    public static IEnumerator Boot()
    {
        if (PlayerPrefs.GetInt(Game.VoidKey, 0) == 1) { yield return VoidScreen(); yield break; }
        yield return Title();
    }

    // ===================== ТИТУЛ =====================
    static int titleSel;
    static List<string> titleItems = new List<string>();

    public static IEnumerator Title()
    {
        Gm.mode = Mode.Title;
        Gm.fadeColor = Color.black;
        Gm.S = new SaveData();
        Gm.UpdateGray();
        Gm.music.pitchMul = 1;
        titleItems = new List<string>();
        if (Gm.HasSave) titleItems.Add("Продолжить");
        titleItems.Add("Новая игра");
        titleItems.Add("Выход");
        titleSel = 0;
        Gm.onDraw = DrawTitle;
        Gm.music.Play("title", 1f);
        yield return Gm.Fade(0, 1.2f);
        Gm.StartCoroutine(Prewarm());
        while (true)
        {
            if (In.Up) { titleSel = (titleSel + titleItems.Count - 1) % titleItems.Count; Gm.music.Sfx("select"); }
            if (In.Down) { titleSel = (titleSel + 1) % titleItems.Count; Gm.music.Sfx("select"); }
            if (In.Ok) { In.Eat(); Gm.music.Sfx("confirm"); break; }
            yield return null;
        }
        string pick = titleItems[titleSel];
        if (pick == "Выход") { Gm.Quit(); yield break; }
        yield return Gm.Fade(1, 1f);
        Gm.onDraw = null;
        Story.Run(pick == "Продолжить" ? Story.Continue() : Story.NewGame());
    }

    static IEnumerator Prewarm()
    {
        foreach (var k in new[] { "dock", "battle", "boss", "echo", "garden", "neon", "archive", "oculus", "gameover", "tape1", "tape2", "tape3", "tape4",
                     "blip", "select", "confirm", "hurt", "heal", "erase", "slash", "spare", "encounter", "save", "click", "beep" })
        {
            Tracks.Get(k);
            yield return null;
        }
    }

    static void DrawTitle()
    {
        float t = Time.time;
        for (int y = 0; y < 480; y += 8)
            G.Rect(0, y, 640, 8, Color.Lerp(new Color(0.02f, 0.02f, 0.08f), new Color(0.12f, 0.08f, 0.2f), y / 480f));
        var r = new System.Random(3);
        for (int i = 0; i < 70; i++)
        {
            float a = 0.3f + 0.3f * Mathf.Sin(t * (1 + (float)r.NextDouble() * 2) + i);
            G.Rect(r.Next(640), r.Next(300), 2, 2, new Color(1, 1, 1, a));
        }
        // летящие ноты
        for (int i = 0; i < 8; i++)
        {
            float x = (i * 83 + t * 20) % 700 - 30;
            float y = 260 + Mathf.Sin(t + i) * 30 - (t * 10 + i * 40) % 200;
            G.Tex(Pix.Color("b_note"), x, y, 3, false, new Color(1, 0.95f, 0.7f, 0.5f));
        }
        bool woke = PlayerPrefs.GetInt(TrueKey, 0) == 1;
        G.Center("КОЛЫБЕЛЬНАЯ ДЛЯ ЭХА", 90, 40, new Color(1f, 0.95f, 0.85f));
        G.Center("L U L L A B Y   O F   E C H O E S", 142, 16, new Color(0.7f, 0.7f, 0.85f));
        if (woke) G.Center("Ноа проснулся. Но колыбельная всё ещё звучит.", 170, 14, new Color(0.6f, 0.8f, 1f));

        // Ноа и Скрип на причале
        G.Rect(200, 330, 240, 6, new Color(0.3f, 0.28f, 0.35f));
        G.TexFoot(Pix.Color("noa_u0"), 320, 330, 3);
        G.TexFoot(Pix.Color("skrip" + ((int)(t * 3) % 2)), 370, 300 + Mathf.Sin(t * 2) * 5, 3);

        for (int i = 0; i < titleItems.Count; i++)
        {
            bool sel = i == titleSel;
            float y = 360 + i * 32;
            if (sel) G.Tex(Pix.Color("soul"), 250, y + 6, 2);
            G.Text(titleItems[i], 275, y, 22, sel ? new Color(1, 1, 0.3f) : Color.white);
        }
        G.Text("Z — выбрать   стрелки — меню", 10, 458, 12, new Color(0.5f, 0.5f, 0.5f));
    }

    // ===================== ПУСТОТА ПРИ ПОВТОРНОМ ЗАПУСКЕ =====================
    static IEnumerator VoidScreen()
    {
        Gm.mode = Mode.Scene;
        Gm.fade = 0;
        Gm.onDraw = () => G.Rect(0, 0, 640, 480, new Color(0.45f, 0.45f, 0.45f));
        float held = 0;
        while (true)
        {
            // Секрет: удерживать Delete 5 секунд — «прощение»
            if (Input.GetKey(KeyCode.Delete)) held += Time.deltaTime; else held = 0;
            if (held > 5f)
            {
                PlayerPrefs.DeleteKey(Game.VoidKey);
                Gm.DeleteSave();
                PlayerPrefs.Save();
                Gm.onDraw = null;
                Gm.fade = 1;
                yield return Title();
                yield break;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) Gm.Quit();
            yield return null;
        }
    }

    // ===================== GAME OVER =====================
    public static IEnumerator GameOver()
    {
        Gm.mode = Mode.Scene;
        Gm.fade = 1;
        Gm.onDraw = () =>
        {
            G.Center("ПАМЯТЬ ПОТЕРЯНА", 130, 40, new Color(1, 1, 1, 0.9f));
            G.TexC(Pix.Color("soul"), 320, 230, 5, new Color(1, 1, 1, 0.3f + 0.2f * Mathf.Sin(Time.time * 2)));
        };
        Gm.music.pitchMul = 1;
        Gm.music.Play("gameover", 1f);
        yield return Gm.Fade(0, 1.5f);
        yield return Gm.dlg.Memory("mom", "Ноа... не сдавайся.", "Ты должен проснуться. Слышишь?\nДолжен.");
        yield return Game.WaitOk();
        yield return Gm.Fade(1, 1f);
        Gm.onDraw = null;
        if (Gm.HasSave) yield return Story.Continue();
        else yield return Story.NewGame();
    }

    // ===================== ИСТИННАЯ КОНЦОВКА =====================
    public static IEnumerator TrueEnding()
    {
        var W = Gm.world;
        yield return Say("echo", "...Ты уверен?", "Будет больно. Очень.");
        yield return Say("", "Ты киваешь.");
        var oc = new Ent { id = "oc", sprite = "oculus_face", x = W.pos.x - 30, y = W.pos.y - 16, solid = false, tint = new Color(1, 1, 1, 0) };
        W.ents.Add(oc);
        for (float t = 0; t < 1; t += Time.deltaTime) { oc.tint = new Color(1, 1, 1, t); yield return null; }
        yield return Say("oculus2", "Я здесь. Послушаем вместе.");
        yield return Say("echo", "Тогда... дослушай. До конца.");
        yield return Say("", "Ты вставляешь кассету «ТИМ» в плеер.\nНажимаешь PLAY.");
        yield return Story.PlayTape(4);
        yield return Say("", "Плёнка доигрывает до конца.\nВпервые.");

        Gm.music.Sfx("rumble");
        Gm.shake = 2f;
        yield return Say("", "Стигия начинает рушиться.");
        Gm.music.Play("ending", 2f);

        yield return Say("skrip", "Ну вот и всё, Ноа. Лети.\nВ смысле — просыпайся.", "И это... почитай там газету, что ли.\nВ память обо мне.");
        foreach (var id in S.spared)
        {
            var d = Enemies.Get(id);
            string line = Farewell(id);
            if (line != null) yield return Say(d.voice, line);
        }
        if (S.Has("bg3")) yield return Say("npc", "Ты заметил меня. Трижды.\nЯ тебя тоже не забуду, Ноа.");
        yield return Say("echo", "Я была лишь твоим желанием не чувствовать.", "Прости, что держала так крепко.\nПрощай, Ноа.");
        yield return Say("oculus2", "Пора.\nМы проснёмся вместе.");

        Gm.fadeColor = Color.white;
        yield return Gm.Fade(1, 2.5f);
        Gm.music.Stop(1f);
        yield return Game.Wait(1.5f);

        // Эпилог в больнице
        Gm.mode = Mode.Scene;
        bool awake = false;
        Gm.onDraw = () => DrawHospital(awake);
        Gm.music.Play("hospital", 1f);
        yield return Gm.Fade(0, 3f);
        Gm.fadeColor = Color.black;
        yield return Game.Wait(1.5f);
        yield return Say("", "...");
        awake = true;
        yield return Say("", "Ноа открывает глаза.", "За окном идёт дождь.", "На спинке стула висит большая куртка.\nОчень большая.",
            "В коридоре кто-то роняет стаканчик с кофе\nи бежит к палате.",
            "Ноа улыбается. Впервые за долгое время.", "Он готов жить дальше.");
        PlayerPrefs.SetInt(TrueKey, 1);
        PlayerPrefs.Save();
        Gm.music.Play("ending", 3f);
        yield return Game.Wait(2f);
        yield return Credits("ИСТИННАЯ КОНЦОВКА: ПРИНЯТИЕ");
    }

    static string Farewell(string id)
    {
        switch (id)
        {
            case "letter": return "«Прости» дошло! Я слышало!";
            case "clock": return "Время идёт — иди и ты.";
            case "keeper": return "Свет горит. Иди на него.";
            case "tape": return "Доиграла! До самого конца!";
            case "hope": return "Что-то всё равно будет. Помнишь?";
            case "thorn": return "Не колись. Живи.";
            case "moth": return "Лети к луне, Ноа!";
            case "token": return "Я в кармане, помнишь? Звяк!";
            case "merchant": return "Без сделок. Просто живи.";
            case "page": return "Допиши меня!";
        }
        return null;
    }

    static void DrawHospital(bool awake)
    {
        G.Tex(Pix.Color(awake ? "hospital1" : "hospital0"), 0, 0, 2);
        // дождь за окном
        var r = new System.Random(1);
        float t = Time.time;
        for (int i = 0; i < 40; i++)
        {
            float x = 404 + (float)r.NextDouble() * 172;
            float y = 70 + ((float)r.NextDouble() * 170 + t * 260 * (0.8f + (float)r.NextDouble() * 0.4f)) % 170;
            G.Rect(x, y, 1, 7, new Color(0.7f, 0.8f, 0.95f, 0.6f));
        }
        G.Rect(486, 70, 8, 172, new Color(0.94f, 0.94f, 0.94f));
        G.Rect(402, 152, 176, 8, new Color(0.94f, 0.94f, 0.94f));
        // кардиомонитор
        float mx = 288, my = 218;
        for (int i = 0; i < 42; i++)
        {
            float ph = ((i / 42f) + t * 0.5f) % 1f;
            float h = ph > 0.45f && ph < 0.5f ? -14 : ph > 0.5f && ph < 0.55f ? 8 : 0;
            G.Rect(mx + i * 2, my + h, 2, 2, new Color(0.3f, 1f, 0.4f));
        }
    }

    // ===================== КОНЦОВКА ИЛЛЮЗИИ =====================
    public static IEnumerator IllusionEnding()
    {
        Gm.fadeColor = Color.white;
        yield return Gm.Fade(1, 1.5f);
        Gm.mode = Mode.Scene;
        bool faceless = false;
        var cast = new List<string> { "skrip", "echo" };
        foreach (var id in S.spared) if (id != "oculus") cast.Add(Enemies.Get(id).sprite);
        cast.Add("npc_bg");
        Gm.onDraw = () => DrawParty(cast, faceless);
        Gm.music.pitchMul = 1;
        Gm.music.Play("illusion", 0.5f);
        yield return Gm.Fade(0, 1.5f);
        Gm.fadeColor = Color.black;
        yield return Say("", "Ноа остаётся в Стигии.", "Мир становится ярким и красивым.", "Все устраивают праздник.\nМадам Эхо смеётся. Скрип танцует.",
            "Праздник, который никогда не закончится.");
        yield return Game.Wait(3f);
        faceless = true;
        var st = Gm.StartCoroutine(Stutter());
        yield return Say("", "Но если присмотреться...", "...у всех исчезают лица.", "А музыка начинает повторяться.\nСнова. И снова. И снова.");
        yield return Game.Wait(2f);
        yield return Say("", "Это вечный, безупречный,\nно абсолютно мёртвый сон.");
        yield return Gm.Fade(1, 3f);
        Gm.StopCoroutine(st);
        Gm.music.Stop(0.5f);
        yield return Credits("КОНЦОВКА ИЛЛЮЗИИ");
    }

    static IEnumerator Stutter()
    {
        var src = Gm.music.Cur;
        int start = src.timeSamples;
        float seg = 0.55f;
        while (true)
        {
            yield return Game.Wait(seg);
            if (src.clip != null) src.timeSamples = start;
            seg = Mathf.Max(0.12f, seg * 0.93f); // петля становится всё короче
        }
    }

    static void DrawParty(List<string> cast, bool faceless)
    {
        float t = Time.time;
        for (int y = 0; y < 480; y += 8)
        {
            float h = (t * 0.05f + y / 900f) % 1f;
            var c = Color.HSVToRGB(h, faceless ? 0.25f : 0.45f, 1f);
            G.Rect(0, y, 640, 8, c);
        }
        var r = new System.Random(4);
        for (int i = 0; i < 60; i++)
        {
            float x = (float)r.NextDouble() * 640;
            float y = ((float)r.NextDouble() * 480 + t * (60 + i % 5 * 20)) % 480;
            G.Rect(x + Mathf.Sin(t * 3 + i) * 6, y, 5, 5, Color.HSVToRGB((float)r.NextDouble(), 0.8f, 1f));
        }
        G.Rect(0, 230, 640, 90, new Color(1, 1, 1, 0.35f));
        int n = cast.Count + 1;
        float step = Mathf.Min(90, 600f / n);
        float x0 = 320 - step * (n - 1) / 2f;
        // Два ряда, чтобы гости не закрывали друг друга и окно текста
        for (int pass = 0; pass < 2; pass++)
        for (int i = 0; i < n; i++)
        {
            bool back = i % 2 == 0 && i != n / 2;
            if (back != (pass == 0)) continue;
            float x = x0 + i * step;
            float foot = back ? 250 : 300;
            float jump = Mathf.Abs(Mathf.Sin(t * 4 + i * 0.7f)) * 14;
            if (i == n / 2)
            {
                G.TexFoot(Pix.Color(faceless ? "noa_u0" : "noa_d0"), x, foot - jump, 3);
                continue;
            }
            string sp = cast[i < n / 2 ? i : i - 1];
            if (sp == "skrip") sp = "skrip" + ((int)(t * 4) % 2);
            if (faceless && sp == "echo") sp = "echo_noface";
            if (faceless && sp == "npc_bg") sp = "npc_bg_noface";
            var tex = Pix.Color(sp);
            float s = tex != null && tex.height > 30 ? 2 : 3;
            G.TexFoot(tex, x, foot - jump, s, false, faceless && !sp.StartsWith("echo") && !sp.StartsWith("npc") ? new Color(1, 1, 1, 0.35f) : Color.white);
        }
    }

    // ===================== ПУТЬ СТИРАНИЯ =====================
    public static IEnumerator VoidPath()
    {
        var W = Gm.world;
        Ent sk = null;
        if (!W.skripFollow)
        {
            sk = new Ent { id = "skrip_v", sprite = "skrip", anim = true, floaty = true, solid = false, x = W.pos.x + 40, y = W.pos.y };
            W.ents.Add(sk);
        }
        yield return Say("skrip", "Ноа. Стой.", "Посмотри вокруг. Здесь больше нет цветов. Нет света. Нет ничего.",
            "Это ведь всё был ТЫ. Каждый Забытый — кусочек тебя.", "Если сотрёшь ещё хоть что-то — наверх будет некому возвращаться.");
        yield return Game.Wait(1.5f);
        yield return Say("skrip", "...Хотя.", "Ты ведь не Ноа, правда?", $"Ты — {Story.PlayerName}.\nТы просто хотел пройти побыстрее.", "Ну так давай. Жми.");

        yield return Gm.battle.Fight(Enemies.Get("skrip"), true);
        Gm.fade = 1;
        Gm.mode = Mode.World;
        W.skripFollow = false;
        S.Set("skrip_gone");
        if (sk != null) W.Remove("skrip_v");
        Gm.UpdateGray();
        yield return Gm.Fade(0, 1f);
        yield return Game.Wait(1f);

        var e = new Ent { id = "echo", sprite = "echo", x = 35 * 16 + 8, y = 6 * 16 + 14, solid = false };
        W.ents.Add(e);
        yield return Say("echo", "Ты пришёл не остаться.\nТы пришёл всё закончить.", "Я была лишь желанием не чувствовать боль.",
            "Но ты решил не чувствовать ничего.\nСовсем.");
        yield return Gm.battle.Fight(Enemies.Get("echo"), true);

        // Конец
        Gm.mode = Mode.Scene;
        Gm.fade = 0;
        Gm.music.Stop(0.1f);
        float t0 = Time.time;
        Gm.onDraw = () =>
        {
            float k = Mathf.Clamp01((Time.time - t0) / 4f);
            var r = new System.Random((int)(Time.time * 15));
            for (int i = 0; i < (int)(300 * (1 - k)); i++)
            {
                float g = (float)r.NextDouble() * 0.6f;
                G.Rect(r.Next(640), r.Next(480), 6, 6, new Color(g, g, g));
            }
        };
        yield return Say("", "Архив пуст.");
        yield return Say("", "Тишина.");
        PlayerPrefs.SetInt(Game.VoidKey, 1);
        Gm.DeleteSave();
        PlayerPrefs.Save();
        float len = Gm.music.PlayClip("flatline", 0.8f);
        yield return Gm.Fade(1, len - 1f);
        yield return Game.Wait(1.5f);
        Gm.Quit();
    }

    // ===================== ТИТРЫ =====================
    static IEnumerator Credits(string title)
    {
        Gm.mode = Mode.Scene;
        var lines = new List<string>
        {
            title, "", "", "КОЛЫБЕЛЬНАЯ ДЛЯ ЭХА", "Lullaby of Echoes", "", "",
            "Идея", "Антон", "",
            "Разработал", "Лёха", "", "",
            "Ты выслушал:"
        };
        if (S.spared.Count == 0) lines.Add("никого");
        foreach (var id in S.spared) lines.Add(Enemies.Get(id).name);
        lines.Add(""); lines.Add("");
        lines.Add($"Стёрто: {S.erased.Count}");
        lines.Add($"Кассет найдено: {Story.TapeCount}/4");
        lines.Add(""); lines.Add(""); lines.Add("");
        lines.Add("Посвящается всем,");
        lines.Add("кто однажды решился проснуться.");
        float y0 = 500;
        float speed = 34;
        float total = lines.Count * 36 + 500;
        float t = 0;
        Gm.onDraw = () =>
        {
            for (int i = 0; i < lines.Count; i++)
            {
                float y = y0 - t * speed + i * 36;
                if (y < -40 || y > 500) continue;
                bool head = i == 0 || lines[i] == "КОЛЫБЕЛЬНАЯ ДЛЯ ЭХА";
                G.Center(lines[i], y, head ? 26 : 20, head ? new Color(1, 0.9f, 0.4f) : Color.white);
            }
        };
        yield return Gm.Fade(0, 1f);
        while (t * speed < total)
        {
            t += Time.deltaTime * (In.BackHeld ? 4 : 1);
            yield return null;
        }
        yield return Gm.Fade(1, 1.5f);
        Gm.onDraw = null;
        Gm.music.Stop(1f);
        yield return Game.Wait(1f);
        yield return Title();
    }
}
