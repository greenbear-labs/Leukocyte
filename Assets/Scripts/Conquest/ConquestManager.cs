using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Tissue War rules: reinforcement tickets, ticket bleed for holding the majority of outposts,
    /// AI reinforcements for both sides and spawn selection.</summary>
    public class ConquestManager : MonoBehaviour
    {
        public static ConquestManager I { get; private set; }

        public int startTickets = 150;
        public float bleedInterval = 3f;
        public int maxImmuneBots = 11;
        public int maxGerms = 18;
        public float reinforceInterval = 1.2f;
        [Range(0f, 1f)] public float spitterShare = 0.3f;
        [Range(0f, 1f)] public float bruteShare = 0.1f;
        public int maxBrutes = 3;

        public readonly int[] Tickets = new int[2];
        public readonly List<Outpost> Outposts = new List<Outpost>();
        public Outpost ImmuneHQ { get; private set; }
        public Outpost InfectionHQ { get; private set; }
        public string LastEvent { get; private set; } = "";
        public float LastEventTime { get; private set; } = -10f;

        /// <summary>Index into PlayerSpawnOptions chosen on the respawn screen.</summary>
        public int PlayerSpawnChoice { get; set; } = -1;

        public int bossTicketThreshold = 60;
        public float bossHealth = 3000f;
        bool bossSpawned;
        float bleedTimer;
        float reinforceTimer;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        public void Init(List<Outpost> outposts, Outpost immuneHQ, Outpost infectionHQ)
        {
            Outposts.AddRange(outposts);
            ImmuneHQ = immuneHQ;
            InfectionHQ = infectionHQ;
            Tickets[0] = Tickets[1] = startTickets;
            if (Application.isMobilePlatform)
            {
                maxImmuneBots = 7;
                maxGerms = 12;
            }

            // Opening squads.
            for (int i = 0; i < 6; i++) SpawnBot();
            for (int i = 0; i < 8; i++) SpawnGerm();
        }

        public int Held(Team team) => Outposts.Count(o => o.Owner == team);

        void Update()
        {
            var gm = GameManager.I;
            if (!gm || gm.State != GameState.Playing) return;
            float dt = Time.deltaTime;

            bleedTimer += dt;
            if (bleedTimer >= bleedInterval)
            {
                bleedTimer = 0f;
                int diff = Held(Team.Immune) - Held(Team.Infection);
                if (diff > 0) Tickets[(int)Team.Infection] -= diff >= 3 ? 2 : 1;
                else if (diff < 0) Tickets[(int)Team.Immune] -= diff <= -3 ? 2 : 1;
                // Holding every outpost drains the enemy fast.
                if (Held(Team.Infection) == 0 && Held(Team.Immune) == Outposts.Count) Tickets[(int)Team.Infection] -= 3;
                if (Held(Team.Immune) == 0 && Held(Team.Infection) == Outposts.Count) Tickets[(int)Team.Immune] -= 3;
            }

            reinforceTimer += dt;
            if (reinforceTimer >= reinforceInterval)
            {
                reinforceTimer = 0f;
                if (ImmuneBot.All.Count < maxImmuneBots && Tickets[(int)Team.Immune] > 0) SpawnBot();
                if (Germ.Alive.Count < maxGerms - 2 && Tickets[(int)Team.Infection] > 0) SpawnGermSquad();
            }

            // Late in the fight the hive throws its boss at the front.
            if (!bossSpawned && Tickets[(int)Team.Infection] <= bossTicketThreshold)
            {
                bossSpawned = true;
                var hive = InfectionHQ.transform.position;
                GermFactory.SporeMother(new Vector3(hive.x, 6f, hive.z - 6f), bossHealth);
                Announce("THE SPORE MOTHER HAS AWAKENED");
                KillFeed.Add("The Spore Mother is coming from the hive", new Color(0.95f, 0.5f, 1f));
                Sfx.UI(() => Sfx.CaptureBad, 0.8f, 0f, 0.6f);
            }

            CheckEnd(gm);
        }

        public void OnBossDied(int ticketValue)
        {
            Tickets[(int)Team.Infection] = Mathf.Max(0, Tickets[(int)Team.Infection] - ticketValue);
            Announce("THE SPORE MOTHER IS DEAD");
            KillFeed.Add($"Spore Mother destroyed: infection loses {ticketValue} tickets", new Color(0.95f, 0.5f, 1f));
            Sfx.UI(() => Sfx.CaptureGood, 0.9f, 0f, 0.8f);
        }

        void CheckEnd(GameManager gm)
        {
            if (Tickets[(int)Team.Infection] <= 0)
            {
                Tickets[(int)Team.Infection] = 0;
                gm.Win("The infection has run out of reinforcements. The lung is ours.");
            }
            else if (Tickets[(int)Team.Immune] <= 0)
            {
                Tickets[(int)Team.Immune] = 0;
                gm.Lose("The Immune Corps has run out of reinforcements.");
            }
        }

        public void OnUnitDied(Team team)
        {
            Tickets[(int)team] = Mathf.Max(0, Tickets[(int)team] - 1);
        }

        public void OnOutpostChanged(Outpost outpost, Team? before)
        {
            if (outpost.Owner == Team.Immune)
            {
                Announce($"Outpost {outpost.letter} captured");
                KillFeed.Add($"Outpost {outpost.letter} captured", new Color(0.45f, 0.75f, 1f));
                Sfx.UI(() => Sfx.CaptureGood, 0.7f, 0f);
                var gm = GameManager.I;
                if (gm && gm.Player && !gm.Player.Hp.IsDead && outpost.Contains(gm.Player.transform.position)) KillFeed.AddScore(250);
            }
            else if (outpost.Owner == Team.Infection)
            {
                Announce($"Outpost {outpost.letter} lost to the infection");
                KillFeed.Add($"Outpost {outpost.letter} lost", new Color(0.55f, 0.95f, 0.25f));
                Sfx.UI(() => Sfx.CaptureBad, 0.7f, 0f);
            }
            else if (before == Team.Immune) Announce($"Outpost {outpost.letter} is being taken");
            else Announce($"Outpost {outpost.letter} neutralized");
        }

        void Announce(string text)
        {
            LastEvent = text;
            LastEventTime = Time.time;
        }

        // ---- Objectives --------------------------------------------------

        /// <summary>Picks a point for an AI unit to move to: mostly the nearest outpost its team doesn't hold,
        /// with some units assigned to defend contested or threatened posts.</summary>
        public Vector3 ObjectiveFor(Team team, Vector3 from, int seed)
        {
            Outpost pick = null;
            bool defender = seed % 4 == 0;

            if (defender)
            {
                pick = Outposts.Where(o => o.Owner == team && (o.Contested || Mathf.Abs(o.Control) < 0.99f))
                    .OrderBy(o => (o.transform.position - from).sqrMagnitude).FirstOrDefault();
            }

            if (!pick)
            {
                var targets = Outposts.Where(o => o.Owner != team).ToList();
                if (targets.Count > 0)
                {
                    // Spread attackers over the two nearest targets.
                    targets.Sort((a, b) => (a.transform.position - from).sqrMagnitude.CompareTo((b.transform.position - from).sqrMagnitude));
                    pick = targets[Mathf.Min(seed % 2, targets.Count - 1)];
                }
            }

            if (!pick) pick = team == Team.Immune ? InfectionHQ : ImmuneHQ;

            float angle = (seed % 360) * Mathf.Deg2Rad;
            float r = pick.radius * (0.25f + 0.5f * ((seed / 360) % 10) / 10f);
            return pick.transform.position + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
        }

        /// <summary>Nearest place a wounded unit can heal: a held outpost or its HQ.</summary>
        public Vector3 NearestSafePoint(Team team, Vector3 from)
        {
            Outpost best = team == Team.Immune ? ImmuneHQ : InfectionHQ;
            float bestSqr = (best.transform.position - from).sqrMagnitude;
            foreach (var o in Outposts)
            {
                if (o.Owner != team || o.Contested) continue;
                float d = (o.transform.position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = o; }
            }
            return best.transform.position;
        }

        // ---- Spawning ----------------------------------------------------

        /// <summary>HQ first, then outposts the team holds that aren't contested.</summary>
        public List<Outpost> SpawnOptions(Team team)
        {
            var list = new List<Outpost> { team == Team.Immune ? ImmuneHQ : InfectionHQ };
            list.AddRange(Outposts.Where(o => o.Owner == team && !o.Contested));
            return list;
        }

        /// <summary>The held outpost closest to an enemy or neutral post: the front line.</summary>
        Outpost FrontSpawn(Team team)
        {
            var options = SpawnOptions(team);
            var enemyPosts = Outposts.Where(o => o.Owner != team).ToList();
            if (enemyPosts.Count == 0 || options.Count == 1) return options[options.Count > 1 ? Random.Range(1, options.Count) : 0];
            return options.OrderBy(o => enemyPosts.Min(e => (e.transform.position - o.transform.position).sqrMagnitude)).First();
        }

        static Vector3 Around(Outpost o, float y)
        {
            var off = Random.insideUnitCircle * o.radius * 0.6f;
            return new Vector3(o.transform.position.x + off.x, y, o.transform.position.z + off.y);
        }

        public Vector3 PlayerSpawn(out Quaternion facing)
        {
            var options = SpawnOptions(Team.Immune);
            Outpost o = PlayerSpawnChoice >= 0 && PlayerSpawnChoice < options.Count ? options[PlayerSpawnChoice] : FrontSpawn(Team.Immune);
            PlayerSpawnChoice = -1;
            var pos = Around(o, 0.1f);
            Vector3 toEnemy = InfectionHQ.transform.position - pos;
            toEnemy.y = 0f;
            facing = Quaternion.LookRotation(toEnemy.sqrMagnitude > 0.01f ? toEnemy : Vector3.forward);
            return pos;
        }

        void SpawnBot()
        {
            var o = Random.value < 0.6f ? FrontSpawn(Team.Immune) : RandomOption(Team.Immune);
            ImmuneBot.Create(Around(o, 0f));
        }

        /// <summary>Germs arrive in squads of 3 that share one objective, so they hit outposts together.</summary>
        void SpawnGermSquad()
        {
            var o = Random.value < 0.6f ? FrontSpawn(Team.Infection) : RandomOption(Team.Infection);
            int seed = Random.Range(0, 100000);
            for (int i = 0; i < 3; i++)
            {
                var g = SpawnGerm(o);
                if (g) g.SetSquadSeed(seed + i * 120);
            }
        }

        Germ SpawnGerm(Outpost o = null)
        {
            if (!o) o = Random.value < 0.6f ? FrontSpawn(Team.Infection) : RandomOption(Team.Infection);
            var pos = Around(o, Random.Range(1.2f, 3f));
            float roll = Random.value;
            int brutes = Germ.Alive.Count(g => g is Brute);
            if (roll < bruteShare && brutes < maxBrutes) return GermFactory.Brute(pos);
            if (roll < bruteShare + spitterShare) return GermFactory.Spitter(pos);
            return GermFactory.Swarmer(pos);
        }

        Outpost RandomOption(Team team)
        {
            var options = SpawnOptions(team);
            return options[Random.Range(0, options.Count)];
        }
    }
}
