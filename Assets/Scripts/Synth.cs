using System;
using System.Collections.Generic;
using UnityEngine;

// Процедурный чиптюн-синтезатор: вся музыка и звуки игры рендерятся в AudioClip кодом.
public static class Synth
{
    public const int SR = 22050;

    public enum Wv { Sq, Pulse, Tri, Sine, Saw, Box, Pluck, Drums }

    public class Voice
    {
        public Wv wave = Wv.Tri;
        public float vol = 0.2f;
        public string seq = "";
        public int oct;              // сдвиг октавы
        public float atk = 0.01f, dec = 0.15f, sus = 0.6f, rel = 0.1f;
        public float gate = 0.9f;    // доля длительности, пока нота "зажата"
        public float vib;            // глубина вибрато в полутонах
        public float detune;         // второй осциллятор, в полутонах (для "плавающего" звука)
        public float unit = 0.5f;    // длительность единицы в долях (0.5 = восьмая)
    }

    static readonly Dictionary<char, int> NoteBase = new Dictionary<char, int>
    { { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 }, { 'G', 7 }, { 'A', 9 }, { 'B', 11 } };

    static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public static int Midi(string n)
    {
        int i = 0;
        int m = NoteBase[char.ToUpper(n[i++])];
        while (i < n.Length && (n[i] == '#' || n[i] == 'b')) { m += n[i] == '#' ? 1 : -1; i++; }
        int oct = int.Parse(n.Substring(i));
        return m + (oct + 1) * 12;
    }

    public static string Name(int midi) => Names[((midi % 12) + 12) % 12] + (midi / 12 - 1);

