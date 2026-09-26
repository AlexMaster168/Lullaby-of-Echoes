using System;
using System.Collections.Generic;
using UnityEngine;

// Весь пиксель-арт игры рисуется кодом.
public static class Art
{
    static Color32 C(string hex) => Pix.C(hex);
    static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    static readonly Color32 Ink = new Color32(26, 20, 24, 255);

    public static Canvas Draw(string key)
    {
        // Кадр анимации: "e_moth@2"
        int f = 0;
        int at = key.IndexOf('@');
        if (at >= 0) { f = (key[at + 1] - '0') & 3; key = key.Substring(0, at); }
        if (key.StartsWith("noa_")) return Noa(key[4], key[5] - '0');
        if (key.StartsWith("skrip")) return Skrip(key[5] - '0');
        if (key.StartsWith("static")) return Static(key[6] - '0');
        if (key.StartsWith("save")) return Save(key[4] - '0');
        if (key.StartsWith("t_")) return Tile(key);
        switch (key)
        {
            case "echo": return Echo(true);
            case "echo_noface": return Echo(false);
            case "oculus": return Oculus(false);
            case "oculus_face": return Oculus(true);
            case "npc_bg": return Ghost(C("8a90a0"), 0);
            case "npc_bg_noface": return Ghost(C("8a90a0"), 0, false);
            case "npc_garden": return Ghost(C("6a9a6a"), 1);
            case "npc_neon": return Ghost(C("c070c0"), 2);
            case "npc_arch": return Ghost(C("c0c0c0"), 3);
            case "tape": return TapeIcon();
            case "exit": return Exit();
            case "e_letter": return Letter(f);
            case "e_clock": return Clock(f);
            case "e_keeper": return Keeper(f);
            case "e_tape": return Cassette(f);
            case "e_hope": return Hope(f);
            case "e_thorn": return Thorn(f);
            case "e_moth": return Moth(f);
            case "e_token": return Token(f);
            case "e_merchant": return Merchant(f);
            case "e_page": return Page(f);
            case "e_oculus": return OculusBig(f);
            case "soul": return Soul();
            case "hospital0": return Hospital(false);
            case "hospital1": return Hospital(true);
        }
        if (key.StartsWith("b_")) return Bullet(key);
        return null;
    }

    // ================= ПЕРСОНАЖИ =================

    // f: 0 — стоит, 1/2 — шаги, 3 — моргает
    static Canvas Noa(char dir, int f)
    {
        var c = new Canvas(16, 24);
        Color32 hair = C("5a3a2a"), hairHi = C("7a5238"), skin = C("f2c9a0"), jacket = C("b5553c"), jshade = C("8a3b2a"),
            pants = C("3b4a6b"), shoe = C("2a2020"), eye = C("201818"), dev = C("9aa0a8"), zip = C("d8b060"), blush = C("e8a090");
        bool blink = f == 3;
        if (blink) f = 0;

        // Ноги
        if (dir == 's')
        {
            if (f == 0) { c.Rect(6, 19, 2, 3, pants); c.Rect(8, 19, 2, 3, pants); c.Rect(6, 22, 4, 1, shoe); }
            else if (f == 1) { c.Rect(5, 19, 2, 3, pants); c.Rect(9, 19, 2, 2, pants); c.Rect(4, 22, 3, 1, shoe); c.Rect(9, 21, 3, 1, shoe); }
            else { c.Rect(9, 19, 2, 3, pants); c.Rect(5, 19, 2, 2, pants); c.Rect(9, 22, 3, 1, shoe); c.Rect(4, 21, 3, 1, shoe); }
        }
        else
        {
            int l = f == 1 ? 1 : 0, r = f == 2 ? 1 : 0;
            c.Rect(5, 19, 2, 3 - l, pants); c.Rect(9, 19, 2, 3 - r, pants);
            c.Rect(5, 22 - l, 2, 1, shoe); c.Rect(9, 22 - r, 2, 1, shoe);
        }
        int sw = f == 1 ? 1 : f == 2 ? -1 : 0; // размах рукавов

        // Огромная куртка (куртка старшего брата)
        if (dir == 's')
        {
            c.Rect(4, 10, 8, 10, jacket);
            c.Rect(3, 14, 10, 6, jacket);
            c.Rect(7 + sw, 11, 3, 8, jshade);
            c.Rect(10 + sw, 15, 3, 3, dev); c.Set(11 + sw, 16, C("e08030"));
        }
        else
        {
            c.Rect(3, 10, 10, 10, jacket);
            c.Rect(2, 14, 12, 6, jacket);
            c.Rect(1, 11 + sw, 2, 8, jshade); c.Rect(13, 11 - sw, 2, 8, jshade);
            c.Rect(5, 10, 6, 2, jshade);
            if (dir == 'd')
            {
                c.Line(8, 12, 8, 19, zip);
                c.Rect(9, 15, 3, 3, dev); c.Set(10, 16, C("e08030"));
            }
            else c.Line(8, 12, 8, 19, jshade);
        }

        // Голова
        c.Ellipse(8, 6, 5.5f, 5.5f, hair);
        c.Line(5, 2, 9, 1, hairHi);
        if (dir == 'd')
        {
            c.Ellipse(8, 7.5f, 4.2f, 3.8f, skin);
            c.Rect(4, 3, 8, 2, hair);
            c.Set(5, 5, hair); c.Set(11, 5, hair); c.Set(8, 5, hair);
            if (blink) { c.Rect(5, 8, 1, 1, eye); c.Rect(10, 8, 1, 1, eye); }
            else { c.Rect(5, 7, 1, 2, eye); c.Rect(10, 7, 1, 2, eye); }
            c.Set(4, 9, blush); c.Set(11, 9, blush);
        }
        else if (dir == 's')
        {
            c.Ellipse(10, 7.5f, 3.4f, 3.6f, skin);
            c.Rect(7, 3, 7, 2, hair);
            if (blink) c.Rect(11, 8, 1, 1, eye); else c.Rect(11, 7, 1, 2, eye);
            c.Set(12, 9, blush);
        }
        else
        {
            c.Line(5, 4, 7, 5, C("6e4a36"));
            c.Line(9, 3, 11, 5, C("6e4a36"));
        }
        c.Shade();
        c.Outline(Ink);
        return c;
    }

