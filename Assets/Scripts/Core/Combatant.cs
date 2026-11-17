using System.Collections.Generic;
using UnityEngine;

namespace Leukocyte
{
    public enum Team { Immune = 0, Infection = 1 }

    /// <summary>Anything that fights: the player, allied bots and germs. Used for targeting and outpost capture.</summary>
    [RequireComponent(typeof(Health))]
    public class Combatant : MonoBehaviour
    {
        public static readonly List<Combatant> All = new List<Combatant>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public Team team;
        public float aimHeight = 1.1f;

        public Health Hp { get; private set; }
        public Vector3 AimPoint => transform.position + Vector3.up * aimHeight;
        public Vector3 Velocity { get; private set; }
        public bool Alive => Hp && !Hp.IsDead && isActiveAndEnabled;

        Vector3 lastPos;
        float markedUntil = -1f;
        GameObject markVisual;

        /// <summary>Antibody Tag: marked units take extra damage and show through walls on the HUD.</summary>
        public bool Marked => Time.time < markedUntil;
        public float DamageTakenMultiplier => Marked ? 1.5f : 1f;

        public void Mark(float seconds)
        {
            markedUntil = Time.time + seconds;
            if (!markVisual)
            {
                markVisual = Greybox.Prim(PrimitiveType.Cube, "AntibodyTag", transform, Vector3.up * (aimHeight + 1.6f),
                    Vector3.one * 0.35f, new Color(1f, 0.25f, 0.3f), ColliderMode.None, 3f);
                markVisual.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            }
            markVisual.SetActive(true);
        }

        void Awake()
        {
            Hp = GetComponent<Health>();
            lastPos = transform.position;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt > 0f) Velocity = (transform.position - lastPos) / dt;
            lastPos = transform.position;
            if (markVisual && markVisual.activeSelf)
            {
                if (!Marked) markVisual.SetActive(false);
                else markVisual.transform.Rotate(0f, 180f * dt, 0f, Space.World);
            }
        }

        public bool IsHostileTo(Team other) => team != other;

        /// <summary>Nearest living enemy of <paramref name="team"/> within range, optionally requiring line of sight.</summary>
        public static Combatant NearestHostile(Team team, Vector3 from, float range, bool needSight)
        {
            Combatant best = null;
            float bestSqr = range * range;
            foreach (var c in All)
            {
                if (c.team == team || !c.Alive) continue;
                float d = (c.AimPoint - from).sqrMagnitude;
                if (d >= bestSqr) continue;
                if (needSight && !HasLineOfSight(from, c)) continue;
                bestSqr = d;
                best = c;
            }
            return best;
        }

        public static bool HasLineOfSight(Vector3 from, Combatant target)
        {
            if (!Physics.Linecast(from, target.AimPoint, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<Combatant>() == target;
        }
    }
}
