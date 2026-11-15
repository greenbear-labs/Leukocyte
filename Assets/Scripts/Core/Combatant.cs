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
