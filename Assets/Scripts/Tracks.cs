using System.Collections.Generic;
using UnityEngine;
using V = Synth.Voice;
using W = Synth.Wv;

// Вся музыка игры. Главный лейтмотив — «Колыбельная» (ля минор, 3/4),
// он проходит через все зоны в разных аранжировках, как в Undertale.
public static class Tracks
{
    // ---- Мелодии ----
    public const string LULL =
        "A4:2 C5:2 E5:2  D5:3 C5:1 B4:2  C5:2 A4:2 E4:2  A4:6 " +
        "F4:2 A4:2 C5:2  B4:3 A4:1 G#4:2 A4:2 B4:2 C5:2  B4:6 " +
        "A4:2 C5:2 E5:2  G5:3 F5:1 E5:2  D5:2 F5:2 A4:2  C5:4 B4:2 " +
        "A4:2 E5:2 D5:2  C5:3 B4:1 G#4:2 A4:6  -:6";
    public const string LULL_PROG = "Am G Am Am F E Am E Am C Dm Am Am E Am Am";

    // Финальная версия: последняя фраза разрешается в ЛЯ МАЖОР — принятие.
    public const string LULL_END =
        "A4:2 C5:2 E5:2  D5:3 C5:1 B4:2  C5:2 A4:2 E4:2  A4:6 " +
        "F4:2 A4:2 C5:2  B4:3 A4:1 G#4:2 A4:2 B4:2 C5:2  B4:6 " +
        "A4:2 C5:2 E5:2  G5:3 F5:1 E5:2  D5:2 F5:2 A4:2  C5:4 B4:2 " +
        "A4:2 E5:2 D5:2  C#5:3 B4:1 G#4:2 A4:2 C#5:2 E5:2  A5:6";
    public const string LULL_END_PROG = "Am G Am Am F E Am E Am C Dm Am A E A A";

    const string GARDEN =
        "C5:2 E5:2 G5:2 E5:2  F5:2 A5:2 G5:4  E5:2 D5:2 C5:2 D5:2  E5:4 G4:4 " +
        "A4:2 C5:2 F5:2 E5:2  D5:2 C5:2 B4:2 G4:2  C5:2 E5:2 D5:2 B4:2  C5:8";
    const string GARDEN_PROG = "C F Am C F G G C";

    const string NEON =
        "A4:3 C5:3 E5:2  D5:3 C5:3 B4:2  C5:3 A4:3 E4:2  A4:8 " +
        "F4:3 A4:3 C5:2  B4:3 A4:3 G#4:2  A4:4 B4:2 C5:2  E5:8";
    const string NEON_PROG = "Am G Am Am F E Am E";

    const string ARCH = "A4:8 E5:8 D5:8 C5:8 B4:8 G#4:8 A4:16 F4:8 C5:8 B4:8 E4:8 A4:16 -:16";
    const string ARCH_PROG = "Am Am Dm Am E E Am Am F F E E Am Am Am Am";

    const string BATTLE =
        "D5:2 D5:1 A4:1 D5:2 F5:2  E5:2 D5:2 C5:2 A4:2  A#4:2 A#4:1 F4:1 A#4:2 D5:2  C5:4 A4:4 " +
        "D5:2 D5:1 A4:1 D5:2 F5:2  G5:2 F5:2 E5:2 C5:2  D5:2 E5:2 F5:2 E5:2  D5:8";
    const string BATTLE_PROG = "Dm C A# F Dm Gm A Dm";

    const string BOSS =
        "E5:1 E5:1 -:1 E5:1 G5:2 E5:2  D5:2 B4:2 D5:2 E5:2  C5:1 C5:1 -:1 C5:1 E5:2 C5:2  B4:4 D#5:4 " +
        "E5:2 G5:2 B5:2 A5:2  G5:2 F#5:2 E5:2 D5:2  C5:2 B4:2 A4:2 G4:2  F#4:4 B4:4";
    const string BOSS_PROG = "Em Em C B Em D Am B";

    const string ECHO =
        "E5:2 G5:2 C6:2  B5:3 A5:1 G5:2  A5:2 F5:2 D5:2  G5:6 " +
        "E5:2 G5:2 C6:2  D6:3 C6:1 B5:2  C6:6  -:6";
    const string ECHO_PROG = "C G F G C G C C";

    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static AudioClip Get(string key)
    {
        if (cache.TryGetValue(key, out var c)) return c;
        c = Build(key);
        if (c != null) cache[key] = c;
        return c;
    }

    static V Mel(W w, string seq, float vol, int oct = 0, float vib = 0.15f) =>
        new V { wave = w, seq = seq, vol = vol, oct = oct, vib = vib, atk = 0.02f, dec = 0.2f, sus = 0.7f, rel = 0.15f };

