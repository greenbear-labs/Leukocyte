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

        public readonly int[] Tickets = new int[2];
        public readonly List<Outpost> Outposts = new List<Outpost>();
        public Outpost ImmuneHQ { get; private set; }
        public Outpost InfectionHQ { get; private set; }
        public string LastEvent { get; private set; } = "";
        public float LastEventTime { get; private set; } = -10f;

        /// <summary>Index into PlayerSpawnOptions chosen on the respawn screen.</summary>
        public int PlayerSpawnChoice { get; set; } = -1;

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
                if (Germ.Alive.Count < maxGerms && Tickets[(int)Team.Infection] > 0) SpawnGerm();
            }

            CheckEnd(gm);
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
            if (outpost.Owner == Team.Immune) Announce($"Outpost {outpost.letter} captured");
            else if (outpost.Owner == Team.Infection) Announce($"Outpost {outpost.letter} lost to the infection");
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

        void SpawnGerm()
        {
            var o = Random.value < 0.6f ? FrontSpawn(Team.Infection) : RandomOption(Team.Infection);
            var pos = Around(o, Random.Range(1.2f, 3f));
            if (Random.value < spitterShare) GermFactory.Spitter(pos);
            else GermFactory.Swarmer(pos);
        }

        Outpost RandomOption(Team team)
        {
            var options = SpawnOptions(team);
            return options[Random.Range(0, options.Count)];
        }
    }
}
