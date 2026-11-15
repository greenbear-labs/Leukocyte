using UnityEngine;

namespace Leukocyte
{
    /// <summary>Slow, dodgeable projectile fired by Spitters.</summary>
    public class AcidGlob : MonoBehaviour
    {
        Vector3 velocity;
        float damage;
        float life = 4f;

        static readonly Color GlobColor = new Color(0.75f, 1f, 0.2f);

        public static void Launch(Vector3 position, Vector3 velocity, float damage)
        {
            var go = Greybox.Prim(PrimitiveType.Sphere, "AcidGlob", Level.Root, Vector3.zero,
                Vector3.one * 0.4f, GlobColor, ColliderMode.None, 2f);
            go.transform.position = position;
            var glob = go.AddComponent<AcidGlob>();
            glob.velocity = velocity;
            glob.damage = damage;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (!gm || gm.State != GameState.Playing) return;

            float dt = Time.deltaTime;
            Vector3 step = velocity * dt;
            if (Physics.SphereCast(transform.position, 0.2f, velocity.normalized, out var hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                var victim = hit.collider.GetComponentInParent<Combatant>();
                if (victim && victim.team == Team.Infection)
                {
                    transform.position += step; // pass through other germs
                }
                else
                {
                    if (victim) victim.Hp.Damage(damage);
                    Fader.Burst(hit.point, GlobColor, 5, 0.15f, 3f);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                transform.position += step;
            }

            life -= dt;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
