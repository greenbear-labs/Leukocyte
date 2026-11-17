using UnityEngine;

namespace Leukocyte
{
    /// <summary>Short-lived visual effect: moves, optionally shrinks, then destroys itself.</summary>
    public class Fader : MonoBehaviour
    {
        public float life = 0.1f;
        public Vector3 velocity;
        public bool shrink;
        float startLife;
        Vector3 startScale;

        void Start()
        {
            startLife = Mathf.Max(0.0001f, life);
            startScale = transform.localScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            transform.position += velocity * dt;
            velocity *= 1f - Mathf.Clamp01(3f * dt);
            if (shrink) transform.localScale = startScale * Mathf.Clamp01(life / startLife);
            if (life <= 0f) Destroy(gameObject);
        }

        public static void Tracer(Vector3 from, Vector3 to, Color color)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.01f) return;
            var go = Greybox.Prim(PrimitiveType.Cube, "Tracer", Level.Root, Vector3.zero,
                new Vector3(0.03f, 0.03f, len), color, ColliderMode.None, 3f);
            go.transform.position = from + d * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(d);
            go.AddComponent<Fader>().life = 0.05f;
        }

        public static void Burst(Vector3 pos, Color color, int count, float size, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var go = Greybox.Prim(PrimitiveType.Sphere, "Gib", Level.Root, Vector3.zero,
                    Vector3.one * size * Random.Range(0.6f, 1.2f), color, ColliderMode.None, 0.6f);
                go.transform.position = pos;
                var f = go.AddComponent<Fader>();
                f.life = Random.Range(0.4f, 0.8f);
                f.velocity = Random.onUnitSphere * speed * Random.Range(0.5f, 1f);
                f.shrink = true;
            }
        }
    }
}