    public static float Freq(float midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

    // ---------- Аккомпанемент из гармонии ----------
    // prog: "Am G F E" (один аккорд на такт), pattern: "1:2 5:2 3:2" или "1+3+5:6"
    // Ступени: 1 корень, 3 терция, 5 квинта, 7 септима, 8 октава, 9 терция+окт, x квинта+окт
    public static string Prog(string prog, string pattern, int oct)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ch in prog.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int[] tones = Chord(ch, oct);
            foreach (var tok in pattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = tok.Split(':');
                string dur = parts.Length > 1 ? parts[1] : "1";
                if (parts[0] == "-") { sb.Append("-:" + dur + " "); continue; }
                var notes = new List<string>();
                foreach (var d in parts[0].Split('+'))
                {
                    int midi;
                    switch (d)
                    {
                        case "1": midi = tones[0]; break;
                        case "3": midi = tones[1]; break;
                        case "5": midi = tones[2]; break;
                        case "7": midi = tones[3]; break;
                        case "8": midi = tones[0] + 12; break;
                        case "9": midi = tones[1] + 12; break;
                        case "x": midi = tones[2] + 12; break;
                        case "0": midi = tones[2] - 12; break;
                        default: midi = tones[0]; break;
                    }
                    notes.Add(Name(midi));
                }
                sb.Append(string.Join("+", notes) + ":" + dur + " ");
            }
        }
        return sb.ToString();
    }

    static int[] Chord(string c, int oct)
    {
        int i = 0;
        int root = NoteBase[c[i++]];
        while (i < c.Length && (c[i] == '#' || c[i] == 'b')) { root += c[i] == '#' ? 1 : -1; i++; }
        string q = c.Substring(i);
        int third = 4, fifth = 7, sev = 10;
        if (q.StartsWith("dim")) { third = 3; fifth = 6; sev = 9; }
        else if (q.StartsWith("m") && !q.StartsWith("maj")) third = 3;
        if (q.Contains("maj7")) sev = 11;
        int r = root + (oct + 1) * 12;
        return new[] { r, r + third, r + fifth, r + sev };
    }

    // ---------- Рендер ----------
    struct Ev { public int[] notes; public float dur; }

    static List<Ev> Parse(string seq)
    {
        var list = new List<Ev>();
        foreach (var tok in seq.Split(new[] { ' ', '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = tok.Split(':');
            float dur = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 1f;
            if (parts[0] == "-" || parts[0] == "r") { list.Add(new Ev { notes = null, dur = dur }); continue; }
            var ns = parts[0].Split('+');
            var arr = new int[ns.Length];
            for (int k = 0; k < ns.Length; k++) arr[k] = Midi(ns[k]);
            list.Add(new Ev { notes = arr, dur = dur });
        }
        return list;
    }

    static float Osc(Wv w, float p)
    {
        p -= Mathf.Floor(p);
        switch (w)
        {
            case Wv.Sq: return p < 0.5f ? 0.6f : -0.6f;
            case Wv.Pulse: return p < 0.25f ? 0.6f : -0.6f;
            case Wv.Tri: return 4f * Mathf.Abs(p - 0.5f) - 1f;
            case Wv.Sine: return Mathf.Sin(p * 2f * Mathf.PI);
            case Wv.Saw: return (2f * p - 1f) * 0.6f;
            case Wv.Box:
                return Mathf.Sin(p * 2f * Mathf.PI) * 0.7f + Mathf.Sin(p * 4f * Mathf.PI) * 0.2f + Mathf.Sin(p * 6f * Mathf.PI) * 0.1f;
            case Wv.Pluck: return 4f * Mathf.Abs(p - 0.5f) - 1f;
        }
        return 0;
    }

    public static float[] RenderVoices(float bpm, float lengthSec, Voice[] vs)
    {
        int total = Mathf.CeilToInt(lengthSec * SR);
        var buf = new float[total];
        float beat = 60f / bpm;
        var rnd = new System.Random(1234);
        foreach (var v in vs)
        {
            if (v.wave == Wv.Drums) { RenderDrums(buf, v, beat, rnd); continue; }
            var evs = Parse(v.seq);
            if (evs.Count == 0) continue;
            float t = 0;
            int idx = 0;
            while (t < lengthSec - 0.0001f)
            {
                var e = evs[idx % evs.Count];
                idx++;
                float dur = e.dur * v.unit * beat;
                if (e.notes != null)
                    foreach (var n in e.notes) Note(buf, v, n + v.oct * 12, t, dur, total, lengthSec);
                t += dur;
            }
        }
        return buf;
    }

    static void Note(float[] buf, Voice v, int midi, float start, float dur, int total, float lengthSec)
    {
        float f = Freq(midi);
        float on = dur * v.gate;
        bool percussive = v.wave == Wv.Box || v.wave == Wv.Pluck;
        float len = percussive ? Mathf.Max(on, 1.2f) : on + v.rel;
        int s0 = (int)(start * SR);
        int n = (int)(len * SR);
        float ph = 0, ph2 = 0;
        float f2 = v.detune != 0 ? Freq(midi + v.detune) : 0;
        for (int i = 0; i < n; i++)
        {
            int si = s0 + i;
            float tt = i / (float)SR;
            float env;
            if (percussive)
            {
                env = Mathf.Min(1f, tt / 0.004f) * Mathf.Exp(-tt * (v.wave == Wv.Box ? 3.2f : 7f));
            }
            else
            {
                if (tt < v.atk) env = tt / v.atk;
                else if (tt < v.atk + v.dec) env = 1f - (1f - v.sus) * ((tt - v.atk) / v.dec);
                else env = v.sus;
                if (tt > on) env *= Mathf.Max(0, 1f - (tt - on) / v.rel);
            }
            float vib = v.vib > 0 ? Mathf.Sin(tt * 2f * Mathf.PI * 5.5f) * v.vib * Mathf.Min(1, tt * 2f) : 0;
            float ff = vib != 0 ? f * Mathf.Pow(2, vib / 12f) : f;
            ph += ff / SR;
            float s = Osc(v.wave, ph);
            if (f2 > 0) { ph2 += f2 / SR; s = (s + Osc(v.wave, ph2)) * 0.6f; }
            // Трек зациклен: хвосты нот заворачиваем в начало
            int idx = si % total;
            buf[idx] += s * env * v.vol;
        }
    }

    static void RenderDrums(float[] buf, Voice v, float beat, System.Random rnd)
    {
        string pat = v.seq.Replace(" ", "");
        if (pat.Length == 0) return;
        float step = beat / 4f; // шестнадцатые
        int total = buf.Length;
        float len = total / (float)SR;
        int k = 0;
        for (float t = 0; t < len - 0.0001f; t += step, k++)
        {
            char c = pat[k % pat.Length];
            if (c == '.') continue;
            int s0 = (int)(t * SR);
            float dlen = c == 'k' ? 0.18f : c == 's' ? 0.16f : c == 'o' ? 0.14f : 0.04f;
            int n = (int)(dlen * SR);
            float ph = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                float tt = i / (float)SR;
                float s;
                float noise = (float)(rnd.NextDouble() * 2 - 1);
                switch (c)
                {
                    case 'k':
                        float fk = 45f + 110f * Mathf.Exp(-tt * 30f);
                        ph += fk / SR;
                        s = Mathf.Sin(ph * 2 * Mathf.PI) * Mathf.Exp(-tt * 14f) * 1.4f;
                        break;
                    case 's':
                        ph += 190f / SR;
                        s = (noise * 0.7f + Mathf.Sin(ph * 2 * Mathf.PI) * 0.4f) * Mathf.Exp(-tt * 22f);
                        break;
                    default: // хэт: "высокочастотный" шум через разность
                        s = (noise - prev) * 0.5f * Mathf.Exp(-tt * (c == 'o' ? 25f : 90f));
                        break;
                }
                prev = noise;
                buf[(s0 + i) % total] += s * v.vol;
            }
        }
    }

    public static void Delay(float[] buf, float sec, float fb, float mix)
    {
        if (sec <= 0 || mix <= 0) return;
        int d = (int)(sec * SR);
        var line = new float[d];
        int p = 0;
        // Два прохода, чтобы эхо корректно завернулось в начало петли
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < buf.Length; i++)
            {
                float dry = buf[i];
                float wet = line[p];
                line[p] = dry + wet * fb;
                p = (p + 1) % d;
                if (pass == 1) buf[i] = dry + wet * mix;
            }
    }

    public static AudioClip ToClip(string name, float[] buf, float gain = 1f)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            float x = buf[i] * gain;
            buf[i] = x / (1f + Mathf.Abs(x)) * 1.3f; // мягкий лимитер
        }
        var clip = AudioClip.Create(name, buf.Length, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }

    public static AudioClip Track(string name, float bpm, int bars, int beatsPerBar, float delay, float fb, float mix, params Voice[] vs)
    {
        float len = bars * beatsPerBar * 60f / bpm;
        var buf = RenderVoices(bpm, len, vs);
        Delay(buf, delay, fb, mix);
        return ToClip(name, buf);
    }

    // ---------- Простые генераторы для SFX ----------
    public static AudioClip Sfx(string name, float len, Func<float, float> f)
    {
        int n = (int)(len * SR);
        var buf = new float[n];
        for (int i = 0; i < n; i++) buf[i] = f(i / (float)SR);
        var clip = AudioClip.Create(name, n, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }

    static readonly System.Random NR = new System.Random(77);
    public static float Noise() => (float)(NR.NextDouble() * 2 - 1);
    public static float Sq(float t, float f) => ((t * f) % 1f) < 0.5f ? 1f : -1f;
    public static float Sin(float t, float f) => Mathf.Sin(t * f * 2 * Mathf.PI);
}