    static Canvas Skrip(int f)
    {
        var c = new Canvas(18, 16);
        Func<int, int, Color32> paper = (x, y) => (y % 3 == 1 && x % 3 != 0) ? C("b8b4a8") : C("ece8dc");
        Color32 fold = C("c8c4b8");
        // крылья
        if (f == 0) { c.Tri(5, 9, 10, 9, 3, 1, paper); c.Tri(7, 9, 12, 9, 14, 1, paper); }
        else { c.Tri(5, 9, 10, 9, 1, 12, paper); c.Tri(7, 9, 12, 9, 16, 12, paper); }
        // тело
        c.Tri(3, 9, 13, 9, 8, 13, paper);
        c.Line(8, 9, 8, 13, fold);
        // хвост и шея
        c.Line(4, 10, 1, 5, C("d8d4c8")); c.Line(3, 10, 0, 6, C("d8d4c8"));
        c.Line(12, 10, 15, 4, C("d8d4c8")); c.Line(13, 10, 16, 4, C("d8d4c8"));
        c.Rect(15, 2, 2, 2, C("ece8dc"));
        c.Set(17, 3, C("c05030"));
        c.Shade(1.08f, 0.8f);
        c.Set(15, 2, Ink);
        c.Outline(C("3a3630"));
        return c;
    }

    static Canvas Echo(bool face)
    {
        var c = new Canvas(26, 42);
        Color32 red = C("8e1b2a"), red2 = C("6a1220"), gold = C("d4a84a"), pale = C("f0e6e0"), hair = C("2a1a2a");
        Func<int, int, Color32> folds = (x, y) => ((x / 2) % 2 == 0) ? red : red2;
        c.Rect(7, 13, 12, 27, folds);
        c.Tri(7, 15, 7, 40, 1, 40, folds);
        c.Tri(18, 15, 18, 40, 24, 40, folds);
        c.Ellipse(13, 14, 7, 3, red);
        c.Rect(1, 39, 24, 2, gold);
        c.Rect(7, 25, 12, 1, gold);
        c.Set(6, 25, gold); c.Set(19, 25, gold);
        c.Rect(11, 27, 4, 2, pale);
        // лицо и причёска
        c.Rect(12, 10, 2, 3, pale);
        c.Ellipse(13, 7, 3.8f, 4.6f, pale);
        c.Ellipse(13, 3.2f, 4.8f, 2.8f, hair);
        c.Circle(13, 0.9f, 1.8f, hair);
        c.Rect(8, 4, 2, 5, hair); c.Rect(16, 4, 2, 5, hair);
        c.Shade();
        if (face)
        {
            c.Line(10, 7, 11, 7, Ink); c.Line(14, 7, 15, 7, Ink);
            c.Rect(12, 9, 2, 1, C("c02040"));
        }
        c.Outline(C("1a0a10"));
        return c;
    }

    static Canvas Oculus(bool face)
    {
        var c = new Canvas(22, 34);
        Color32 body = C("15121c"), edge = C("2a2438"), blank = C("3a3448");
        c.Tri(11, 10, 1, 33, 21, 33, body);
        c.Rect(2, 14, 2, 16, body); c.Rect(18, 14, 2, 16, body);
        c.Line(2, 30, 1, 33, body); c.Line(3, 30, 3, 33, body); c.Line(19, 30, 20, 33, body); c.Line(18, 30, 18, 33, body);
        c.Ellipse(11, 9, 7, 8, body);
        c.Ellipse(11, 9, 6, 7, edge);
        if (!face)
        {
            c.Ellipse(11, 10, 4, 5, blank);
        }
        else
        {
            // Настоящее лицо Ноа: заплаканное, с пластырем
            c.Ellipse(11, 10, 4, 5, C("f2c9a0"));
            c.Rect(7, 6, 8, 2, C("5a3a2a"));
            c.Line(8, 10, 9, 10, Ink); c.Line(13, 10, 14, 10, Ink);
            c.Set(8, 11, C("80b0ff")); c.Set(8, 12, C("80b0ff")); c.Set(14, 11, C("80b0ff"));
            c.Rect(12, 12, 3, 1, C("f8f0e0"));
        }
        // размытые края
        var r = new System.Random(5);
        for (int i = 0; i < 40; i++) c.Set(r.Next(0, 22), 28 + r.Next(0, 6), Clear);
        c.Shade(1.3f, 0.8f);
        c.Outline(C("4a3a6a"));
        return c;
    }

    // Боевая форма Окулюса: крупнее, щупальца шевелятся, пустота на лице мерцает
    static Canvas OculusBig(int f)
    {
        var c = new Canvas(46, 64);
        Color32 body = C("120f18"), edge = C("241e30"), blank = C("35304a"), shimmer = C("4c4666"), claw = C("3a3450");
        // щупальца снизу: изгибаются и сужаются, каждое в своей фазе
        for (int i = 0; i < 6; i++)
        {
            float x = 8 + i * 6f;
            float px = x, py = 52;
            for (int k = 1; k <= 6; k++)
            {
                float nx = x + Mathf.Sin(f * 1.5708f + i * 1.7f + k * 0.8f) * (k * 0.7f);
                float ny = 52 + k * 2;
                c.Line(px, py, nx, ny, body);
                if (k < 4) c.Line(px + 1, py, nx + 1, ny, body);
                px = nx; py = ny;
            }
        }
        // плащ-тень
        c.Tri(23, 12, 3, 58, 43, 58, body);
        // длинные руки тянутся вниз
        float sway = f % 2 == 0 ? 0 : 1.5f;
        for (int k = 0; k < 3; k++)
        {
            c.Line(13 + k, 22, 3 + k - sway, 50, body);
            c.Line(31 - k, 22, 41 - k + sway, 50, body);
        }
        c.Line(3 - sway, 50, 0, 55, claw); c.Line(4 - sway, 50, 3, 56, claw); c.Line(5 - sway, 50, 6, 55, claw);
        c.Line(41 + sway, 50, 45, 55, claw); c.Line(40 + sway, 50, 42, 56, claw); c.Line(39 + sway, 50, 39, 55, claw);
        // капюшон
        c.Ellipse(23, 14, 11, 13, body);
        c.Ellipse(23, 15, 9, 11, edge);
        // пустое лицо
        c.Ellipse(23, 17, 6, 8, blank);
        var r = new System.Random(f * 13 + 7);
        for (int i = 0; i < 14; i++)
        {
            int x = r.Next(18, 29), y = r.Next(10, 25);
            float dx = (x - 23) / 6f, dy = (y - 17) / 8f;
            if (dx * dx + dy * dy < 0.8f) c.Set(x, y, shimmer);
        }
        // рваные края снизу
        var rr = new System.Random(11 + f);
        for (int i = 0; i < 30; i++) c.Set(rr.Next(4, 43), 52 + rr.Next(0, 4), Clear);
        c.Shade(1.35f, 0.8f);
        c.Outline(C("5a4a8a"));
        return c;
    }

