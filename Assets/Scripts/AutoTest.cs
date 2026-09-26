using System.Collections;
using System.IO;
using UnityEngine;

// Бот-тестировщик. Запуск: LullabyOfEchoes.exe -autotest pacifist|erase|mixed|shot -shotdir <папка>
// В обычной игре не активен.
public static class AutoTest
{
    public static string Mode;
    static string shotDir;
    static int shotN;

    public static void ReadArgs()
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++)
        {
            if (a[i] == "-autotest") Mode = a[i + 1];
            if (a[i] == "-shotdir") shotDir = a[i + 1];
        }
    }

    static Game Gm => Game.I;
    static World Wd => Game.I.world;

    static void Log(string s) => Debug.Log("[AUTOTEST] " + s);

    static IEnumerator Shot(string name)
    {
        if (shotDir == null) yield break;
        Directory.CreateDirectory(shotDir);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, $"{++shotN:00}_{Mode}_{name}.png"));
        yield return null;
    }

    static bool Idle => Gm.mode == global::Mode.World && Gm.busy == 0 && !Gm.dlg.Active && Gm.fade < 0.05f;

    static IEnumerator WaitIdle(float timeout = 400)
    {
        float t = 0;
        while (!Idle && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
        if (t >= timeout) Log("TIMEOUT waiting idle, mode=" + Gm.mode + " busy=" + Gm.busy);
        yield return null;
    }

    public static IEnumerator Run()
    {
        Application.logMessageReceived += (msg, st, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error) Debug.Log("[AUTOTEST] ERROR: " + msg + "\n" + st);
        };

        Application.wantsToQuit += () => { Log("QUIT requested: " + System.Environment.StackTrace); return true; };
        Gm.StartCoroutine(Watch());

        if (Mode == "idle")
        {
            In.auto = true;
            for (int i = 0; i < 36; i++)
            {
                yield return new WaitForSecondsRealtime(5);
                Log($"alive {i * 5 + 5}s mode={Gm.mode} busy={Gm.busy} dlg={Gm.dlg.Active} area={Wd.area?.id} focus={Application.isFocused}");
            }
            Log("DONE idle");
            Application.Quit();
            yield break;
        }

        if (Mode == "scenes")
        {
            PlayerPrefs.DeleteAll();
            In.auto = true;
            In.autoChoice = 1;
            Time.timeScale = 3;
            yield return WaitIdle();
            Time.timeScale = 1;
            foreach (var id in new[] { "letter", "clock", "keeper", "tape", "hope", "thorn", "moth", "token", "merchant", "page" }) Gm.S.spared.Add(id);
            Gm.S.Set("tape4");
            Story.Run(Story.Go("garden"));
            yield return new WaitForSecondsRealtime(1);
            yield return WaitIdle();
            Wd.pos = new Vector2(8 * 16, 4 * 16 + 14);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("garden_decor");
            Gm.StartCoroutine(EndingShots());
            Story.Run(Screens.IllusionEnding());
            while (Gm.mode != global::Mode.Title) yield return null;
            yield return new WaitForSecondsRealtime(1);
            yield return WaitIdle();
            foreach (var id in new[] { "letter", "clock", "keeper" }) if (!Gm.S.spared.Contains(id)) Gm.S.spared.Add(id);
            Gm.S.Set("tape4");
            Story.Run(Story.Go("archive"));
            yield return new WaitForSecondsRealtime(1);
            yield return WaitIdle();
            Story.Run(Screens.TrueEnding());
            while (Gm.mode != global::Mode.Title) yield return null;
            Log("DONE scenes");
            Application.Quit();
            yield break;
        }

        if (Mode == "novice")
        {
            PlayerPrefs.DeleteAll();
            Screens.DefaultDiff = 0;
            yield return new WaitForSecondsRealtime(2f);
            In.auto = true;
            while (!Screens.ChoosingDiff) yield return null;
            In.auto = false;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("diff_menu");
            In.auto = true;
            In.autoChoice = 1;
            Battle.Autopilot = true;
            Time.timeScale = 4;
            yield return WaitIdle();
            Log("novice diff=" + Gm.S.diff + " maxHp=" + Gm.S.maxHp + " name=" + Diff.Name);
            Gm.StartCoroutine(BattleShots("letter"));
            yield return Touch("letter");
            Log("DONE novice");
            Application.Quit();
            yield break;
        }

        if (Mode == "intro")
        {
            PlayerPrefs.DeleteAll();
            In.auto = true;
            while (Gm.mode != global::Mode.Scene) yield return null;
            In.auto = false;
            while (!Gm.dlg.Active) yield return null;
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("intro");
            Log("DONE intro");
            Application.Quit();
            yield break;
        }

        if (Mode == "shot")
        {
            yield return new WaitForSecondsRealtime(3);
            yield return Shot("boot");
            Log("DONE");
            Application.Quit();
            yield break;
        }

        PlayerPrefs.DeleteAll();
        Time.timeScale = 5;
        In.auto = true;
        Battle.Autopilot = true;
        Battle.EraseAll = Mode == "erase";
        In.autoChoice = 1; // «Нет» Эхо в саду, «Проснуться» в финале

        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("title");
        yield return WaitIdle();
        yield return Shot("dock");

        yield return Area(new[] { "N", "letter", "save", "y", "clock", "tape1", "x", "keeper" });
        yield return Exit();
        yield return Shot("garden");
        yield return Area(new[] { "N", "tape", "y", "save", "hope", "tape2", "x", "thorn" });
        yield return Exit();
        yield return Shot("neon");
        yield return Area(new[] { "N", "moth", "tape3", "token", "save", "merchant", "oculus" });
        yield return Exit();
        yield return Shot("archive");
        yield return Area(new[] { "N", "page", "save" });

        Log($"Before finale: spared={string.Join(",", Gm.S.spared)} erased={string.Join(",", Gm.S.erased)} tapes={Story.TapeCount} skips={Gm.S.skips}");
        Gm.StartCoroutine(EndingShots());
        yield return Touch("finale");

        // Ждём возврата на титул (или выхода игры в пути стирания)
        float t = 0;
        while (!(Gm.mode == global::Mode.Title && Gm.fade < 0.05f) && t < 600) { t += Time.unscaledDeltaTime; yield return null; }
        Log("DONE mode=" + Gm.mode + " void=" + PlayerPrefs.GetInt(Game.VoidKey, 0) + " true=" + PlayerPrefs.GetInt("LoE_true", 0));
        yield return Shot("title_after");
        yield return new WaitForSecondsRealtime(0.5f);
        Application.Quit();
    }

    static IEnumerator Watch()
    {
        var last = Gm.mode;
        float t = 0;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            if (Gm.mode != last) { Log($"mode {last} -> {Gm.mode} busy={Gm.busy} t={t:0.0}"); last = Gm.mode; }
            yield return null;
        }
    }

    static IEnumerator EndingShots()
    {
        bool hosp = false, party = false, credits = false;
        while (true)
        {
            if (Gm.mode == global::Mode.Scene && Gm.fade < 0.05f && Gm.dlg.Active)
            {
                if (!hosp && Gm.onDraw != null && Gm.onDraw.Method.Name.Contains("TrueEnding")) { hosp = true; yield return Shot("hospital"); }
                else if (!party && Gm.onDraw != null && Gm.onDraw.Method.Name.Contains("Illusion")) { party = true; yield return Shot("party"); yield return new WaitForSecondsRealtime(4); yield return Shot("party_faceless"); }
            }
            if (!credits && Gm.mode == global::Mode.Scene && Gm.onDraw != null && Gm.onDraw.Method.Name.Contains("Credits") && Gm.fade < 0.05f)
            {
                credits = true;
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Shot("credits");
            }
            yield return null;
        }
    }

    static IEnumerator Area(string[] steps)
    {
        foreach (var s in steps)
        {
            yield return WaitIdle();
            Log("step " + Wd.area.id + ":" + s);
            if (s == "N") { yield return Talk(Wd.ents.Find(e => e.id.StartsWith("npc"))); continue; }
            if (s == "save") { yield return Talk(Wd.Find("save")); continue; }
            if (s == "y" || s == "x") { yield return Touch(Wd.ents.Find(e => e.id.StartsWith(s == "y" ? "echo_" : "oculus_"))?.id); continue; }
            if (Mode == "mixed" && s == "letter") Battle.EraseAll = true;
            bool isEnemy = Wd.Find(s) != null && s != "save" && !s.StartsWith("tape");
            if (isEnemy) Gm.StartCoroutine(BattleShots(s));
            yield return Touch(s);
            Battle.EraseAll = Mode == "erase";
        }
        yield return WaitIdle();
    }

    static IEnumerator BattleShots(string id)
    {
        bool menu = false, dodge = false, darkShot = false, shrinkShot = false;
        while (Gm.mode != global::Mode.Battle) yield return null;
        while (Gm.mode == global::Mode.Battle)
        {
            if (!menu && Gm.battle.InMenu && Gm.fade < 0.05f) { menu = true; yield return new WaitForSecondsRealtime(0.2f); yield return Shot(id + "_menu"); }
            if (!dodge && Gm.battle.InDodge && Gm.battle.BulletCount > 3) { dodge = true; yield return Shot(id + "_dodge"); }
            if (!darkShot && Gm.battle.InDark) { darkShot = true; yield return new WaitForSecondsRealtime(0.3f); yield return Shot(id + "_dark"); }
            if (!shrinkShot && Gm.battle.Shrinking) { shrinkShot = true; yield return Shot(id + "_shrink"); }
            yield return null;
        }
    }

    static IEnumerator Talk(Ent e)
    {
        if (e == null) { Log("missing ent to talk"); yield break; }
        Story.Run(e.onTalk());
        yield return null;
        yield return WaitIdle();
    }

    static IEnumerator Touch(string id)
    {
        var e = id == null ? null : Wd.Find(id);
        if (e == null) { Log("missing ent " + id); yield break; }
        Wd.pos = new Vector2(e.solid ? e.x - 12 : e.x, e.y);
        yield return null;
        yield return null;
        yield return WaitIdle();
    }

    static IEnumerator Exit()
    {
        var e = Wd.Find("exit");
        if (e == null) { Log("missing exit in " + Wd.area.id); yield break; }
        string from = Wd.area.id;
        Wd.pos = new Vector2(e.x, e.y);
        float t = 0;
        while (Wd.area.id == from && t < 30) { t += Time.unscaledDeltaTime; yield return null; }
        yield return WaitIdle();
        Log("entered " + Wd.area.id);
    }
}
