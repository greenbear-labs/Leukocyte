using System.Linq;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Boss: a huge fungal pod. Drifts toward the fight, births Swarmers,
    /// fires spore volleys and, when wounded, slams out a shockwave. Only one at a time.</summary>
    public class SporeMother : Germ
    {
        public static SporeMother Current { get; private set; }

        public float birthInterval = 7f;
        public int birthCount = 3;
        public float volleyInterval = 3.2f;
        public int volleyGlobs = 5;
        public float shockwaveInterval = 9f;
        public float shockwaveRadius = 9f;
        public float shockwaveDamage = 35f;
        public int ticketValue = 25;

        float birthTimer = 4f;
        float volleyTimer = 2f;
        float shockTimer = 6f;
        Transform body;
        Vector3 bodyScale;

        protected override void Awake()
        {
            base.Awake();
            speed = 2.2f;
            aggroRange = 45f;
            hostDamage = 40f;
            turnSpeed = 1.5f;
            BloodColor = new Color(0.8f, 0.4f, 0.9f);
            body = transform.Find("Pod");
            if (body) bodyScale = body.localScale;
            Current = this;
            Hp.Died += () => { if (ConquestManager.I) ConquestManager.I.OnBossDied(ticketValue); };
        }

        void OnDestroy() { if (Current == this) Current = null; }

        protected override void Think(float dt)
        {
            var pos = transform.position;
            if (pos.y < 3f) { pos.y = 3f; transform.position = pos; }
            if (body) body.localScale = bodyScale * (1f + 0.04f * Mathf.Sin(Time.time * 1.7f));

            birthTimer -= dt;
            volleyTimer -= dt;
            shockTimer -= dt;

            if (birthTimer <= 0f)
            {
                birthTimer = birthInterval;
                for (int i = 0; i < birthCount; i++)
                    GermFactory.Swarmer(Level.Clamp(transform.position + Random.onUnitSphere * 3.5f));
                Fader.Burst(transform.position, BloodColor, 10, 0.4f, 6f);
                Sfx.At(() => Sfx.Engulf, transform.position, 1f);
            }

            bool hasTarget = TryGetTarget(out var target, out var aim);

            if (hasTarget && volleyTimer <= 0f && Combatant.HasLineOfSight(transform.position + (aim - transform.position).normalized * 3.5f, target))
            {
                volleyTimer = volleyInterval;
                Vector3 muzzle = transform.position + (aim - transform.position).normalized * 3.5f;
                for (int i = 0; i < volleyGlobs; i++)
                {
                    Vector3 lead = aim + target.Velocity * 0.4f + Random.insideUnitSphere * 2.5f;
                    AcidGlob.Launch(muzzle, (lead - muzzle).normalized * 17f, 10f);
                }
                Sfx.At(() => Sfx.Spit, muzzle, 1f);
            }

            // Below half health she starts slamming anything that gets close.
            if (Hp.Current < Hp.max * 0.5f && shockTimer <= 0f)
            {
                bool near = Combatant.All.Any(c => c.Alive && c.team == Team.Immune &&
                    (c.transform.position - transform.position).sqrMagnitude < shockwaveRadius * shockwaveRadius);
                if (near) Shockwave();
            }

            if (hasTarget)
            {
                Vector3 to = aim - transform.position;
                float dist = to.magnitude;
                Vector3 dir = to / Mathf.Max(dist, 0.01f);
                Steer(dist > 14f ? dir : Vector3.zero, speed, dt, dir);
            }
            else GoToObjective(dt);
        }

        void Shockwave()
        {
            shockTimer = shockwaveInterval;
            Vector3 p = transform.position;
            foreach (var c in Combatant.All.ToArray())
            {
                if (!c.Alive || c.team == Team.Infection) continue;
                float d = Vector3.Distance(c.transform.position, p);
                if (d > shockwaveRadius) continue;
                c.Hp.Damage(shockwaveDamage * Mathf.Lerp(1f, 0.4f, d / shockwaveRadius), p);
            }
            Fader.Burst(p, BloodColor, 24, 0.6f, 14f);
            Sfx.At(() => Sfx.Boom, p, 1f);
            var gm = GameManager.I;
            if (gm && gm.Player)
                gm.Player.AddShake(Mathf.Clamp01(1f - Vector3.Distance(gm.Player.transform.position, p) / 30f) * 0.9f);
        }
    }
}