    static V Acc(W w, string prog, string pat, int oct, float vol, float gate = 0.9f) =>
        new V { wave = w, seq = Synth.Prog(prog, pat, oct), vol = vol, gate = gate, atk = 0.01f, dec = 0.1f, sus = 0.6f, rel = 0.08f };

    static V Pad(string prog, string pat, int oct, float vol) =>
        new V { wave = W.Sine, seq = Synth.Prog(prog, pat, oct), vol = vol, atk = 0.4f, dec = 0.5f, sus = 0.8f, rel = 0.6f, gate = 1f, detune = 0.08f };

    static V Drums(string pat, float vol) => new V { wave = W.Drums, seq = pat, vol = vol };

    static AudioClip Build(string key)
    {
        switch (key)
        {
            case "title":
                return Synth.Track(key, 70, 16, 3, 0.43f, 0.35f, 0.35f,
                    Mel(W.Box, LULL, 0.35f, 1, 0),
                    Acc(W.Sine, LULL_PROG, "1:2 5:2 8:2", -1, 0.10f),
                    Pad(LULL_PROG, "1+3+5:6", 3, 0.05f));

            case "dock": // Тихий Причал — туманный, одинокий
                return Synth.Track(key, 62, 16, 3, 0.48f, 0.45f, 0.45f,
                    Mel(W.Pulse, LULL, 0.12f, 0, 0.25f),
                    Acc(W.Tri, LULL_PROG, "1:6", 2, 0.16f),
                    Pad(LULL_PROG, "1+5:3 3+8:3", 3, 0.06f),
                    Acc(W.Box, LULL_PROG, "-:2 x:2 9:2", 3, 0.06f));

            case "garden": // Забытый Сад — тёплая, но выцветшая надежда
                return Synth.Track(key, 100, 8, 4, 0.3f, 0.3f, 0.25f,
                    Mel(W.Pluck, GARDEN, 0.30f),
                    Mel(W.Tri, GARDEN, 0.08f, -1, 0.1f),
                    Acc(W.Tri, GARDEN_PROG, "1:2 5:2 8:2 5:2", 2, 0.16f),
                    Acc(W.Box, GARDEN_PROG, "3:1 5:1 8:1 5:1 9:1 5:1 8:1 5:1", 4, 0.05f),
                    Drums("h...h...h...h.h.", 0.15f));

            case "neon": // Неоновый Архипелаг — ностальгический синтпоп
                return Synth.Track(key, 118, 8, 4, 0.25f, 0.3f, 0.2f,
                    Mel(W.Sq, NEON, 0.10f, 0, 0.2f),
                    Mel(W.Saw, NEON, 0.05f, -1, 0.1f),
                    Acc(W.Tri, NEON_PROG, "1:1 1:1 8:1 1:1 1:1 8:1 1:1 5:1", 2, 0.2f, 0.7f),
                    Acc(W.Pulse, NEON_PROG, "1:1 3:1 5:1 8:1 5:1 3:1 5:1 8:1", 4, 0.05f, 0.5f),
                    Drums("k.h.s.h.k.k.s.hh", 0.35f));

            case "archive": // Бесконечный Архив — монохромная тишина
                return Synth.Track(key, 56, 16, 4, 0.8f, 0.5f, 0.5f,
                    Mel(W.Sine, ARCH, 0.22f, 0, 0.1f),
                    Pad(ARCH_PROG, "1+5:8", 2, 0.10f),
                    Acc(W.Box, ARCH_PROG, "-:4 x:4", 4, 0.05f));

            case "battle": // Встреча с Забытым
                return Synth.Track(key, 140, 8, 4, 0.2f, 0.2f, 0.15f,
                    Mel(W.Sq, BATTLE, 0.11f, 0, 0.1f),
                    Acc(W.Tri, BATTLE_PROG, "1:1 8:1 1:1 8:1 1:1 8:1 1:1 8:1", 2, 0.22f, 0.6f),
                    Acc(W.Pulse, BATTLE_PROG, "1:1 3:1 5:1 3:1 8:1 5:1 3:1 5:1", 4, 0.04f, 0.5f),
                    Drums("k.h.s.hkk.h.s.hh", 0.35f));

            case "boss": // Стадии горя
                return Synth.Track(key, 150, 8, 4, 0.2f, 0.25f, 0.15f,
                    Mel(W.Sq, BOSS, 0.11f, 0, 0.12f),
                    Mel(W.Saw, BOSS, 0.05f, -1, 0),
                    Acc(W.Tri, BOSS_PROG, "1:1 1:1 8:1 1:1 1:1 8:1 5:1 8:1", 2, 0.24f, 0.6f),
                    Acc(W.Pulse, BOSS_PROG, "1:1 5:1 8:1 9:1 x:1 9:1 8:1 5:1", 4, 0.04f, 0.5f),
                    Drums("k.hsk.hsk.hsk.ss", 0.38f));

            case "oculus": // Та же колыбельная, но из-под воды
                return Synth.Track(key, 46, 16, 3, 0.6f, 0.55f, 0.5f,
                    new V { wave = W.Tri, seq = LULL, vol = 0.22f, oct = -1, detune = 0.35f, vib = 0.4f, atk = 0.2f, dec = 0.3f, sus = 0.7f, rel = 0.4f },
                    Pad(LULL_PROG, "1+5:6", 1, 0.09f),
                    Drums("k...........k.........................", 0.25f));

            case "echo": // Мадам Эхо — обольстительный вальс
                return Synth.Track(key, 84, 8, 3, 0.36f, 0.35f, 0.3f,
                    Mel(W.Box, ECHO, 0.25f),
                    Mel(W.Sine, ECHO, 0.08f, -1, 0.3f),
                    Acc(W.Tri, ECHO_PROG, "1:2 3+5:2 3+5:2", 2, 0.12f),
                    Pad(ECHO_PROG, "1+3+5:6", 3, 0.04f));

            case "illusion": // Праздник, который никогда не закончится
                return Synth.Track(key, 124, 8, 4, 0.15f, 0.2f, 0.15f,
                    Mel(W.Pulse, GARDEN, 0.10f, 1, 0.1f),
                    Mel(W.Box, GARDEN, 0.14f),
                    Acc(W.Tri, GARDEN_PROG, "1:1 5:1 8:1 5:1 1:1 5:1 8:1 5:1", 2, 0.2f, 0.6f),
                    Drums("k.h.s.h.k.h.s.hh", 0.3f));

            case "ending": // Истинный финал — принятие
                return Synth.Track(key, 72, 16, 3, 0.42f, 0.4f, 0.35f,
                    Mel(W.Tri, LULL_END, 0.22f, 0, 0.2f),
                    Mel(W.Box, LULL_END, 0.12f, 1, 0),
                    Acc(W.Sine, LULL_END_PROG, "1:2 5:2 8:2", 1, 0.12f),
                    Pad(LULL_END_PROG, "1+3+5:6", 3, 0.06f));

            case "gameover":
                return Synth.Track(key, 60, 4, 3, 0.5f, 0.4f, 0.4f,
                    Mel(W.Box, "A4:2 C5:2 E5:2 D5:3 C5:1 B4:2 C5:2 A4:2 E4:2 A4:6", 0.3f, 1, 0),
                    Pad("Am G Am Am", "1+5:6", 3, 0.06f));

            case "hospital": return Hospital();

            // ---- Кассеты ----
            case "tape1": return Tape1();
            case "tape2": return Tape2();
            case "tape3": return Tape3();
            case "tape4": return Tape4();

            // ---- SFX ----
            case "blip": return Synth.Sfx(key, 0.045f, t => Synth.Sq(t, 520) * 0.18f * (1 - t / 0.045f));
            case "select": return Synth.Sfx(key, 0.06f, t => Synth.Sq(t, 880) * 0.15f * (1 - t / 0.06f));
            case "confirm": return Synth.Sfx(key, 0.14f, t => Synth.Sq(t, t < 0.05f ? 660 : 990) * 0.16f * (1 - t / 0.14f));
            case "hurt":
                return Synth.Sfx(key, 0.3f, t => (Synth.Sq(t, 220 - t * 400) * 0.5f + Synth.Noise() * 0.5f) * 0.3f * (1 - t / 0.3f));
            case "heal":
                return Synth.Sfx(key, 0.5f, t => Synth.Sin(t, t < 0.12f ? 523 : t < 0.24f ? 659 : t < 0.36f ? 784 : 1046) * 0.25f * (1 - t / 0.5f));
            case "erase":
                return Synth.Sfx(key, 1.4f, t => Synth.Noise() * 0.35f * (1 - t / 1.4f) * (0.6f + 0.4f * Synth.Sq(t, 30)));
            case "slash":
                return Synth.Sfx(key, 0.25f, t => Synth.Noise() * 0.4f * Mathf.Exp(-t * 14f));
            case "spare":
                return Synth.Sfx(key, 1.0f, t => Synth.Sin(t, 880 * Mathf.Pow(2, Mathf.Floor(t * 8) % 4 / 4f)) * 0.2f * (1 - t));
            case "encounter":
                return Synth.Sfx(key, 0.45f, t => Synth.Sq(t, t < 0.15f ? 440 : t < 0.3f ? 660 : 880) * 0.15f);
            case "save":
                return Synth.Sfx(key, 1.2f, t => (Synth.Sin(t, 1318) * 0.5f + Synth.Sin(t, 1760) * 0.3f) * 0.25f * Mathf.Exp(-t * 3f));
            case "click":
                return Synth.Sfx(key, 0.08f, t => Synth.Noise() * 0.5f * Mathf.Exp(-t * 60f));
            case "shatter":
                return Synth.Sfx(key, 1.5f, t => (Synth.Noise() * 0.4f + Synth.Sin(t, 1200 + Synth.Noise() * 300) * 0.2f) * Mathf.Exp(-t * 2.5f));
            case "flatline":
                return Synth.Sfx(key, 9f, t => Synth.Sin(t, 1000) * 0.18f * Mathf.Min(1, t * 20f) * Mathf.Min(1, (9f - t) * 0.3f));
            case "beep":
                return Synth.Sfx(key, 0.15f, t => Synth.Sin(t, 1000) * 0.2f * Mathf.Min(1, (0.15f - t) * 40f));
            case "rumble":
                return Synth.Sfx(key, 2f, t => (Synth.Noise() * 0.3f + Synth.Sin(t, 40) * 0.5f) * Mathf.Min(1, t * 3) * Mathf.Min(1, (2f - t) * 2f));
        }
        return null;
    }