    static Canvas Ghost(Color32 col, int hat, bool faceOn = true)
    {
        var c = new Canvas(16, 24);
        Color32 dark = Pix.Shade(col, 0.7f);
        c.Rect(4, 11, 8, 11, col);
        c.Tri(4, 14, 4, 22, 2, 22, col); c.Tri(11, 14, 11, 22, 13, 22, col);
        c.Circle(8, 7, 4.5f, col);
        switch (hat)
        {
            case 0: c.Rect(3, 3, 10, 1, dark); c.Rect(5, 0, 6, 3, dark); break;           // шляпа
            case 1: c.Tri(2, 4, 14, 4, 8, 0, C("c8a860")); break;                        // соломенная
            case 2: c.Rect(4, 5, 8, 2, C("40e0ff")); break;                              // визор
        }
        c.Shade();
        if (faceOn) { c.Set(6, 7, Ink); c.Set(10, 7, Ink); }
        if (hat == 3) { c.Rect(5, 5, 2, 2, Ink); c.Rect(9, 5, 2, 2, Ink); c.Line(7, 6, 8, 6, Ink); } // очки
        var r = new System.Random(hat * 7 + 1);
        for (int i = 0; i < 10; i++) c.Set(r.Next(2, 14), 21 + r.Next(0, 3), Clear);
        c.Outline(Pix.Shade(col, 0.4f));
        return c;
    }

    // ================= ЗАБЫТЫЕ (4 кадра анимации) =================

    static Canvas Letter(int f)
    {
        var c = new Canvas(34, 32);
        Color32 paper = C("efe6d0"), shade = C("d9ceb4");
        int flap = f % 2 == 1 ? 3 : 0;
        c.Rect(4, 9, 26, 17, paper);
        c.Line(4, 25, 17, 16, shade); c.Line(29, 25, 17, 16, shade);
        c.Line(2, 18 - flap / 2, 4, 16, paper); c.Line(31, 18 - flap / 2, 29, 16, paper);
        c.Rect(10, 26, 2, f % 2 == 0 ? 3 : 2, paper); c.Rect(22, 26, 2, f % 2 == 0 ? 2 : 3, paper);
        c.Shade();
        c.Tri(4, 9, 29, 9, 17, 18 - flap, shade);
        c.Circle(17, 17 - flap / 2, 3, C("b02a2a"));
        c.Set(16, 16 - flap / 2, C("e05050"));
        c.Rect(23, 11, 4, 4, C("6a8ac0"));
        if (f == 3) { c.Line(9, 21, 10, 21, Ink); c.Line(24, 21, 25, 21, Ink); }
        else { c.Rect(9, 20, 2, 2, Ink); c.Set(9, 20, C("f8f0e0")); c.Rect(24, 20, 2, 2, Ink); c.Set(24, 20, C("f8f0e0")); }
        c.Set(9, 23 + f % 2, C("70a0e0")); c.Set(9, 24 + f % 2, C("70a0e0"));
        c.Outline(Ink);
        return c;
    }

    static void Hand(Canvas c, float cx, float cy, float deg, float len, Color32 col)
    {
        float a = deg * Mathf.Deg2Rad;
        c.Line(cx, cy, cx + Mathf.Sin(a) * len, cy - Mathf.Cos(a) * len, col);
    }

