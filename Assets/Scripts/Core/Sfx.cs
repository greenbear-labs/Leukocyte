using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Leukocyte
{
    /// <summary>Procedurally generated placeholder sound effects, so the prototype needs no audio files.
    /// Replace with real clips during the audio pass.</summary>
    public static class Sfx
    {
        const int Rate = 44100;
        const int MaxVoices = 28;

        public static AudioClip Shot, BotShot, HitTick, Squelch, Boom, Hurt, Spit, Slam, CaptureGood, CaptureBad, Engulf, Throw, Denied;

        static bool built;
        static int voices;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { voices = 0; }

        static void Ensure()
        {
            if (built) return;
            built = true;

            Shot = Make("Shot", 0.14f, (t, k, n) => (Sweep(t, 1500f, 280f, 0.14f) * 0.6f + n * 0.35f) * Decay(k, 6f));
            BotShot = Make("BotShot", 0.12f, (t, k, n) => (Sweep(t, 1200f, 300f, 0.12f) * 0.5f + n * 0.3f) * Decay(k, 7f));
            HitTick = Make("HitTick", 0.04f, (t, k, n) => Mathf.Sin(t * 2f * Mathf.PI * 2400f) * Decay(k, 5f));
            Squelch = MakeFiltered("Squelch", 0.3f, 0.08f, (t, k, n) => (n * 0.8f + Mathf.Sin(t * 2f * Mathf.PI * (180f - 120f * k)) * 0.6f) * Decay(k, 4f));
            Boom = MakeFiltered("Boom", 0.9f, 0.04f, (t, k, n) => (n * 1.2f + Mathf.Sin(t * 2f * Mathf.PI * (60f - 30f * k)) * 0.8f) * Decay(k, 3.5f));
            Hurt = MakeFiltered("Hurt", 0.18f, 0.15f, (t, k, n) => (n * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 95f)) * Decay(k, 5f));
            Spit = MakeFiltered("Spit", 0.22f, 0.3f, (t, k, n) => (n + Sweep(t, 300f, 900f, 0.22f) * 0.4f) * Mathf.Sin(k * Mathf.PI));
            Slam = MakeFiltered("Slam", 0.6f, 0.05f, (t, k, n) => (n + Mathf.Sin(t * 2f * Mathf.PI * 45f) * 1.2f) * Decay(k, 4f));
            Engulf = MakeFiltered("Engulf", 0.35f, 0.12f, (t, k, n) => (n * 0.6f + Sweep(t, 400f, 120f, 0.35f) * 0.7f) * Mathf.Sin(k * Mathf.PI));
            Throw = Make("Throw", 0.15f, (t, k, n) => n * 0.4f * Mathf.Sin(k * Mathf.PI));
            Denied = Make("Denied", 0.12f, (t, k, n) => Square(t, 140f) * 0.25f * Decay(k, 3f));
            CaptureGood = Make("CaptureGood", 0.5f, (t, k, n) => Mathf.Sin(t * 2f * Mathf.PI * (k < 0.4f ? 660f : 990f)) * 0.5f * Decay(k, 2f));
            CaptureBad = Make("CaptureBad", 0.5f, (t, k, n) => Mathf.Sin(t * 2f * Mathf.PI * (k < 0.4f ? 440f : 294f)) * 0.5f * Decay(k, 2f));
        }

        // ---- Playback ----------------------------------------------------

        /// <summary>Positional sound in the world.</summary>
        public static void At(Func<AudioClip> clip, Vector3 position, float volume = 1f, float pitchJitter = 0.08f)
        {
            Ensure();
            Spawn(clip(), position, volume, pitchJitter, true);
        }

        /// <summary>Non-positional UI or first-person sound.</summary>
        public static void UI(Func<AudioClip> clip, float volume = 1f, float pitchJitter = 0.05f)
        {
            Ensure();
            Spawn(clip(), Vector3.zero, volume, pitchJitter, false);
        }

        static void Spawn(AudioClip clip, Vector3 position, float volume, float pitchJitter, bool spatial)
        {
            if (!clip || voices >= MaxVoices || !Level.Root) return;
            var go = new GameObject("Sfx_" + clip.name);
            go.transform.SetParent(Level.Root, false);
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.spatialBlend = spatial ? 1f : 0f;
            src.minDistance = 4f;
            src.maxDistance = 80f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.dopplerLevel = 0f;
            src.Play();
            voices++;
            go.AddComponent<VoiceRelease>().life = clip.length / src.pitch + 0.05f;
        }

        class VoiceRelease : MonoBehaviour
        {
            public float life;
            void Update() { life -= Time.deltaTime; if (life <= 0f) Destroy(gameObject); }
            void OnDestroy() { voices = Mathf.Max(0, voices - 1); }
        }

        // ---- Synthesis ---------------------------------------------------

        delegate float Gen(float t, float k, float noise);

        static AudioClip Make(string name, float seconds, Gen gen) => MakeFiltered(name, seconds, 1f, gen);

        /// <summary>Builds a clip; the noise input is passed through a one-pole low-pass (1 = unfiltered).</summary>
        static AudioClip MakeFiltered(string name, float seconds, float lowpass, Gen gen)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (raw - lp) * lowpass;
                float t = i / (float)Rate;
                data[i] = Mathf.Clamp(gen(t, t / seconds, lp) * 0.7f, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Decay(float k, float rate) => Mathf.Exp(-k * rate);
        static float Square(float t, float f) => Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * f));

        static float Sweep(float t, float from, float to, float seconds)
        {
            // Phase of a linear frequency sweep.
            float k = (to - from) / seconds;
            return Mathf.Sin(2f * Mathf.PI * (from * t + 0.5f * k * t * t));
        }
    }
}
