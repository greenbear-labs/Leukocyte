using UnityEngine;

namespace Leukocyte
{
    /// <summary>Small fast bacterium. Rushes in zig-zags and bites at melee range.</summary>
    public class Swarmer : Germ
    {
        public float biteDamage = 8f;
        public float biteRange = 1.7f;
        public float biteCooldown = 0.8f;

        float biteTimer;
        float wobbleSeed;

        protected override void Awake()
        {
            base.Awake();
            speed = 7f;
            aggroRange = 32f;
            hostDamage = 6f;
            BloodColor = new Color(0.4f, 0.95f, 0.2f);
            wobbleSeed = Random.value * 100f;
        }

        protected override void Think(float dt)
        {
            biteTimer -= dt;

            if (!TryGetTarget(out var target, out var aim))
            {
                GoToObjective(dt);
                return;
            }

            Vector3 to = aim - transform.position;
            float dist = to.magnitude;
            Vector3 dir = to / Mathf.Max(dist, 0.001f);

            if (dist < biteRange)
            {
                if (biteTimer <= 0f)
                {
                    target.Hp.Damage(biteDamage);
                    biteTimer = biteCooldown;
                }
                Steer(dir, 1f, dt);
                return;
            }

            if (dist > 4f)
            {
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
                dir = (dir + side * Mathf.Sin(Time.time * 4f + wobbleSeed) * 0.6f).normalized;
            }
            Steer(dir, speed, dt);
        }
    }
}
