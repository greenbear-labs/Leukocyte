using UnityEngine;

namespace Leukocyte
{
    /// <summary>Virus that keeps its distance, strafes, and lobs acid globs at its target.</summary>
    public class Spitter : Germ
    {
        public float minRange = 12f;
        public float maxRange = 20f;
        public float fireCooldown = 2.2f;
        public float globSpeed = 20f;
        public float globDamage = 12f;

        float fireTimer;
        float strafeSign = 1f;
        float strafeTimer;

        protected override void Awake()
        {
            base.Awake();
            speed = 3.5f;
            aggroRange = 38f;
            hostDamage = 10f;
            BloodColor = new Color(0.7f, 0.3f, 1f);
            fireTimer = Random.Range(1f, 2f);
        }

        protected override void Think(float dt)
        {
            if (!TryGetTarget(out var target, out var aim))
            {
                GoToObjective(dt);
                return;
            }

            Vector3 to = aim - transform.position;
            float dist = to.magnitude;
            Vector3 dir = to / Mathf.Max(dist, 0.001f);

            strafeTimer -= dt;
            if (strafeTimer <= 0f)
            {
                strafeSign = -strafeSign;
                strafeTimer = Random.Range(1.5f, 3f);
            }

            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 move = side * strafeSign * 0.8f;
            if (dist > maxRange) move += dir;
            else if (dist < minRange) move -= dir;
            Steer(move.normalized, speed, dt, dir);

            fireTimer -= dt;
            Vector3 muzzle = transform.position + dir * 1.1f;
            if (fireTimer <= 0f && dist < aggroRange && Combatant.HasLineOfSight(muzzle, target))
            {
                fireTimer = fireCooldown;
                Vector3 lead = aim + target.Velocity * (dist / globSpeed) * 0.5f;
                AcidGlob.Launch(muzzle, (lead - muzzle).normalized * globSpeed, globDamage);
                Sfx.At(() => Sfx.Spit, muzzle, 0.6f);
            }
        }
    }
}