    // Звук старой плёнки поверх всего
    static float Hiss(float t) => Synth.Noise() * 0.03f + Mathf.Sin(t * 2 * Mathf.PI * 0.5f) * 0.004f;

    static AudioClip Tape1() // монитор сердца
    {
        return Synth.Sfx("tape1", 8f, t =>
        {
            float ph = t % 0.9f;
            float beep = ph < 0.12f ? Synth.Sin(t, 1000) * 0.2f * Mathf.Min(1, (0.12f - ph) * 40f) : 0;
            float vent = Mathf.Sin(t * 2 * Mathf.PI / 4f) > 0.6f ? Synth.Noise() * 0.04f : 0;
            return beep + vent + Hiss(t);
        });
    }

    static AudioClip Tape2() // мама напевает колыбельную
    {
        var buf = Synth.RenderVoices(66, 8 * 3 * 60f / 66, new[]
        {
            new V { wave = W.Box, seq = LULL, vol = 0.3f, oct = 1 },
            new V { wave = W.Sine, seq = LULL, vol = 0.12f, vib = 0.3f, atk = 0.05f, dec = 0.2f, sus = 0.8f, rel = 0.2f }
        });
        for (int i = 0; i < buf.Length; i++) buf[i] = buf[i] * 0.8f + Hiss(i / (float)Synth.SR);
        return Synth.ToClip("tape2", buf);
    }

