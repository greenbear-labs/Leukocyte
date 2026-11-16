using System.Collections.Generic;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Allied Leukocyte trooper AI. Pushes outposts with the squad, engages germs in sight,
    /// and fires short bursts of enzyme bolts with distance-based accuracy.</summary>
    public class ImmuneBot : MonoBehaviour
    {
        public static readonly List<ImmuneBot> All = new List<ImmuneBot>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public float speed = 5.2f;
        public float sightRange = 42f;
        public float preferredRange = 16f;
        public float damage = 7f;
        public float shotInterval = 0.22f;
        public float turnSpeed = 8f;

        static readonly Color BoltColor = new Color(0.4f, 0.95f, 1f);

        Health hp;
        Combatant target;
        Transform muzzle;
        float retargetTimer;
        float shotTimer;
        int burstLeft;
        float burstPause;
        float strafeSign = 1f;
        float strafeTimer;
        int unitSeed;
        Renderer[] renderers;
        Color[] baseColors;
        float flashTimer;
        bool retreating;

        /// <summary>Set by the player's squad order: this trooper escorts the player.</summary>
        public bool Following { get; set; }
        public int SquadSlot { get; set; }
        public bool Retreating => retreating;

        public static ImmuneBot Create(Vector3 position)
        {
            var go = new GameObject("ImmuneTrooper");
            go.transform.SetParent(Level.Root, false);
            go.transform.position = position;

            var armor = new Color(0.82f, 0.9f, 1f);
            var trim = new Color(0.35f, 0.6f, 0.95f);
            Greybox.Prim(PrimitiveType.Capsule, "Body", go.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.75f, 0.85f, 0.75f), armor);
            Greybox.Prim(PrimitiveType.Sphere, "Head", go.transform, new Vector3(0f, 1.8f, 0f), Vector3.one * 0.5f, Color.white, ColliderMode.None);
            Greybox.Prim(PrimitiveType.Cube, "Visor", go.transform, new Vector3(0f, 1.82f, 0.2f), new Vector3(0.36f, 0.12f, 0.15f), BoltColor, ColliderMode.None, 2f);
            Greybox.Prim(PrimitiveType.Cube, "Pauldrons", go.transform, new Vector3(0f, 1.4f, 0f), new Vector3(0.95f, 0.18f, 0.45f), trim, ColliderMode.None);
            var gun = Greybox.Prim(PrimitiveType.Cube, "Rifle", go.transform, new Vector3(0.28f, 1.2f, 0.45f), new Vector3(0.1f, 0.12f, 0.6f), new Color(0.6f, 0.7f, 0.8f), ColliderMode.None);

            go.AddComponent<Health>().SetMax(100f);
            var c = go.AddComponent<Combatant>();
            c.team = Team.Immune;
            c.aimHeight = 1.2f;

            var bot = go.AddComponent<ImmuneBot>();
            bot.muzzle = new GameObject("Muzzle").transform;
            bot.muzzle.SetParent(gun.transform, false);
            bot.muzzle.localPosition = new Vector3(0f, 0f, 0.55f);
            return bot;
        }

        void Awake()
        {
            hp = GetComponent<Health>();
            hp.Died += OnDied;
            hp.Damaged += _ => flashTimer = 0.08f;
            unitSeed = Random.Range(0, 100000);
            strafeTimer = Random.Range(0.5f, 2f);
        }

        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].material.color;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        Vector3 Eye => transform.position + Vector3.up * 1.6f;

        void Update()
        {
            var gm = GameManager.I;
            if (!gm || gm.State != GameState.Playing || hp.IsDead) return;
            float dt = Time.deltaTime;

            retargetTimer -= dt;
            if (retargetTimer <= 0f || (target && !target.Alive))
            {
                retargetTimer = 0.4f;
                target = Combatant.NearestHostile(Team.Immune, Eye, sightRange, true);
            }

            // Wounded troopers fall back to heal at a held post.
            if (!retreating && hp.Current < hp.max * 0.35f && !Following) retreating = true;
            if (retreating && hp.Current > hp.max * 0.85f) retreating = false;

            if (retreating) Retreat(dt);
            else if (Following && FollowLeash(dt)) { }
            else if (target) Fight(dt);
            else Advance(dt);

            UpdateFlash(dt);
        }

        void Retreat(float dt)
        {
            if (!ConquestManager.I) return;
            Vector3 safe = ConquestManager.I.NearestSafePoint(Team.Immune, transform.position);
            Vector3 to = safe - transform.position;
            to.y = 0f;
            Vector3 face = target ? target.AimPoint - Eye : to;
            if (to.sqrMagnitude > 9f) Move(to.normalized, speed * 1.1f, dt, face);
            if (target) TryShoot(dt, Vector3.Distance(Eye, target.AimPoint));
        }

        /// <summary>Squad follow: stay in formation near the player. Returns false when free to fight nearby.</summary>
        bool FollowLeash(float dt)
        {
            var gm = GameManager.I;
            var player = gm ? gm.Player : null;
            if (!player || player.Hp.IsDead) return false;

            Vector3 back = -player.transform.forward;
            Vector3 side = player.transform.right;
            float lateral = (SquadSlot % 2 == 0 ? 1f : -1f) * (2.5f + 1.5f * (SquadSlot / 2));
            Vector3 slot = player.transform.position + back * (3f + SquadSlot * 0.8f) + side * lateral;
            Vector3 to = slot - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            // Close to the player with an enemy in sight: fight from here.
            if (target && dist < 12f) return false;

            if (dist > 1.5f)
            {
                float s = dist > 10f ? speed * 1.3f : speed;
                Move(to / dist, s, dt, target ? target.AimPoint - Eye : player.transform.forward);
            }
            if (target) TryShoot(dt, Vector3.Distance(Eye, target.AimPoint));
            return true;
        }

        void Advance(float dt)
        {
            if (!ConquestManager.I) return;
            Vector3 point = ConquestManager.I.ObjectiveFor(Team.Immune, transform.position, unitSeed);
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 4f) return;
            Move(to.normalized, speed, dt, to);
        }

        void Fight(float dt)
        {
            Vector3 to = target.AimPoint - Eye;
            float dist = to.magnitude;
            Vector3 flat = new Vector3(to.x, 0f, to.z).normalized;

            strafeTimer -= dt;
            if (strafeTimer <= 0f)
            {
                strafeSign = -strafeSign;
                strafeTimer = Random.Range(1f, 2.5f);
            }
            Vector3 side = Vector3.Cross(Vector3.up, flat);
            Vector3 move = side * strafeSign * 0.7f;
            if (dist > preferredRange + 4f) move += flat;
            else if (dist < preferredRange - 6f) move -= flat * 0.8f;
            Move(move.sqrMagnitude > 0.01f ? move.normalized : Vector3.zero, speed * 0.8f, dt, flat);
            TryShoot(dt, dist);
        }

        void TryShoot(float dt, float dist)
        {
            // Bursts of 3-5 shots.
            if (burstPause > 0f)
            {
                burstPause -= dt;
                return;
            }
            shotTimer -= dt;
            if (shotTimer > 0f) return;
            shotTimer = shotInterval;
            if (burstLeft <= 0) burstLeft = Random.Range(3, 6);
            Shoot(dist);
            if (--burstLeft <= 0) burstPause = Random.Range(0.4f, 0.9f);
        }

        void Shoot(float dist)
        {
            if (!Combatant.HasLineOfSight(Eye, target)) { retargetTimer = 0f; return; }

            float hitChance = Mathf.Lerp(0.65f, 0.2f, dist / sightRange);
            Vector3 aim = target.AimPoint;
            if (Random.value < hitChance) target.Hp.Damage(damage * target.DamageTakenMultiplier, Eye);
            else aim += Random.insideUnitSphere * 1.5f;

            Fader.Tracer(muzzle.position, aim, BoltColor);
            Sfx.At(() => Sfx.BotShot, muzzle.position, 0.25f, 0.15f);
        }

        void Move(Vector3 dir, float moveSpeed, float dt, Vector3 face)
        {
            Vector3 v = dir * moveSpeed;

            // Separation from other troopers.
            foreach (var b in All)
            {
                if (b == this) continue;
                Vector3 d = transform.position - b.transform.position;
                d.y = 0f;
                float m = d.magnitude;
                if (m < 1.5f && m > 0.001f) v += d / m * (1.5f - m) * 3f;
            }

            // Steer around cover.
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            if (v.sqrMagnitude > 0.01f &&
                Physics.SphereCast(origin, 0.45f, v.normalized, out var hit, 2.2f, ~0, QueryTriggerInteraction.Ignore) &&
                !hit.collider.GetComponentInParent<Combatant>())
            {
                Vector3 n = hit.normal;
                n.y = 0f;
                Vector3 slide = Vector3.Cross(Vector3.up, n).normalized;
                if (Vector3.Dot(slide, v) < 0f) slide = -slide;
                v = slide * moveSpeed + n * moveSpeed * 0.5f;
            }

            Vector3 p = transform.position + v * dt;
            p.y = 0f;
            p.x = Mathf.Clamp(p.x, Level.Min.x, Level.Max.x);
            p.z = Mathf.Clamp(p.z, Level.Min.z, Level.Max.z);
            transform.position = p;

            face.y = 0f;
            if (face.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), turnSpeed * dt);
        }

        void UpdateFlash(float dt)
        {
            if (renderers == null) return;
            flashTimer -= dt;
            bool on = flashTimer > 0f;
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = on ? new Color(1f, 0.5f, 0.5f) : baseColors[i];
        }

        void OnDied()
        {
            if (ConquestManager.I) ConquestManager.I.OnUnitDied(Team.Immune);
            KillFeed.Add("Trooper lost", new Color(1f, 0.5f, 0.5f));
            Fader.Burst(transform.position + Vector3.up, new Color(0.85f, 0.9f, 1f), 10, 0.25f, 5f);
            Destroy(gameObject);
        }
    }
}