    static Canvas Clock(int f)
    {
        var c = new Canvas(34, 36);
        Color32 gold = C("b89a4a"), face = C("e8e4d8");
        c.Ring(17, 2.5f, 2.5f, 1, gold);
        c.Rect(15, 4, 4, 3, gold);
        c.Circle(17, 19, 13, gold);
        c.Shade();
        c.Circle(17, 19, 11, face);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6;
            c.Set(Mathf.RoundToInt(17 + Mathf.Cos(a) * 9), Mathf.RoundToInt(19 + Mathf.Sin(a) * 9), C("6a6050"));
        }
        c.Line(8, 13, 12, 17, C("9a9488"));
        // 17:42 — стрелки дрожат, но не могут сдвинуться; секундная пытается бежать
        Hand(c, 17, 19, 171, 6, Ink);
        Hand(c, 17, 19, 252 + (f % 2 == 0 ? -5 : 5), 9, Ink);
        Hand(c, 17, 19, f * 90, 10, C("c03030"));
        if (f == 3) { c.Rect(11, 14, 2, 2, Ink); c.Rect(22, 14, 2, 2, Ink); }
        else { c.Line(11, 15, 13, 15, Ink); c.Line(21, 15, 23, 15, Ink); }
        c.Set(16, 33 + f % 2, C("5080c0")); c.Set(19, 34 - f % 2, C("5080c0")); c.Set(19, 35 - f % 2, C("5080c0")); c.Set(9, 30 + f % 3, C("5080c0"));
        c.Outline(Ink);
        return c;
    }

    static Canvas Keeper(int f)
    {
        var c = new Canvas(44, 52);
        Color32 red = C("b83a3a"), white = C("e8e0d8"), glass = f % 2 == 0 ? C("ffe890") : C("fff4c0"), roof = C("6a1a1a"), hand = C("e8c8a8");
        c.Rect(13, 20, 18, 30, (x, y) => ((y - 20) / 5) % 2 == 0 ? red : white);
        c.Tri(13, 20, 13, 50, 9, 50, red); c.Tri(30, 20, 30, 50, 34, 50, red);
        c.Rect(8, 49, 28, 3, C("3a3a44"));
        c.Rect(19, 40, 6, 10, C("2a1a1a"));
        c.Rect(11, 18, 22, 2, C("3a3a44"));
        c.Rect(14, 8, 16, 10, glass);
        c.Line(18, 8, 18, 17, C("c8b060")); c.Line(25, 8, 25, 17, C("c8b060"));
        c.Tri(12, 8, 32, 8, 22, 0, roof);
        c.Shade();
        // руки закрывают свет — отрицание. Руки дрожат, свет прорывается между пальцами
        int hy = f == 2 ? 1 : 0;
        c.Line(13, 28, 8, 20 + hy, hand); c.Line(30, 28, 36, 20 + hy, hand);
        c.Ellipse(17, 13 + hy, 5, 3.5f, hand); c.Ellipse(27, 13 + hy, 5, 3.5f, hand);
        c.Line(14, 12 + hy, 20, 12 + hy, C("c8a888")); c.Line(24, 12 + hy, 30, 12 + hy, C("c8a888"));
        if (f % 2 == 1) { c.Line(22, 9, 22, 17, C("ffffff")); c.Set(21, 11, C("fff8d0")); c.Set(23, 14, C("fff8d0")); }
        c.Outline(Ink);
        return c;
    }

    static Canvas Cassette(int f)
    {
        var c = new Canvas(38, 34);
        Color32 body = C("3a3a44"), label = C("e0c070"), tape = C("6a4020");
        c.Rect(3, 4, 32, 20, body);
        c.Rect(6, 6, 26, 8, label);
        c.Shade();
        c.Line(8, 9, 29, 9, C("a08040")); c.Line(8, 11, 22, 11, C("a08040"));
        c.Rect(9, 15, 20, 6, C("1a1a20"));
        c.Circle(13, 18, 2.5f, C("f0f0f0")); c.Circle(25, 18, 2.5f, C("f0f0f0"));
        // катушки-зрачки вращаются
        int[] ox = { 0, 1, 0, -1 }, oy = { -1, 0, 1, 0 };
        c.Set(13 + ox[f], 18 + oy[f], Ink); c.Set(25 + ox[f], 18 + oy[f], Ink);
        // зажёванная плёнка шевелится
        int w = f % 2;
        c.Line(12, 24, 8 - w, 27, tape); c.Line(8 - w, 27, 12, 30, tape); c.Line(12, 30, 7 + w, 33, tape);
        c.Line(24, 24, 29 + w, 28, tape); c.Line(29 + w, 28, 26, 31, tape); c.Line(26, 31, 31 - w, 33, tape);
        c.Line(18, 24, 19 + w, 29, tape);
        c.Outline(Ink);
        return c;
    }

    static Canvas Hope(int f)
    {
        var c = new Canvas(34, 42);
        Color32 stem = C("4a7a3a"), petal = C("c890a0"), petal2 = C("a87080");
        int hx = 11 + new[] { 0, 1, 0, -1 }[f];
        c.Line(18, 41, 19, 30, stem); c.Line(19, 30, 17, 22, stem); c.Line(17, 22, hx + 1, 17, stem);
        c.Line(19, 41, 20, 30, stem);
        c.Tri(19, 32, 27, 28 + f % 2, 25, 34, C("5a8a4a")); c.Tri(18, 36, 10, 33 - f % 2, 12, 38, C("5a8a4a"));
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3 + 0.4f;
            c.Ellipse(hx + Mathf.Cos(a) * 5.5f, 16 + Mathf.Sin(a) * 5.5f, 3.5f, 3.5f, i % 2 == 0 ? petal : petal2);
        }
        c.Ellipse(24 + f % 2, 36 + f, 2.5f, 1.5f, petal2); // падающий лепесток
        c.Shade();
        c.Circle(hx, 16, 4, C("2a2a30"));
        c.Circle(hx, 16, 1.6f, C("f0f0f0"));
        c.Set(hx + (f % 2 == 0 ? 1 : -1), 16, Ink);
        c.Outline(Ink);
        return c;
    }

    static Canvas Thorn(int f)
    {
        var c = new Canvas(48, 52);
        Color32 rose = C("c0203a"), rose2 = C("8a1028"), vine = C("2a5a2a"), thorn = C("d8d0a0");
        int ay = new[] { 0, -2, 0, 2 }[f];
        for (int k = 0; k < 3; k++) c.Line(24 + k - 1, 51, 22 + k, 26, vine);
        c.Line(23, 36, 6, 24 + ay, vine); c.Line(23, 37, 6, 25 + ay, vine);
        c.Line(25, 36, 42, 24 - ay, vine); c.Line(25, 37, 42, 25 - ay, vine);
        c.Line(6, 24 + ay, 2, 30 + ay, vine); c.Line(42, 24 - ay, 46, 30 - ay, vine);
        c.Ellipse(24, 15, 14, 12, rose);
        c.Shade();
        int[] tx = { 10, 16, 32, 38 };
        foreach (var x in tx)
        {
            int dy = x < 24 ? ay : -ay;
            c.Set(x, 24 + Math.Abs(x - 24) / 3 - 4 + dy, thorn); c.Set(x, 23 + Math.Abs(x - 24) / 3 - 4 + dy, thorn);
        }
        c.Set(21, 44, thorn); c.Set(20, 44, thorn); c.Set(26, 40, thorn); c.Set(27, 40, thorn);
        c.Ring(24, 15, 10, 1, rose2); c.Ring(24, 15, 6, 1, rose2); c.Ring(24, 15, 3, 1, rose2);
        // злые глаза горят, пасть щёлкает
        c.Line(14, 10, 20, 13, Ink); c.Line(34, 10, 28, 13, Ink);
        var eye = f % 2 == 0 ? C("ffe040") : C("ff9030");
        c.Rect(16, 13, 3, 2, eye); c.Rect(29, 13, 3, 2, eye);
        if (f % 2 == 1)
        {
            c.Rect(17, 19, 15, 3, C("400810"));
            for (int x = 17; x < 32; x += 2) { c.Set(x, 19, thorn); c.Set(x + 1, 21, thorn); }
        }
        else for (int x = 17; x < 32; x += 2) { c.Set(x, 21, Ink); c.Set(x + 1, 20, Ink); }
        c.Outline(Ink);
        return c;
    }

    static Canvas Moth(int f)
    {
        var c = new Canvas(34, 30);
        Color32 pink = C("ff4fd8"), cyan = C("40e0ff");
        float ry = new[] { 9f, 7f, 4f, 7f }[f], ry2 = new[] { 5f, 4f, 3f, 4f }[f];
        c.Ellipse(9, 12, 8, ry, pink); c.Ellipse(25, 12, 8, ry, pink);
        c.Ellipse(10, 22, 5, ry2, cyan); c.Ellipse(24, 22, 5, ry2, cyan);
        c.Shade(1.25f, 0.75f);
        if (ry >= 7)
        {
            c.Circle(9, 11, 3, cyan); c.Circle(25, 11, 3, cyan);
            c.Circle(9, 11, 1.2f, Ink); c.Circle(25, 11, 1.2f, Ink);
        }
        c.Ellipse(17, 16, 2.5f, 10, C("2a1a3a"));
        c.Line(16, 6, 12, 1 + f % 2, C("c0c0ff")); c.Line(18, 6, 22, 1 + f % 2, C("c0c0ff"));
        c.Set(12, 1 + f % 2, C("ffffa0")); c.Set(22, 1 + f % 2, C("ffffa0"));
        c.Outline(C("0a0010"));
        return c;
    }

    static Canvas Token(int f)
    {
        var c = new Canvas(34, 36);
        Color32 gold = C("e0b040"), g2 = C("b08020"), g3 = C("f8d870");
        float rx = new[] { 13f, 9f, 3f, 9f }[f];
        c.Line(10, 28, 8, 34, g2); c.Line(24, 28, 26, 34, g2);
        c.Line(4, 16, 0, 12 + f % 2 * 2, g2); c.Line(30, 16, 34, 12 + f % 2 * 2, g2);
        c.Ellipse(17, 16, rx, 13, g2);
        if (rx > 3) c.Ellipse(17, 16, rx - 2, 11, gold);
        c.Shade(1.3f, 0.75f);
        if (rx > 3) c.Ellipse(17, 16, rx * 0.6f, 8, g3);
        if (rx >= 9)
        {
            float k = rx / 13f;
            c.Rect(Mathf.RoundToInt(17 - 4 * k), 13, 2, 3, Ink); c.Rect(Mathf.RoundToInt(17 + 3 * k), 13, 2, 3, Ink);
            c.Line(17 - 4 * k, 19, 17 - 2 * k, 21, Ink); c.Line(17 - 2 * k, 21, 17 + 2 * k, 21, Ink); c.Line(17 + 2 * k, 21, 17 + 4 * k, 19, Ink);
        }
        c.Outline(Ink);
        return c;
    }

    static Canvas Merchant(int f)
    {
        var c = new Canvas(48, 56);
        Color32 coat = C("5a2a7a"), coat2 = C("40205a"), face = C("e8e0f0"), gold = C("e0b040"), cyan = C("40e0ff");
        int t = new[] { 0, 2, 0, -2 }[f];
        c.Tri(24, 22, 4, 55, 44, 55, (x, y) => ((x / 3) % 2 == 0) ? coat : coat2);
        // четыре руки и весы: чаши качаются
        c.Line(18, 30, 6, 28 + t, coat); c.Line(30, 30, 42, 28 - t, coat);
        c.Line(18, 36, 8, 42, coat); c.Line(30, 36, 40, 42, coat);
        c.Ellipse(24, 18, 8, 7, face);
        c.Shade();
        c.Line(24, 26, 24, 55, cyan);
        c.Circle(6, 28 + t, 1.5f, face); c.Circle(42, 28 - t, 1.5f, face); c.Circle(8, 42, 1.5f, face); c.Circle(40, 42, 1.5f, face);
        c.Line(6, 27 + t, 42, 27 - t, gold);
        c.Line(6, 27 + t, 3, 33 + t, gold); c.Line(6, 27 + t, 9, 33 + t, gold); c.Ellipse(6, 34 + t, 4, 1.5f, gold);
        c.Line(42, 27 - t, 39, 33 - t, gold); c.Line(42, 27 - t, 45, 33 - t, gold); c.Ellipse(42, 34 - t, 4, 1.5f, gold);
        c.Circle(6, 32 + t, 1.5f, C("ff4fd8")); c.Circle(42, 32 - t, 1.5f, cyan);
        c.Ring(27, 17, 2.5f, 1, gold);
        c.Set(20, 17, Ink); c.Set(27, 17, Ink);
        int grin = f == 3 ? 1 : 0;
        c.Line(18 - grin, 21, 30 + grin, 21, Ink); c.Set(17 - grin, 20, Ink); c.Set(31 + grin, 20, Ink);
        int hb = f == 1 ? -1 : 0;
        c.Rect(16, 2 + hb, 16, 10, C("15101f"));
        c.Rect(16, 9 + hb, 16, 2, C("ff4fd8"));
        c.Rect(12, 11, 24, 2, C("15101f"));
        c.Outline(C("0a0010"));
        return c;
    }

    static Canvas Page(int f)
    {
        var c = new Canvas(34, 38);
        Color32 white = C("f4f4f0"), gray = C("c8c8c4");
        int ear = f % 2 == 0 ? 6 : 8;
        c.Rect(6, 3, 22, 31, white);
        c.Rect(9, 34, 2, f % 2 == 0 ? 3 : 2, white); c.Rect(23, 34, 2, f % 2 == 0 ? 2 : 3, white);
        c.Tri(28 - ear, 3, 28, 3, 28, 3 + ear, Clear);
        c.Shade(1.05f, 0.85f);
        c.Tri(28 - ear, 3, 28 - ear, 3 + ear, 28, 3 + ear, gray);
        for (int y = 24; y < 32; y += 3) c.Line(10, y, 23, y, C("e4e4e0"));
        if (f == 3) { c.Line(11, 16, 15, 16, C("808080")); c.Line(19, 16, 23, 16, C("808080")); }
        else { c.Ring(13, 16, 3, 1, C("808080")); c.Ring(21, 16, 3, 1, C("808080")); }
        c.Line(15, 22, 19, 22, C("a0a0a0"));
        c.Outline(C("505050"));
        return c;
    }

    static Canvas Static(int v)
    {
        var c = new Canvas(24, 24);
        var r = new System.Random(v * 31 + 3);
        for (int y = 0; y < 24; y++)
            for (int x = 0; x < 24; x++)
            {
                float dx = (x - 12) / 11f, dy = (y - 13) / 10f;
                if (dx * dx + dy * dy > 1 || r.NextDouble() < 0.35) continue;
                byte g = (byte)r.Next(40, 200);
                c.Set(x, y, new Color32(g, g, g, 255));
            }
        return c;
    }

    // ================= ПРЕДМЕТЫ =================

    static Canvas Save(int f)
    {
        var c = new Canvas(13, 13);
        Color32 y = C("ffe860"), w = C("fffff0");
        int s = f == 0 ? 6 : 5;
        c.Line(6, 6 - s, 6, 6 + s, y); c.Line(6 - s, 6, 6 + s, 6, y);
        int d = f == 0 ? 2 : 3;
        c.Line(6 - d, 6 - d, 6 + d, 6 + d, y); c.Line(6 - d, 6 + d, 6 + d, 6 - d, y);
        c.Rect(5, 5, 3, 3, w);
        return c;
    }

    static Canvas TapeIcon()
    {
        var c = new Canvas(14, 10);
        c.Rect(0, 0, 14, 10, C("2a2a34"));
        c.Rect(2, 1, 10, 3, C("f0e0a0"));
        c.Circle(4.5f, 6.5f, 1.6f, C("f0f0f0")); c.Circle(9.5f, 6.5f, 1.6f, C("f0f0f0"));
        return c;
    }

    static Canvas Exit()
    {
        var c = new Canvas(16, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = (x - 7.5f) / 8f, dy = (y - 18f) / 16f;
                float d = dx * dx + dy * dy;
                if (d > 1) continue;
                byte a = (byte)(255 * (1 - d));
                c.Set(x, y, new Color32(255, 250, 220, a));
            }
        return c;
    }

    static Canvas Soul()
    {
        var c = new Canvas(8, 7);
        var pal = new Dictionary<char, Color32> { { '#', C("ff2040") }, { 'w', C("ffa0b0") } };
        c.Art(0, 0, new[]
        {
            ".##.##.",
            "#w#####",
            "#######",
            "#######",
            ".#####.",
            "..###..",
            "...#...",
        }, pal);
        return c;
    }

    static Canvas Bullet(string key)
    {
        Canvas c;
        switch (key)
        {
            case "b_tear":
                c = new Canvas(5, 9);
                c.Circle(2.5f, 6, 2.4f, C("7aa8ff")); c.Tri(0.5f, 5.5f, 4.5f, 5.5f, 2.5f, 0, C("7aa8ff")); c.Set(1, 5, C("d8e8ff"));
                return c;
            case "b_eye":
                c = new Canvas(13, 9);
                c.Ellipse(6.5f, 4.5f, 6.5f, 4.2f, C("e8e0f0")); c.Circle(6.5f, 4.5f, 2.6f, C("3a2060")); c.Circle(6.5f, 4.5f, 1.1f, C("ff3050"));
                c.Outline(C("15101f"));
                return c;
            case "b_letter":
                c = new Canvas(9, 7);
                c.Rect(0, 0, 9, 7, C("efe6d0")); c.Line(0, 0, 4, 3, C("a09070")); c.Line(8, 0, 4, 3, C("a09070"));
                return c;
            case "b_note":
                c = new Canvas(6, 8);
                c.Art(0, 0, new[] { "..###", "..#.#", "..#..", "..#..", ".##..", "###..", ".#..." },
                    new Dictionary<char, Color32> { { '#', C("fff0a0") } });
                return c;
            case "b_thorn":
                c = new Canvas(7, 9);
                c.Tri(0, 9, 7, 9, 3.5f, 0, C("5a8a3a")); c.Line(3, 1, 3, 4, C("d8d0a0"));
                return c;
            case "b_drop":
                c = new Canvas(6, 8);
                c.Circle(3, 5, 2.6f, C("60a0ff")); c.Tri(1, 4, 5, 4, 3, 0, C("60a0ff")); c.Set(2, 4, C("d0e8ff"));
                return c;
            case "b_petal":
                c = new Canvas(6, 5);
                c.Ellipse(3, 2.5f, 3, 2, C("e0a0b8"));
                return c;
            case "b_tape":
                c = new Canvas(10, 3);
                c.Rect(0, 0, 10, 3, C("6a4020")); c.Line(0, 1, 9, 1, C("8a5a30"));
                return c;
            case "b_moth":
                c = new Canvas(8, 6);
                c.Ellipse(2, 2, 2, 2, C("ff4fd8")); c.Ellipse(6, 2, 2, 2, C("ff4fd8")); c.Rect(3, 1, 2, 5, C("40e0ff"));
                return c;
            case "b_coin":
                c = new Canvas(7, 7);
                c.Circle(3.5f, 3.5f, 3.5f, C("e0b040")); c.Circle(3.5f, 3.5f, 1.8f, C("f8d870"));
                return c;
            case "b_dark":
                c = new Canvas(9, 9);
                c.Circle(4.5f, 4.5f, 4.5f, C("8a60c0")); c.Circle(4.5f, 4.5f, 3.2f, C("15101f"));
                return c;
            case "b_light":
                c = new Canvas(8, 8);
                c.Circle(4, 4, 4, C("fff0a0")); c.Circle(4, 4, 2.4f, C("ffffff"));
                return c;
            case "b_page":
                c = new Canvas(7, 9);
                c.Rect(0, 0, 7, 9, C("f4f4f0")); c.Line(1, 3, 5, 3, C("c0c0c0")); c.Line(1, 5, 5, 5, C("c0c0c0"));
                return c;
            case "b_static":
                c = new Canvas(7, 7);
                var r = new System.Random(9);
                for (int i = 0; i < 49; i++) { byte g = (byte)r.Next(60, 255); if (r.NextDouble() < 0.8) c.Set(i % 7, i / 7, new Color32(g, g, g, 255)); }
                return c;
            case "b_gear":
                c = new Canvas(9, 9);
                c.Circle(4.5f, 4.5f, 4.5f, C("c0a060")); c.Circle(4.5f, 4.5f, 1.5f, Clear);
                for (int i = 0; i < 8; i += 2) c.Set(i % 2 == 0 ? i : 8 - i, i / 2 * 2, Clear);
                return c;
            default: // b_dot
                c = new Canvas(7, 7);
                c.Circle(3.5f, 3.5f, 3.5f, C("ffffff"));
                return c;
        }
    }

    // ================= ТАЙЛЫ =================
    // Ключ: t_{zone}_{kind}{variant}, zone: dock/garden/neon/arch

    static Canvas Tile(string key)
    {
        var p = key.Split('_');
        string zone = p[1];
        string kind = p[2];
        int v = 0;
        if (char.IsDigit(kind[kind.Length - 1])) { v = kind[kind.Length - 1] - '0'; kind = kind.Substring(0, kind.Length - 1); }
        var c = new Canvas(16, 16);
        int seed = (zone.GetHashCode() & 0xffff) + v * 13 + kind.Length * 101;
        switch (zone + ":" + kind)
        {
            // ---- Тихий Причал ----
            case "dock:floor":
                c.Rect(0, 0, 16, 16, (x, y) => (y % 4 == 3) ? C("2e2a36") : ((y / 4 + v) % 2 == 0 ? C("5b5566") : C("555062")));
                c.Set((v * 5 + 3) % 16, 1, C("8a8494")); c.Set((v * 7 + 11) % 16, 9, C("8a8494"));
                break;
            case "dock:street":
                c.Fill(C("3a4050"));
                for (int y = 0; y < 16; y += 4) for (int x = (y / 4 % 2) * 2; x < 16; x += 4) c.Rect(x, y, 3, 3, C("454c5e"));
                break;
            case "dock:wall":
                c.Rect(0, 0, 16, 16, (x, y) => ((x + (y / 4 % 2) * 4) % 8 == 0 || y % 4 == 0) ? C("1c1f2a") : C("2a2e3c"));
                if (v == 1) { c.Rect(4, 4, 8, 7, C("ffd870")); c.Line(8, 4, 8, 10, C("3a2a1a")); }
                break;
            case "dock:water":
                c.Fill(C("1c2a40"));
                for (int y = 2; y < 16; y += 5) { int o = (y * 3 + v * 4) % 16; c.Line(o, y, o + 4, y, C("2e4868")); }
                break;
            case "dock:lamp":
                c.Rect(7, 4, 2, 12, C("2a2a30")); c.Rect(5, 0, 6, 5, C("3a3a40")); c.Rect(6, 1, 4, 3, C("ffe080"));
                break;
            case "dock:clock":
                c.Fill(C("1c2a40")); c.Circle(8, 9, 6, C("b89a4a")); c.Circle(8, 9, 4.5f, C("d8d4c8"));
                c.Line(8, 9, 8, 6, Ink); c.Line(8, 9, 10, 10, Ink); c.Rect(0, 12, 16, 4, C("1c2a40")); c.Line(2, 13, 6, 13, C("2e4868"));
                break;
            case "dock:letters":
                c.Rect(2, 8, 7, 5, C("efe6d0")); c.Rect(6, 5, 7, 5, C("e0d8c0")); c.Rect(4, 10, 8, 4, C("f4ecd8"));
                c.Set(9, 12, C("b02a2a"));
                break;
            case "dock:post":
                c.Rect(5, 4, 6, 12, C("4a3a2a")); c.Rect(5, 4, 6, 2, C("6a5a4a")); c.Rect(4, 9, 8, 2, C("7a7060"));
                break;

            // ---- Забытый Сад ----
            case "garden:floor":
                c.Fill(C("4a3a2a")); c.Speckle(seed, 0.12f, C("5a4a36")); c.Speckle(seed + 1, 0.05f, C("3a2a1e"));
                break;
            case "garden:grass":
                c.Fill(C("2f5a34"));
                var gr = new System.Random(seed);
                for (int i = 0; i < 10; i++) { int x = gr.Next(16), y = gr.Next(2, 16); c.Line(x, y, x + gr.Next(-1, 2), y - 2, C("4a7a44")); }
                break;
            case "garden:wall":
                c.Rect(0, 0, 16, 16, (x, y) => (x % 8 == 0 || y % 8 == 0) ? C("3a4a40") : C("6f9c96"));
                c.Line(2, 6, 6, 2, C("b0d8d0")); c.Line(10, 14, 14, 10, C("b0d8d0"));
                if (v == 1) { c.Line(3, 0, 4, 15, C("3a6a3a")); c.Line(12, 0, 11, 10, C("3a6a3a")); c.Set(4, 8, C("4a8a4a")); c.Set(11, 5, C("4a8a4a")); }
                break;
            case "garden:flower":
                c.Line(8, 15, 8, 7, C("4a7a3a")); c.Tri(8, 12, 12, 10, 11, 13, C("5a8a4a"));
                c.Rect(4, 2, 9, 6, v == 1 ? C("c890a0") : C("3a3a44")); c.Rect(5, 3, 7, 2, C("e0c070"));
                c.Set(6, 6, C("f0f0f0")); c.Set(10, 6, C("f0f0f0"));
                break;
            case "garden:pot":
                c.Tri(3, 8, 13, 8, 8, 16, Clear); c.Rect(4, 8, 8, 8, C("a0603a")); c.Rect(3, 7, 10, 2, C("b87048"));
                c.Circle(8, 4, 3.5f, C("3a6a3a")); c.Set(7, 3, C("e0a0b8"));
                break;
            case "garden:water":
                c.Fill(C("1f3a3a"));
                for (int y = 3; y < 16; y += 5) { int o = (y * 5 + v * 4) % 16; c.Line(o, y, o + 3, y, C("3a6a60")); }
                break;

            // ---- Неоновый Архипелаг ----
            case "neon:floor":
                c.Rect(0, 0, 16, 16, (x, y) => (x % 8 == 0 || y % 8 == 0) ? C("2e2848") : C("1e1a2e"));
                c.Set(2, 2, C("4a4468")); c.Set(10, 10, C("4a4468"));
                break;
            case "neon:road":
                c.Fill(C("24203a")); if (v == 0) c.Rect(0, 7, 16, 2, C("ff4fd8")); else c.Rect(7, 0, 2, 16, C("40e0ff"));
                break;
            case "neon:wall":
                c.Fill(C("15101f"));
                var wr = new System.Random(seed);
                for (int y = 2; y < 14; y += 5) for (int x = 1; x < 16; x += 5)
                    { var cc = wr.Next(4); c.Rect(x, y, 3, 3, cc == 0 ? C("40e0ff") : cc == 1 ? C("ff4fd8") : cc == 2 ? C("ffd860") : C("2a2438")); }
                if (v == 1) c.Rect(0, 14, 16, 2, C("ff4fd8"));
                break;
            case "neon:sign":
                c.Rect(1, 3, 14, 9, C("15101f")); c.Rect(1, 3, 14, 1, C("ff4fd8")); c.Rect(1, 11, 14, 1, C("ff4fd8"));
                c.Rect(1, 3, 1, 9, C("ff4fd8")); c.Rect(14, 3, 1, 9, C("ff4fd8"));
                c.Line(4, 6, 11, 6, C("40e0ff")); c.Line(4, 8, 9, 8, C("40e0ff")); c.Rect(7, 12, 2, 4, C("3a3448"));
                break;
            case "neon:gear":
                c.Circle(8, 8, 7, C("b08a40")); c.Circle(8, 8, 4, C("8a6a30")); c.Circle(8, 8, 1.5f, C("15101f"));
                for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4 + v * 0.4f; c.Set((int)(8 + Mathf.Cos(a) * 7.5f), (int)(8 + Mathf.Sin(a) * 7.5f), Clear); }
                break;
            case "neon:water":
                c.Fill(C("1a0f2a"));
                for (int y = 2; y < 16; y += 4) { int o = (y * 7 + v * 5) % 16; c.Line(o, y, o + 2, y, y % 8 == 2 ? C("ff4fd8") : C("40e0ff")); }
                break;

            // ---- Бесконечный Архив ----
            case "arch:floor":
                c.Rect(0, 0, 16, 16, (x, y) => (x % 8 == 0 || y % 8 == 0) ? C("b0b0ac") : C("d8d8d4"));
                break;
            case "arch:carpet":
                c.Fill(C("7a7a78")); c.Rect(0, 0, 16, 1, C("5a5a58")); c.Speckle(seed, 0.08f, C("8a8a88"));
                break;
            case "arch:wall":
                c.Fill(C("2a2a2a"));
                var br = new System.Random(seed);
                for (int shelf = 0; shelf < 2; shelf++)
                {
                    int y0 = 1 + shelf * 8;
                    for (int x = 1; x < 15;)
                    {
                        int bw = br.Next(1, 3); byte g = (byte)br.Next(80, 220);
                        c.Rect(x, y0 + br.Next(0, 2), bw, 6, new Color32(g, g, g, 255));
                        x += bw + (br.NextDouble() < 0.2 ? 1 : 0);
                    }
                    c.Rect(0, y0 + 6, 16, 1, C("4a4a4a"));
                }
                break;
            case "arch:papers":
                c.Rect(3, 9, 10, 6, C("f4f4f0")); c.Rect(4, 6, 9, 4, C("e8e8e4")); c.Rect(5, 3, 7, 4, C("f8f8f4"));
                c.Line(6, 5, 10, 5, C("b0b0b0"));
                break;
            case "arch:pedestal":
                c.Rect(4, 6, 8, 10, C("e8e8e8")); c.Rect(3, 5, 10, 2, C("ffffff")); c.Rect(3, 14, 10, 2, C("c0c0c0"));
                break;
            case "arch:void":
                c.Fill(C("0a0a0a")); c.Speckle(seed, 0.03f, C("303030"));
                break;
            default:
                c.Fill(C("ff00ff"));
                break;
        }
        return c;
    }

    // ================= БОЛЬНИЦА (эпилог) =================

    static Canvas Hospital(bool awake)
    {
        var c = new Canvas(320, 240);
        c.Rect(0, 0, 320, 170, (x, y) => (x / 40 % 2 == 0) ? C("c8d0d8") : C("c0c8d2"));
        c.Rect(0, 170, 320, 70, (x, y) => ((x / 16 + y / 16) % 2 == 0) ? C("9aa4ae") : C("929ca6"));
        // окно
        c.Rect(196, 30, 98, 96, C("f0f0f0"));
        c.Rect(201, 35, 88, 86, C("4a5a70"));
        c.Rect(243, 35, 4, 86, C("f0f0f0")); c.Rect(201, 76, 88, 4, C("f0f0f0"));
        // монитор на стене над кроватью
        c.Rect(140, 92, 50, 36, C("2a2e34")); c.Rect(144, 96, 42, 26, C("0a1a10"));
        c.Rect(162, 128, 6, 4, C("6a6e74"));
        // тумбочка и цветы
        c.Rect(18, 150, 34, 50, C("c8b898")); c.Rect(18, 150, 34, 4, C("b0a080"));
        c.Rect(29, 132, 12, 18, C("a0c0d0"));
        c.Circle(31, 126, 5, C("f0d050")); c.Circle(39, 124, 5, C("f08080")); c.Circle(35, 118, 5, C("f0f0f0"));
        // кровать
        c.Rect(60, 150, 180, 36, C("e8eef2")); c.Rect(60, 186, 180, 8, C("8a929a"));
        c.Rect(60, 130, 8, 70, C("8a929a")); c.Rect(232, 140, 8, 60, C("8a929a"));
        c.Rect(72, 140, 50, 16, C("ffffff"));
        c.Rect(110, 146, 125, 30, C("a8c0d8"));
        // та самая куртка, перекинутая через спинку кровати
        c.Rect(222, 134, 24, 26, C("b5553c")); c.Rect(222, 134, 24, 4, C("8a3b2a"));
        c.Rect(219, 156, 7, 18, C("a04a34")); c.Rect(242, 156, 7, 18, C("a04a34"));
        c.Line(234, 138, 234, 159, C("d8b060"));
        // Ноа
        c.Circle(96, 138, 13, C("5a3a2a"));
        c.Ellipse(98, 142, 10, 10, C("f2c9a0"));
        c.Rect(88, 131, 20, 4, C("5a3a2a"));
        c.Rect(88, 136, 22, 3, C("f8f8f0")); // бинт
        if (awake)
        {
            c.Rect(92, 142, 2, 3, Ink); c.Rect(102, 142, 2, 3, Ink);
            c.Line(94, 149, 96, 150, Ink); c.Line(96, 150, 100, 150, Ink); c.Line(100, 150, 102, 149, Ink);
        }
        else
        {
            c.Line(91, 143, 94, 143, Ink); c.Line(101, 143, 104, 143, Ink);
        }
        c.Set(89, 147, C("e8a090")); c.Set(107, 147, C("e8a090"));
        return c;
    }
}
