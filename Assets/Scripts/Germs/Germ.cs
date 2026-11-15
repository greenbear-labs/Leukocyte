using System.Collections.Generic;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Base germ: floats through the artery, hunts the player when close,
    /// otherwise heads for the nearest Tissue Node to infect it.</summary>
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

        protected PlayerController Player => GameManager.I ? GameManager.I.Player : null;

        Renderer[] renderers;
        Color[] baseColors;
        float flashTimer;
        bool flashing;
        float bobPhase;

        protected virtual void Awake()
        {
            Hp = GetComponent<Health>();
            Hp.Died += OnDied;
            Hp.Damaged += _ => flashTimer = 0.08f;
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].material.color;
            bobPhase = Random.value * 10f;
        }

        void OnEnable() { Alive.Add(this); }
        void OnDisable() { Alive.Remove(this); }

        void Update()
        {
            var gm = GameManager.I;
            if (!gm || gm.State != GameState.Playing) return;

            float dt = Time.deltaTime;
            Think(dt);
            CheckTissueContact();
            UpdateFlash(dt);
        }

        protected abstract void Think(float dt);

        // ---- Targeting ---------------------------------------------------

        protected bool TryGetPlayerTarget(float range, out Vector3 target)
        {
            target = default(Vector3);
            var p = Player;
            if (!p || p.Hp.IsDead) return false;
            target = p.transform.position + Vector3.up * 1.1f;
            return (target - transform.position).sqrMagnitude < range * range;
        }

        protected TissueNode NearestNode()
        {
            TissueNode best = null;
            float bestSqr = float.MaxValue;
            foreach (var n in TissueNode.All)
            {
                float d = (n.transform.position - transform.position).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = n; }
            }
            return best;
        }

        protected bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            if (!Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponent<PlayerController>() != null;
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
                !hit.collider.GetComponentInParent<Germ>() &&
                !hit.collider.GetComponentInParent<TissueNode>() &&
                !hit.collider.GetComponent<PlayerController>())
            {
                v += hit.normal * moveSpeed * 1.5f;
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
            if (GameManager.I) GameManager.I.Kills++;
            Fader.Burst(transform.position, BloodColor, 10, 0.25f, 6f);
            Destroy(gameObject);
        }
    }
}