    static AudioClip Tape3() // дождь, радио, визг тормозов, удар
    {
        return Synth.Sfx("tape3", 7f, t =>
        {
            float rain = Synth.Noise() * 0.06f;
            float radio = t < 3.5f ? Synth.Sq(t, 330 * (Mathf.Floor(t * 4) % 3 == 0 ? 1f : 1.25f)) * 0.03f : 0;
            float screech = 0;
            if (t > 3.3f && t < 4.8f)
            {
                float k = (t - 3.3f) / 1.5f;
                screech = (Synth.Sin(t, 2400 - k * 900 + Mathf.Sin(t * 90) * 60) * 0.25f + Synth.Noise() * 0.1f) * (1 - k * 0.3f);
            }
            float crash = t > 4.8f && t < 6.2f ? Synth.Noise() * 0.6f * Mathf.Exp(-(t - 4.8f) * 3f) : 0;
            float silence = t > 6.2f ? 0 : 1;
            return (rain + radio) * silence + screech + crash + Hiss(t);
        });
    }

    static AudioClip Tape4() // брат играет колыбельную на гитаре — последнее воспоминание
    {
        var buf = Synth.RenderVoices(70, 16 * 3 * 60f / 70, new[]
        {
            new V { wave = W.Pluck, seq = LULL_END, vol = 0.3f },
            new V { wave = W.Pluck, seq = Synth.Prog(LULL_END_PROG, "1:2 5:2 8:2", 2), vol = 0.15f }
        });
        for (int i = 0; i < buf.Length; i++) buf[i] = buf[i] * 0.8f + Hiss(i / (float)Synth.SR);
        return Synth.ToClip("tape4", buf);
    }

    static AudioClip Hospital()
    {
        float len = 12f;
        int n = (int)(len * Synth.SR);
        var buf = new float[n];
        float lp = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Synth.SR;
            lp += (Synth.Noise() - lp) * 0.08f; // дождь — отфильтрованный шум
            float ph = t % 1.2f;
            float beep = ph < 0.1f ? Synth.Sin(t, 880) * 0.07f * Mathf.Min(1, (0.1f - ph) * 40f) : 0;
            buf[i] = lp * 0.35f + beep;
        }
        return Synth.ToClip("hospital", buf, 1f);
    }
}
