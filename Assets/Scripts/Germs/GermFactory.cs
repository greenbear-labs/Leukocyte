using UnityEngine;

namespace Leukocyte
{
    /// <summary>Builds greybox germs out of primitives. Swap these for real prefabs during the art pass.</summary>
    public static class GermFactory
    {
        public static Swarmer Swarmer(Vector3 position)
        {
            var root = NewRoot("Swarmer", position, 30f);
            var green = new Color(0.35f, 0.85f, 0.2f);
            var dark = new Color(0.15f, 0.4f, 0.1f);

            Greybox.Prim(PrimitiveType.Sphere, "Body", root, Vector3.zero, new Vector3(0.9f, 0.8f, 1.1f), green, ColliderMode.Keep, 0.4f);
            Greybox.Prim(PrimitiveType.Sphere, "Maw", root, new Vector3(0f, -0.05f, 0.5f), new Vector3(0.45f, 0.3f, 0.15f), new Color(0.5f, 0f, 0.05f), ColliderMode.None);
            for (int i = 0; i < 10; i++)
                Spike(root, Random.onUnitSphere, 0.45f, new Vector3(0.07f, 0.18f, 0.07f), dark);
            for (int i = 0; i < 3; i++)
            {
                var f = Greybox.Prim(PrimitiveType.Cylinder, "Flagellum", root, new Vector3((i - 1) * 0.2f, 0f, -0.9f),
                    new Vector3(0.05f, 0.45f, 0.05f), dark, ColliderMode.None);
                f.transform.localRotation = Quaternion.Euler(90f + (i - 1) * 15f, 0f, 0f);
            }
            return root.gameObject.AddComponent<Swarmer>();
        }

        public static Spitter Spitter(Vector3 position)
        {
            var root = NewRoot("Spitter", position, 60f);
            var violet = new Color(0.55f, 0.2f, 0.85f);
            var pale = new Color(0.8f, 0.6f, 1f);

            Greybox.Prim(PrimitiveType.Sphere, "Capsid", root, Vector3.zero, Vector3.one * 1.4f, violet, ColliderMode.Keep, 0.3f);
            Greybox.Prim(PrimitiveType.Sphere, "AcidSac", root, new Vector3(0f, 0f, 0.62f), Vector3.one * 0.45f, new Color(0.75f, 1f, 0.2f), ColliderMode.None, 1.5f);
            for (int i = 0; i < 12; i++)
            {
                var dir = Random.onUnitSphere;
                Spike(root, dir, 0.7f, new Vector3(0.1f, 0.22f, 0.1f), pale);
                Greybox.Prim(PrimitiveType.Sphere, "Knob", root, dir * 1.15f, Vector3.one * 0.18f, pale, ColliderMode.None, 1f);
            }
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                var leg = Greybox.Prim(PrimitiveType.Cylinder, "Leg", root,
                    Quaternion.Euler(0f, a, 0f) * new Vector3(0f, -0.9f, 0.35f), new Vector3(0.08f, 0.45f, 0.08f), pale, ColliderMode.None);
                leg.transform.localRotation = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(25f, 0f, 0f);
            }
            return root.gameObject.AddComponent<Spitter>();
        }

        public static Brute Brute(Vector3 position)
        {
            var root = NewRoot("Brute", position, 400f);
            var amber = new Color(0.85f, 0.55f, 0.2f);
            var rot = new Color(0.45f, 0.25f, 0.1f);
            var glow = new Color(1f, 0.85f, 0.3f);

            Greybox.Prim(PrimitiveType.Sphere, "Body", root, Vector3.zero, new Vector3(2.6f, 2.2f, 2.6f), amber, ColliderMode.Keep, 0.2f);
            for (int i = 0; i < 8; i++)
            {
                var dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y);
                Greybox.Prim(PrimitiveType.Sphere, "SporeCap", root, dir * 1.15f, Vector3.one * Random.Range(0.5f, 0.9f), rot, ColliderMode.None);
            }
            Greybox.Prim(PrimitiveType.Sphere, "Eye", root, new Vector3(-0.45f, 0.35f, 1.15f), Vector3.one * 0.35f, glow, ColliderMode.None, 2f);
            Greybox.Prim(PrimitiveType.Sphere, "Eye", root, new Vector3(0.45f, 0.35f, 1.15f), Vector3.one * 0.35f, glow, ColliderMode.None, 2f);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var arm = Greybox.Prim(PrimitiveType.Capsule, "Arm", root, new Vector3(1.5f * side, -0.4f, 0.4f),
                    new Vector3(0.7f, 1f, 0.7f), rot, ColliderMode.None);
                arm.transform.localRotation = Quaternion.Euler(20f, 0f, 25f * side);
            }
            var brute = root.gameObject.AddComponent<Brute>();
            return brute;
        }

        static Transform NewRoot(string name, Vector3 position, float hp)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Level.Root, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(Vector3.back);
            go.AddComponent<Health>().SetMax(hp);
            var c = go.AddComponent<Combatant>();
            c.team = Team.Infection;
            c.aimHeight = 0f;
            return go.transform;
        }

        static void Spike(Transform root, Vector3 dir, float radius, Vector3 scale, Color color)
        {
            var s = Greybox.Prim(PrimitiveType.Cylinder, "Spike", root, dir * (radius + scale.y), scale, color, ColliderMode.None);
            s.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
    }
}
