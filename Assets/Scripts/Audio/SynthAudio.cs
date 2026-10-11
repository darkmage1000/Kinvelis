using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Wildbond
{
    public enum Sfx
    {
        Hit, HitHeavy, Shoot, Burst, Whoosh, Summon, Recall, Jump, Step, Swing, Clunk, Mine, Chop, Pluck, Hurt,
        Throw, Bind, Shake, CaptureSuccess, CaptureFail, LevelUp, Craft, Build, UiOpen, UiClick, Pickup, Count
    }

    /// <summary>
    /// Zero-asset audio: every SFX and the three biome music loops are synthesized at startup
    /// (music on a worker thread). Pooled AudioSources, crossfading music.
    /// </summary>
    public class SynthAudio : MonoBehaviour
    {
        static SynthAudio inst;
        const int SR = 22050;

        public static float SfxVolume = 0.8f;
        public static float MusicVolume = 0.45f;

        readonly AudioClip[] clips = new AudioClip[(int)Sfx.Count];
        readonly List<AudioSource> sources = new List<AudioSource>();
        AudioSource musicA, musicB;
        readonly AudioClip[] music = new AudioClip[3];
        float[][] musicData;
        Task musicTask;
        int currentTrack = -1;
        float fade = 1f;
        int next;
        readonly float[] lastPlay = new float[(int)Sfx.Count];

        public void Init()
        {
            inst = this;
            for (int i = 0; i < 10; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sources.Add(s);
            }
            musicA = gameObject.AddComponent<AudioSource>();
            musicB = gameObject.AddComponent<AudioSource>();
            foreach (var m in new[] { musicA, musicB }) { m.loop = true; m.playOnAwake = false; m.volume = 0f; }
            for (int i = 0; i < (int)Sfx.Count; i++) clips[i] = MakeClip(((Sfx)i).ToString(), Gen((Sfx)i));
            musicTask = Task.Run(() =>
            {
                var d = new float[3][];
                for (int b = 0; b < 3; b++) d[b] = MusicGen.Compose((BiomeId)b, SR);
                musicData = d;
            });
        }

        static AudioClip MakeClip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }

        /// <summary>Three music tracks so far: outer lands borrow the closest mood.</summary>
        static int MusicFor(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Badlands: case BiomeId.Desert: case BiomeId.Blight: return 1;
                case BiomeId.Peaks: case BiomeId.Glow: return 2;
                default: return 0;
            }
        }

        public static void Play(Sfx s, float pitch = 1f, float volume = 1f)
        {
            if (inst == null) return;
            // de-duplicate identical sounds in the same few ms (big AoE hits)
            if (Time.unscaledTime - inst.lastPlay[(int)s] < 0.035f) return;
            inst.lastPlay[(int)s] = Time.unscaledTime;
            var src = inst.sources[inst.next];
            inst.next = (inst.next + 1) % inst.sources.Count;
            src.pitch = pitch;
            src.volume = volume * SfxVolume;
            src.clip = inst.clips[(int)s];
            src.Play();
        }

        void Update()
        {
            if (musicData != null && music[0] == null)
            {
                for (int i = 0; i < 3; i++) music[i] = MakeClip("music_" + (BiomeId)i, musicData[i]);
                musicData = null;
            }
            if (music[0] == null) return;
            int want = EnvironmentSystem.I != null ? MusicFor(EnvironmentSystem.I.CurrentBiome) : 0;
            if (want != currentTrack)
            {
                currentTrack = want;
                (musicA, musicB) = (musicB, musicA);
                musicA.clip = music[want];
                musicA.Play();
                fade = 0f;
            }
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / 3f);
            float night = EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight ? 0.7f : 1f;
            musicA.volume = fade * MusicVolume * night;
            musicB.volume = (1f - fade) * MusicVolume * night;
            if (fade >= 1f && musicB.isPlaying) musicB.Stop();
        }

        // ============================================================ SFX GENERATION
        enum Wave { Sine, Square, Tri, Saw, Noise }

        static float Osc(Wave w, float phase, System.Random r)
        {
            switch (w)
            {
                case Wave.Sine: return Mathf.Sin(phase * Mathf.PI * 2f);
                case Wave.Square: return (phase % 1f) < 0.5f ? 0.6f : -0.6f;
                case Wave.Tri: { float p = phase % 1f; return (p < 0.5f ? p * 4f - 1f : 3f - p * 4f); }
                case Wave.Saw: return ((phase % 1f) * 2f - 1f) * 0.7f;
                default: return (float)(r.NextDouble() * 2.0 - 1.0);
            }
        }

        /// <summary>Adds a tone into buf at offset (seconds).</summary>
        static void Tone(List<float> buf, float at, float dur, Wave w, float f0, float f1, float vol, float attack = 0.005f, float lowpass = 1f)
        {
            var r = new System.Random(1234);
            int start = (int)(at * SR), n = (int)(dur * SR);
            while (buf.Count < start + n) buf.Add(0f);
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / SR;
                float env = Mathf.Min(1f, i / (attack * SR + 1f)) * (1f - t) * (1f - t);
                float s = Osc(w, phase, r);
                lp += (s - lp) * lowpass;
                buf[start + i] += lp * env * vol;
            }
        }

        static float[] Gen(Sfx s)
        {
            var b = new List<float>(SR);
            switch (s)
            {
                case Sfx.Hit: Tone(b, 0, 0.09f, Wave.Noise, 0, 0, 0.5f, 0.001f, 0.35f); Tone(b, 0, 0.12f, Wave.Sine, 190, 80, 0.7f); break;
                case Sfx.HitHeavy: Tone(b, 0, 0.14f, Wave.Noise, 0, 0, 0.6f, 0.001f, 0.25f); Tone(b, 0, 0.25f, Wave.Sine, 150, 45, 0.9f); break;
                case Sfx.Shoot: Tone(b, 0, 0.14f, Wave.Square, 620, 280, 0.25f, 0.002f, 0.4f); break;
                case Sfx.Burst: Tone(b, 0, 0.4f, Wave.Noise, 0, 0, 0.5f, 0.01f, 0.12f); Tone(b, 0, 0.35f, Wave.Sine, 110, 50, 0.7f); break;
                case Sfx.Whoosh: Tone(b, 0, 0.22f, Wave.Noise, 0, 0, 0.35f, 0.08f, 0.18f); break;
                case Sfx.Summon: for (int i = 0; i < 4; i++) Tone(b, i * 0.055f, 0.2f, Wave.Tri, new[] { 523f, 659f, 784f, 1046f }[i], new[] { 523f, 659f, 784f, 1046f }[i], 0.35f); break;
                case Sfx.Recall: for (int i = 0; i < 3; i++) Tone(b, i * 0.05f, 0.16f, Wave.Tri, new[] { 1046f, 784f, 523f }[i], new[] { 1046f, 784f, 523f }[i], 0.3f); break;
                case Sfx.Jump: Tone(b, 0, 0.12f, Wave.Sine, 300, 540, 0.35f); break;
                case Sfx.Step: Tone(b, 0, 0.05f, Wave.Noise, 0, 0, 0.35f, 0.001f, 0.12f); break;
                case Sfx.Swing: Tone(b, 0, 0.14f, Wave.Noise, 0, 0, 0.35f, 0.04f, 0.5f); break;
                case Sfx.Clunk: Tone(b, 0, 0.1f, Wave.Square, 130, 110, 0.35f, 0.001f, 0.3f); break;
                case Sfx.Mine: Tone(b, 0, 0.03f, Wave.Noise, 0, 0, 0.5f, 0.001f, 0.8f); Tone(b, 0, 0.2f, Wave.Sine, 1250, 1180, 0.3f); Tone(b, 0, 0.16f, Wave.Sine, 820, 800, 0.25f); break;
                case Sfx.Chop: Tone(b, 0, 0.07f, Wave.Noise, 0, 0, 0.5f, 0.001f, 0.3f); Tone(b, 0, 0.1f, Wave.Tri, 240, 140, 0.5f); break;
                case Sfx.Pluck: Tone(b, 0, 0.1f, Wave.Tri, 720, 480, 0.4f); break;
                case Sfx.Hurt: Tone(b, 0, 0.22f, Wave.Saw, 320, 140, 0.4f, 0.002f, 0.3f); break;
                case Sfx.Throw: Tone(b, 0, 0.18f, Wave.Noise, 0, 0, 0.25f, 0.05f, 0.25f); Tone(b, 0, 0.15f, Wave.Sine, 400, 820, 0.25f); break;
                case Sfx.Bind: Tone(b, 0, 0.3f, Wave.Tri, 300, 640, 0.35f); Tone(b, 0.05f, 0.3f, Wave.Sine, 1200, 1600, 0.12f); break;
                case Sfx.Shake: Tone(b, 0, 0.1f, Wave.Sine, 210, 150, 0.6f); Tone(b, 0, 0.02f, Wave.Noise, 0, 0, 0.3f, 0.001f, 0.6f); break;
                case Sfx.CaptureSuccess:
                {
                    float[] n = { 523f, 659f, 784f, 1046f, 1318f };
                    for (int i = 0; i < n.Length; i++) Tone(b, i * 0.09f, i == n.Length - 1 ? 0.7f : 0.22f, Wave.Tri, n[i], n[i], 0.35f);
                    Tone(b, 0.45f, 0.7f, Wave.Sine, 1568f, 1568f, 0.15f);
                    break;
                }
                case Sfx.CaptureFail: Tone(b, 0, 0.12f, Wave.Square, 420, 380, 0.2f); Tone(b, 0.12f, 0.25f, Wave.Square, 300, 180, 0.2f); break;
                case Sfx.LevelUp:
                {
                    float[] n = { 392f, 523f, 659f, 784f, 1046f };
                    for (int i = 0; i < n.Length; i++) Tone(b, i * 0.07f, 0.35f, Wave.Tri, n[i], n[i], 0.3f);
                    break;
                }
                case Sfx.Craft: Tone(b, 0, 0.12f, Wave.Sine, 880, 880, 0.3f); Tone(b, 0.1f, 0.2f, Wave.Sine, 1320, 1320, 0.3f); break;
                case Sfx.Build: Tone(b, 0, 0.2f, Wave.Sine, 120, 60, 0.7f); Tone(b, 0.12f, 0.35f, Wave.Tri, 660, 660, 0.25f); Tone(b, 0.2f, 0.35f, Wave.Tri, 990, 990, 0.2f); break;
                case Sfx.UiOpen: Tone(b, 0, 0.09f, Wave.Sine, 660, 900, 0.25f); break;
                case Sfx.UiClick: Tone(b, 0, 0.04f, Wave.Sine, 1300, 1100, 0.2f); break;
                case Sfx.Pickup: Tone(b, 0, 0.09f, Wave.Sine, 880, 1320, 0.25f); break;
            }
            if (b.Count == 0) b.Add(0);
            // soft clip
            var arr = b.ToArray();
            for (int i = 0; i < arr.Length; i++) arr[i] = (float)System.Math.Tanh(arr[i]);
            return arr;
        }
    }

    /// <summary>Procedural biome music: pad + bass + seeded pentatonic melody + light percussion. Thread-safe.</summary>
    public static class MusicGen
    {
        public static float[] Compose(BiomeId biome, int sr)
        {
            float bpm; int[] scale; float root; int[] prog; bool perc; float bright;
            switch (biome)
            {
                case BiomeId.Badlands:
                    bpm = 86; scale = new[] { 0, 1, 4, 5, 7, 8, 10 }; root = 146.83f; prog = new[] { 0, 0, 5, 4 }; perc = true; bright = 0.6f; break;
                case BiomeId.Peaks:
                    bpm = 68; scale = new[] { 0, 2, 3, 7, 9 }; root = 110f; prog = new[] { 0, 5, 3, 4 }; perc = false; bright = 0.8f; break;
                default:
                    bpm = 98; scale = new[] { 0, 2, 4, 7, 9 }; root = 130.81f; prog = new[] { 0, 5, 3, 4 }; perc = true; bright = 1f; break;
            }
            float beat = 60f / bpm;
            int bars = 8;
            float len = bars * 4 * beat;
            int n = (int)(len * sr);
            var buf = new float[n];
            var rng = new System.Random(77 + (int)biome * 13);

            float Note(int degree, int octave)
            {
                int oct = octave + degree / scale.Length;
                int d = ((degree % scale.Length) + scale.Length) % scale.Length;
                return root * Mathf.Pow(2f, oct + scale[d] / 12f);
            }

            void Add(float at, float dur, float freq, float vol, float attack, int wave)
            {
                int s0 = (int)(at * sr), cnt = (int)(dur * sr);
                float ph = 0f;
                for (int i = 0; i < cnt; i++)
                {
                    int idx = (s0 + i) % n; // wrap for seamless loop
                    float t = i / (float)sr;
                    float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-t * (wave == 1 ? 3.5f : 0.9f) / dur);
                    ph += freq / sr;
                    float v = wave == 0 ? Mathf.Sin(ph * 6.2831853f) + 0.25f * Mathf.Sin(ph * 12.566f) * bright
                            : (wave == 1 ? Mathf.Sin(ph * 6.2831853f) * (1f + 0.5f * Mathf.Sin(ph * 18.85f)) * 0.7f
                            : (float)(rng.NextDouble() * 2 - 1));
                    buf[idx] += v * env * vol;
                }
            }

            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4 * beat;
                int chord = prog[bar % prog.Length];
                // pad: chord tones, long
                Add(t0, 4 * beat * 1.1f, Note(chord, 1), 0.06f, 0.6f, 0);
                Add(t0, 4 * beat * 1.1f, Note(chord + 2, 1), 0.05f, 0.7f, 0);
                Add(t0, 4 * beat * 1.1f, Note(chord + 4, 1), 0.04f, 0.8f, 0);
                // bass
                Add(t0, 2 * beat, Note(chord, 0), 0.11f, 0.02f, 0);
                Add(t0 + 2 * beat, 2 * beat, Note(chord + (bar % 2 == 0 ? 4 : 2), 0), 0.09f, 0.02f, 0);
                // melody: 8th-note phrase, sparse
                int deg = chord + 2 + rng.Next(0, 3);
                for (int e = 0; e < 8; e++)
                {
                    if (rng.NextDouble() < (biome == BiomeId.Peaks ? 0.55 : 0.4)) continue;
                    deg += rng.Next(-2, 3);
                    deg = Mathf.Clamp(deg, chord, chord + 9);
                    Add(t0 + e * beat * 0.5f, beat * (biome == BiomeId.Peaks ? 1.6f : 0.9f), Note(deg, 2), 0.07f, 0.005f, 1);
                }
                if (perc)
                {
                    for (int q = 0; q < 8; q++)
                        Add(t0 + q * beat * 0.5f, 0.06f, 0f, q % 2 == 0 ? 0.03f : 0.015f, 0.001f, 2);
                }
            }
            // normalize + gentle saturation
            float peak = 0.001f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            float g = 0.8f / peak;
            for (int i = 0; i < n; i++) buf[i] = (float)System.Math.Tanh(buf[i] * g);
            return buf;
        }
    }
}
