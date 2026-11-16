using System.Collections.Generic;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Base germ: floats over the battlefield, fights the nearest hostile it can reach,
    /// and otherwise heads for its objective (a Tissue Node in Artery Defense, an outpost in Tissue War).</summary>
    [RequireComponent(typeof(Health))]
    public abstract class Germ : MonoBehaviour
    {
        public static readonly List<Germ> Alive = new List<Germ>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Alive.Clear(); }

        public float speed = 5f;
        public float aggroRange = 30f;
        public float hostDamage = 6f;
        public float turnSpeed = 8f;
        public Color BloodColor = new Color(0.4f, 0.9f, 0.2f);

        public Health Hp { get; private set; }
        public Combatant Self { get; private set; }

        /// <summary>Current enemy being fought, refreshed a few times per second.</summary>
        protected Combatant Target { get; private set; }

        Renderer[] renderers;
        Color[] baseColors;
        float flashTimer;
        bool flashing;
        float bobPhase;
        float retargetTimer;
        protected int unitSeed;

        protected virtual void Awake()
        {
            Hp = GetComponent<Health>();
            Self = GetComponent<Combatant>();
            Hp.Died += OnDied;
            Hp.Damaged += _ => flashTimer = 0.08f;
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].material.color;
            bobPhase = Random.value * 10f;
            unitSeed = Random.Range(0, 100000);
        }

        void OnEnable() { Alive.Add(this); }
        void OnDisable() { Alive.Remove(this); }

        void Update()
        {
            var gm = GameManager.I;
            if (!gm || gm.State != GameState.Playing) return;

            float dt = Time.deltaTime;
            retargetTimer -= dt;
            if (retargetTimer <= 0f || (Target && !Target.Alive))
            {
                retargetTimer = 0.4f;
                Target = Combatant.NearestHostile(Team.Infection, transform.position, aggroRange, false);
            }

            Think(dt);
            CheckTissueContact();
            UpdateFlash(dt);
        }

        protected abstract void Think(float dt);

        // ---- Targeting ---------------------------------------------------

        protected bool TryGetTarget(out Combatant target, out Vector3 aim)
        {
            target = Target;
            aim = target ? target.AimPoint : default(Vector3);
            return target && target.Alive;
        }

        /// <summary>Where to go when there is no one to fight.</summary>
        protected bool TryGetObjective(out Vector3 point)
        {
            point = default(Vector3);
            if (TissueNode.All.Count > 0)
            {
                TissueNode best = null;
                float bestSqr = float.MaxValue;
                foreach (var n in TissueNode.All)
                {
                    float d = (n.transform.position - transform.position).sqrMagnitude;
                    if (d < bestSqr) { bestSqr = d; best = n; }
                }
                point = best.transform.position;
                return true;
            }
            if (ConquestManager.I)
            {
                point = ConquestManager.I.ObjectiveFor(Team.Infection, transform.position, unitSeed) + Vector3.up * 1.5f;
                return true;
            }
            return false;
        }

        /// <summary>Moves to the objective and loiters once there.</summary>
        protected void GoToObjective(float dt)
        {
            if (!TryGetObjective(out var point)) return;
            Vector3 to = point - transform.position;
            if (to.sqrMagnitude < 4f) Steer(Vector3.zero, 0f, dt);
            else Steer(to.normalized, speed, dt);
        }

        void CheckTissueContact()
        {
            foreach (var n in TissueNode.All)
            {
                if ((n.transform.position - transform.position).sqrMagnitude < (n.radius + 0.7f) * (n.radius + 0.7f))
                {
                    n.Infect(hostDamage);
                    Fader.Burst(transform.position, BloodColor, 8, 0.25f, 5f);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // ---- Movement ----------------------------------------------------

        /// <summary>Move in a direction with separation, wall/obstacle avoidance and a floaty bob.</summary>
        protected void Steer(Vector3 moveDir, float moveSpeed, float dt, Vector3? faceDir = null)
        {
            Vector3 v = moveDir * moveSpeed + Separation() * 3f;

            if (v.sqrMagnitude > 0.01f &&
                Physics.SphereCast(transform.position, 0.5f, v.normalized, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore) &&
                !hit.collider.GetComponentInParent<Combatant>() &&
                !hit.collider.GetComponentInParent<TissueNode>())
            {
                v += hit.normal * Mathf.Max(moveSpeed, 2f) * 1.5f;
            }

            v.y += Mathf.Sin(Time.time * 3f + bobPhase) * 0.6f;
            transform.position = Level.Clamp(transform.position + v * dt);

            Vector3 face = faceDir ?? moveDir;
            if (face.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), turnSpeed * dt);
        }

        Vector3 Separation()
        {
            Vector3 push = Vector3.zero;
            foreach (var g in Alive)
            {
                if (g == this) continue;
                Vector3 d = transform.position - g.transform.position;
                float m = d.magnitude;
                if (m < 1.6f && m > 0.001f) push += d / m * (1.6f - m);
            }
            return push;
        }

        // ---- Feedback ----------------------------------------------------

        void UpdateFlash(float dt)
        {
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (!flashing)
                {
                    foreach (var r in renderers) r.material.color = Color.white;
                    flashing = true;
                }
            }
            else if (flashing)
            {
                for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = baseColors[i];
                flashing = false;
            }
        }

        void OnDied()
        {
            if (ConquestManager.I) ConquestManager.I.OnUnitDied(Team.Infection);
            Fader.Burst(transform.position, BloodColor, 10, 0.25f, 6f);
            Sfx.At(() => Sfx.Squelch, transform.position, 0.5f);
            Destroy(gameObject);
        }
    }
}
