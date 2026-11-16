using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Leukocyte
{
    public enum GermType { Swarmer, Spitter, Brute }

    /// <summary>Spawns escalating germ waves from the Wound Breach.</summary>
    public class WaveManager : MonoBehaviour
    {
        [Serializable]
        public struct Wave
        {
            public int swarmers;
            public int spitters;
            public int brutes;
            public float spawnInterval;
        }

        public Wave[] waves =
        {
            new Wave { swarmers = 6,  spitters = 0, spawnInterval = 0.9f },
            new Wave { swarmers = 8,  spitters = 2, spawnInterval = 0.8f },
            new Wave { swarmers = 10, spitters = 3, spawnInterval = 0.7f },
            new Wave { swarmers = 12, spitters = 5, brutes = 1, spawnInterval = 0.6f },
            new Wave { swarmers = 14, spitters = 6, brutes = 2, spawnInterval = 0.5f },
        };

        public float firstDelay = 6f;
        public float breakTime = 10f;

        public int CurrentWave { get; private set; }
        public int TotalWaves => waves.Length;
        public bool InBreak { get; private set; }
        public float BreakTimer { get; private set; }
        public int PendingSpawns { get; private set; }
        public int Remaining => PendingSpawns + Germ.Alive.Count;

        Vector3 breach;

        public void Init(Vector3 breachCenter)
        {
            breach = breachCenter;
            StartCoroutine(Run());
        }

        static bool Playing => GameManager.I && GameManager.I.State == GameState.Playing;

        IEnumerator Countdown(float seconds)
        {
            InBreak = true;
            BreakTimer = seconds;
            while (BreakTimer > 0f)
            {
                BreakTimer -= Time.deltaTime;
                yield return null;
            }
            InBreak = false;
        }

        IEnumerator Run()
        {
            yield return Countdown(firstDelay);

            for (int i = 0; i < waves.Length; i++)
            {
                if (!Playing) yield break;
                CurrentWave = i + 1;

                var queue = new List<GermType>();
                for (int s = 0; s < waves[i].swarmers; s++) queue.Add(GermType.Swarmer);
                for (int s = 0; s < waves[i].spitters; s++) queue.Add(GermType.Spitter);
                for (int s = 0; s < waves[i].brutes; s++) queue.Add(GermType.Brute);
                for (int s = queue.Count - 1; s > 0; s--)
                {
                    int j = Random.Range(0, s + 1);
                    (queue[s], queue[j]) = (queue[j], queue[s]);
                }

                PendingSpawns = queue.Count;
                foreach (var type in queue)
                {
                    if (!Playing) yield break;
                    Spawn(type);
                    PendingSpawns--;
                    yield return new WaitForSeconds(waves[i].spawnInterval);
                }

                while (Germ.Alive.Count > 0)
                {
                    if (!Playing) yield break;
                    yield return null;
                }

                if (i < waves.Length - 1) yield return Countdown(breakTime);
            }

            if (Playing) GameManager.I.Win("Infection cleared. The femoral artery is secure.");
        }

        void Spawn(GermType type)
        {
            var pos = breach + new Vector3(Random.Range(-6f, 6f), Random.Range(-2f, 3f), Random.Range(-2f, 0f));
            pos = Level.Clamp(pos);
            if (type == GermType.Swarmer) GermFactory.Swarmer(pos);
            else if (type == GermType.Spitter) GermFactory.Spitter(pos);
            else GermFactory.Brute(pos);
        }
    }
}
