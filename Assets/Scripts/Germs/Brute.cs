using System.Linq;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Fungal hulk. Slow and tough; winds up and slams the ground, hurting everything nearby.
    /// The swelling wind-up is the tell: back off or burst it.</summary>
    public class Brute : Germ
    {
        public float slamRadius = 4.5f;
        public float slamDamage = 30f;
        public float slamWindup = 0.8f;
        public float slamCooldown = 2.5f;
        public float triggerRange = 3.5f;

        float cooldown;
        float windup = -1f;
        Transform body;
        Vector3 bodyScale;

        protected override void Awake()
        {
            base.Awake();
            speed = 2.6f;
            aggroRange = 30f;
            hostDamage = 20f;
            turnSpeed = 3f;
            BloodColor = new Color(0.9f, 0.6f, 0.2f);
            body = transform.Find("Body");
            if (body) bodyScale = body.localScale;
        }

        protected override void Think(float dt)
        {
            cooldown -= dt;
            var pos = transform.position;
            if (pos.y < 1.3f) { pos.y = 1.3f; transform.position = pos; }

            if (windup >= 0f)
            {
                windup += dt;
                float k = windup / slamWindup;
                if (body) body.localScale = bodyScale * (1f + 0.25f * k);
                if (windup >= slamWindup) Slam();
                return;
            }

            if (!TryGetTarget(out var target, out var aim))
            {
                GoToObjective(dt);
                return;
            }

            Vector3 to = aim - transform.position;
            float dist = to.magnitude;
            Vector3 dir = to / Mathf.Max(dist, 0.001f);

            if (dist < triggerRange && cooldown <= 0f)
            {
                windup = 0f;
                return;
            }
            Steer(dir, dist < triggerRange ? 0.5f : speed, dt);
        }

        void Slam()
        {
            windup = -1f;
            cooldown = slamCooldown;
            if (body) body.localScale = bodyScale;

            Vector3 p = transform.position;
            foreach (var c in Combatant.All.ToArray())
            {
                if (!c.Alive || c.team == Team.Infection) continue;
                float d = Vector3.Distance(c.transform.position, new Vector3(p.x, c.transform.position.y, p.z));
                if (d > slamRadius) continue;
                c.Hp.Damage(slamDamage * Mathf.Lerp(1f, 0.5f, d / slamRadius), p);
            }

            Fader.Burst(p + Vector3.down, BloodColor, 14, 0.4f, 9f);
            Sfx.At(() => Sfx.Slam, p, 1f);
            var gm = GameManager.I;
            if (gm && gm.Player)
            {
                float dist = Vector3.Distance(gm.Player.transform.position, p);
                gm.Player.AddShake(Mathf.Clamp01(1f - dist / 15f) * 0.6f);
            }
        }
    }
}
