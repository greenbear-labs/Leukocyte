using System.Linq;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Leukocyte trooper abilities, configured by class.
    /// G: the class ability. Cytokine Burst (thrown area blast) or Antibody Tag (marks enemies in a cone).
    /// F: Phagocyte Grab, engulfs a weakened germ in front of you to heal, or punches a healthy one.</summary>
    public class PlayerAbilities : MonoBehaviour
    {
        public int burstCharges = 2;
        public float burstRecharge = 12f;
        public float grabCooldown = 4f;
        public float grabRange = 2.8f;
        public float grabHeal = 30f;
        public float grabDamage = 40f;
        [Range(0f, 1f)] public float engulfThreshold = 0.4f;

        public Ability ability = Ability.CytokineBurst;
        public float tagRange = 45f;
        public float tagConeDegrees = 25f;
        public float tagSeconds = 10f;

        public int BurstCharges { get; private set; }
        public string AbilityName => ability == Ability.AntibodyTag ? "ANTIBODY TAG" : "CYTOKINE BURST";
        public float BurstRechargeProgress => 1f - burstTimer / burstRecharge;
        public float GrabReady => grabTimer <= 0f ? 1f : 1f - grabTimer / grabCooldown;

        PlayerController owner;
        float burstTimer;
        float grabTimer;

        public void Init(PlayerController player)
        {
            owner = player;
            BurstCharges = burstCharges;
        }

        public void Configure(ClassProfile profile)
        {
            ability = profile.ability;
            burstCharges = profile.abilityCharges;
            burstRecharge = profile.abilityRecharge;
            grabHeal = profile.grabHeal;
            engulfThreshold = profile.engulfThreshold;
            Refill();
        }

        public void Refill()
        {
            BurstCharges = burstCharges;
            burstTimer = 0f;
            grabTimer = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (BurstCharges < burstCharges)
            {
                if (burstTimer <= 0f) burstTimer = burstRecharge;
                burstTimer -= dt;
                if (burstTimer <= 0f) { BurstCharges++; burstTimer = BurstCharges < burstCharges ? burstRecharge : 0f; }
            }
            grabTimer = Mathf.Max(0f, grabTimer - dt);

            var gm = GameManager.I;
            if (!gm || gm.State != GameState.Playing || owner.Hp.IsDead || !owner.InputEnabled) return;

            if (GameInput.Ability)
            {
                if (ability == Ability.AntibodyTag) Tag();
                else ThrowBurst();
            }
            if (GameInput.Grab) Grab();
            if (GameInput.Squad) SquadOrder();
        }

        void ThrowBurst()
        {
            if (BurstCharges <= 0) { Sfx.UI(() => Sfx.Denied, 0.5f); return; }
            BurstCharges--;
            var cam = owner.Cam.transform;
            Vector3 velocity = cam.forward * 22f + Vector3.up * 4f + owner.Velocity * 0.5f;
            CytokineBurst.Launch(cam.position + cam.forward * 0.8f, velocity, owner.GetComponent<Collider>());
            Sfx.UI(() => Sfx.Throw, 0.6f);
        }

        public int squadSize = 4;
        public float recruitRange = 35f;

        public int SquadCount
        {
            get { int n = 0; foreach (var b in ImmuneBot.All) if (b.Following) n++; return n; }
        }

        /// <summary>Q: call the nearest troopers to follow you, or send your squad back to the objectives.</summary>
        void SquadOrder()
        {
            if (ImmuneBot.All.Count == 0) { Sfx.UI(() => Sfx.Denied, 0.5f); return; }
            if (SquadCount > 0)
            {
                foreach (var b in ImmuneBot.All) b.Following = false;
                KillFeed.Add("Squad: push the objectives!", new Color(0.45f, 0.75f, 1f));
                Sfx.UI(() => Sfx.CaptureGood, 0.4f, 0f, 0.8f);
                return;
            }

            var candidates = ImmuneBot.All
                .Where(b => !b.Retreating && (b.transform.position - transform.position).sqrMagnitude < recruitRange * recruitRange)
                .OrderBy(b => (b.transform.position - transform.position).sqrMagnitude)
                .Take(squadSize).ToList();
            if (candidates.Count == 0) { KillFeed.Add("No troopers in range", new Color(1f, 0.6f, 0.5f)); Sfx.UI(() => Sfx.Denied, 0.5f); return; }
            for (int i = 0; i < candidates.Count; i++)
            {
                candidates[i].Following = true;
                candidates[i].SquadSlot = i;
            }
            KillFeed.Add($"Squad: {candidates.Count} troopers on me!", new Color(0.45f, 0.75f, 1f));
            Sfx.UI(() => Sfx.CaptureGood, 0.4f, 0f, 1.2f);
        }

        void Tag()
        {
            if (BurstCharges <= 0) { Sfx.UI(() => Sfx.Denied, 0.5f); return; }
            var cam = owner.Cam.transform;
            int tagged = 0;
            float cos = Mathf.Cos(tagConeDegrees * Mathf.Deg2Rad);
            foreach (var c in Combatant.All)
            {
                if (!c.Alive || c.team == Team.Immune) continue;
                Vector3 to = c.AimPoint - cam.position;
                float d = to.magnitude;
                if (d > tagRange || Vector3.Dot(to / d, cam.forward) < cos) continue;
                if (!Combatant.HasLineOfSight(cam.position, c)) continue;
                c.Mark(tagSeconds);
                tagged++;
            }
            if (tagged == 0) { Sfx.UI(() => Sfx.Denied, 0.5f); return; }
            BurstCharges--;
            KillFeed.Add($"Antibody Tag: {tagged} marked", new Color(1f, 0.45f, 0.5f));
            KillFeed.AddScore(10 * tagged);
            Sfx.UI(() => Sfx.CaptureGood, 0.5f, 0f, 1.5f);
        }

        void Grab()
        {
            if (grabTimer > 0f) { Sfx.UI(() => Sfx.Denied, 0.5f); return; }
            grabTimer = grabCooldown;

            var cam = owner.Cam.transform;
            Vector3 center = cam.position + cam.forward * (grabRange * 0.6f);
            Combatant best = null;
            float bestSqr = float.MaxValue;
            foreach (var col in Physics.OverlapSphere(center, grabRange * 0.7f, ~0, QueryTriggerInteraction.Ignore))
            {
                var c = col.GetComponentInParent<Combatant>();
                if (!c || c.team == Team.Immune || !c.Alive) continue;
                float d = (c.transform.position - center).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = c; }
            }

            owner.Rifle.Punch();
            if (!best) return;

            bool engulf = best.Hp.Current <= best.Hp.max * engulfThreshold || best.GetComponent<Swarmer>() && best.Hp.Current <= grabDamage;
            if (engulf)
            {
                best.Hp.Damage(best.Hp.Current + 1f, transform.position);
                owner.Hp.Heal(grabHeal);
                KillFeed.PlayerKill(best, "Engulfed");
                if (GameManager.I.Hud) GameManager.I.Hud.ShowHitMarker(true);
                Sfx.UI(() => Sfx.Engulf, 0.9f);
            }
            else
            {
                float dmg = grabDamage * best.DamageTakenMultiplier;
                bool kill = best.Hp.Current <= dmg;
                best.Hp.Damage(dmg, transform.position);
                best.transform.position = Level.Clamp(best.transform.position + cam.forward * 3f);
                if (kill) KillFeed.PlayerKill(best, "Crushed");
                if (GameManager.I.Hud) GameManager.I.Hud.ShowHitMarker(kill);
                Sfx.UI(() => Sfx.Squelch, 0.7f);
            }
            owner.AddShake(0.25f);
        }
    }

    /// <summary>Thrown cytokine grenade: bounces, then bursts and damages every enemy in range.</summary>
    public class CytokineBurst : MonoBehaviour
    {
        public float fuse = 1.4f;
        public float radius = 7f;
        public float damage = 110f;

        static readonly Color BurstColor = new Color(1f, 0.85f, 0.35f);

        public static void Launch(Vector3 position, Vector3 velocity, Collider ignore)
        {
            var go = Greybox.Prim(PrimitiveType.Sphere, "CytokineBurst", Level.Root, Vector3.zero, Vector3.one * 0.3f, BurstColor, ColliderMode.Keep, 2f);
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var col = go.GetComponent<Collider>();
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = 0.2f;
            rb.linearVelocity = velocity;
            col.material = new PhysicsMaterial { bounciness = 0.45f, bounceCombine = PhysicsMaterialCombine.Maximum, dynamicFriction = 0.6f };
#else
            rb.drag = 0.2f;
            rb.velocity = velocity;
            col.material = new PhysicMaterial { bounciness = 0.45f, bounceCombine = PhysicMaterialCombine.Maximum, dynamicFriction = 0.6f };
#endif
            if (ignore) Physics.IgnoreCollision(col, ignore);
            go.AddComponent<CytokineBurst>();
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 3f;
            l.intensity = 2f;
            l.color = BurstColor;
        }

        void Update()
        {
            fuse -= Time.deltaTime;
            if (fuse <= 0f) Explode();
        }

        void Explode()
        {
            Vector3 p = transform.position;
            foreach (var c in Combatant.All.ToArray())
            {
                if (!c.Alive || c.team == Team.Immune) continue;
                float d = Vector3.Distance(c.AimPoint, p);
                if (d > radius) continue;
                float dmg = damage * Mathf.Lerp(1f, 0.35f, d / radius) * c.DamageTakenMultiplier;
                bool kill = c.Hp.Current <= dmg;
                c.Hp.Damage(dmg, p);
                if (kill) KillFeed.PlayerKill(c, "Burst");
            }

            Fader.Burst(p, BurstColor, 18, 0.5f, 12f);
            Fader.Burst(p, Color.white, 6, 0.8f, 4f);
            var flash = new GameObject("BurstFlash").AddComponent<Light>();
            flash.transform.SetParent(Level.Root, false);
            flash.transform.position = p;
            flash.type = LightType.Point;
            flash.range = radius * 2.5f;
            flash.intensity = 6f;
            flash.color = BurstColor;
            flash.gameObject.AddComponent<Fader>().life = 0.15f;

            Sfx.At(() => Sfx.Boom, p, 1f);
            var gm = GameManager.I;
            if (gm && gm.Player)
            {
                float dist = Vector3.Distance(gm.Player.transform.position, p);
                gm.Player.AddShake(Mathf.Clamp01(1f - dist / 25f) * 0.8f);
            }
            Destroy(gameObject);
        }
    }
}
