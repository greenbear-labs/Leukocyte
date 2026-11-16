using System.Collections.Generic;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Recent combat events shown in the HUD, plus the player's score.</summary>
    public static class KillFeed
    {
        public struct Entry
        {
            public string text;
            public Color color;
            public float time;
        }

        public static readonly List<Entry> Entries = new List<Entry>();
        public static int Score { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Reset(); }

        public static void Reset()
        {
            Entries.Clear();
            Score = 0;
        }

        public static void Add(string text, Color color)
        {
            Entries.Add(new Entry { text = text, color = color, time = Time.time });
            if (Entries.Count > 6) Entries.RemoveAt(0);
        }

        /// <summary>Call when the player personally destroys an enemy.</summary>
        public static void PlayerKill(Combatant victim, string how)
        {
            var gm = GameManager.I;
            if (gm) gm.Kills++;
            int points = victim && victim.GetComponent<Brute>() ? 300 : 100;
            Score += points;
            Add($"{how} {UnitName(victim)}  +{points}", new Color(1f, 0.9f, 0.5f));
        }

        public static void AddScore(int points) { Score += points; }

        public static string UnitName(Combatant c)
        {
            if (!c) return "enemy";
            if (c.GetComponent<Brute>()) return "Brute";
            if (c.GetComponent<Spitter>()) return "Spitter";
            if (c.GetComponent<Swarmer>()) return "Swarmer";
            return "enemy";
        }
    }
}
