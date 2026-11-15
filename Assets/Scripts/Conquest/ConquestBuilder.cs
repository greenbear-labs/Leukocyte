using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Leukocyte
{
    /// <summary>Tissue War map: the Alveoli Front, a wide open stretch of lung tissue.
    /// Immune Corps HQ is in the south (-Z), the infection's HQ in the north (+Z).
    /// Outposts A-C start with the Immune Corps, D-F with the infection.</summary>
    public static class ConquestBuilder
    {
        public const float HalfSize = 100f;
        const float WallHeight = 18f;

        static readonly Color GroundColor = new Color(0.78f, 0.42f, 0.47f);
        static readonly Color WallColor = new Color(0.6f, 0.25f, 0.32f);
        static readonly Color AlveolusColor = new Color(0.92f, 0.58f, 0.64f);
        static readonly Color BronchiColor = new Color(0.95f, 0.8f, 0.78f);
        static readonly Color CapillaryColor = new Color(0.65f, 0.12f, 0.18f);
        static readonly Color SkyColor = new Color(0.45f, 0.22f, 0.28f);

        struct PostDef
        {
            public string id;
            public Vector3 pos;
            public Team owner;
            public PostDef(string i, float x, float z, Team t) { id = i; pos = new Vector3(x, 0f, z); owner = t; }
        }

        static readonly PostDef[] Posts =
        {
            new PostDef("A", -60f, -38f, Team.Immune),
            new PostDef("B",   0f, -48f, Team.Immune),
            new PostDef("C",  60f, -38f, Team.Immune),
            new PostDef("D", -60f,  38f, Team.Infection),
            new PostDef("E",   0f,  48f, Team.Infection),
            new PostDef("F",  60f,  38f, Team.Infection),
        };

        static readonly Vector3 ImmuneHQPos = new Vector3(0f, 0f, -88f);
        static readonly Vector3 InfectionHQPos = new Vector3(0f, 0f, 88f);

        public static GameObject Build()
        {
            var root = new GameObject("Level_TissueWar");
            Level.Root = root.transform;
            Level.SetBounds(new Vector3(-HalfSize + 2f, 0.8f, -HalfSize + 2f), new Vector3(HalfSize - 2f, 14f, HalfSize - 2f));

            Atmosphere();
            Ground(root.transform);
            Lights(root.transform);

            var outposts = new List<Outpost>();
            foreach (var p in Posts) outposts.Add(BuildOutpost(root.transform, p.id, p.pos, p.owner, false));
            var immuneHQ = BuildOutpost(root.transform, "HQ", ImmuneHQPos, Team.Immune, true);
            var infectionHQ = BuildOutpost(root.transform, "HIVE", InfectionHQPos, Team.Infection, true);

            Cover(root.transform);

            var gm = root.AddComponent<GameManager>();
            var conquest = root.AddComponent<ConquestManager>();
            var spawn = ImmuneHQPos + new Vector3(0f, 0.1f, 4f);
            var player = LevelBuilder.PlayerRig(root.transform, spawn, Quaternion.identity, SkyColor);
            player.bloodCurrent = Vector3.zero;
            player.Cam.farClipPlane = 400f;
            var hud = root.AddComponent<ConquestHud>();
            gm.Init(GameMode.TissueWar, player, hud, spawn);
            conquest.Init(outposts, immuneHQ, infectionHQ);
            return root;
        }

        static void Atmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.35f, 0.38f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogDensity = 0.007f;
        }

        static void Ground(Transform root)
        {
            var t = new GameObject("Terrain").transform;
            t.SetParent(root, false);
            float S = HalfSize * 2f;
            Greybox.Prim(PrimitiveType.Cube, "Ground", t, new Vector3(0, -0.5f, 0), new Vector3(S, 1, S), GroundColor);
            Greybox.Prim(PrimitiveType.Cube, "WallN", t, new Vector3(0, WallHeight * 0.5f, HalfSize + 0.5f), new Vector3(S, WallHeight, 1), WallColor);
            Greybox.Prim(PrimitiveType.Cube, "WallS", t, new Vector3(0, WallHeight * 0.5f, -HalfSize - 0.5f), new Vector3(S, WallHeight, 1), WallColor);
            Greybox.Prim(PrimitiveType.Cube, "WallE", t, new Vector3(HalfSize + 0.5f, WallHeight * 0.5f, 0), new Vector3(1, WallHeight, S), WallColor);
            Greybox.Prim(PrimitiveType.Cube, "WallW", t, new Vector3(-HalfSize - 0.5f, WallHeight * 0.5f, 0), new Vector3(1, WallHeight, S), WallColor);

            // Capillary veins across the ground for texture (no collision).
            for (int i = 0; i < 40; i++)
            {
                var v = Greybox.Prim(PrimitiveType.Cube, "Capillary", t,
                    new Vector3(Random.Range(-95f, 95f), 0.02f, Random.Range(-95f, 95f)),
                    new Vector3(0.35f, 0.05f, Random.Range(8f, 25f)), CapillaryColor, ColliderMode.None);
                v.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
            }
        }

        static void Lights(Transform root)
        {
            var sun = new GameObject("Diffuse").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.transform.rotation = Quaternion.Euler(55f, 30f, 0f);
            sun.type = LightType.Directional;
            sun.intensity = 0.9f;
            sun.color = new Color(1f, 0.85f, 0.85f);
            sun.shadows = LightShadows.Soft;
        }

        static bool Clear(Vector3 p, float margin)
        {
            foreach (var d in Posts)
                if (Flat(d.pos - p) < 14f + margin) return false;
            if (Flat(ImmuneHQPos - p) < 16f + margin || Flat(InfectionHQPos - p) < 16f + margin) return false;
            return true;
        }

        static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;

        static Vector3 FreeSpot(float margin)
        {
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector3(Random.Range(-92f, 92f), 0f, Random.Range(-80f, 80f));
                if (Clear(p, margin)) return p;
            }
            return new Vector3(Random.Range(-92f, 92f), 0f, 0f);
        }

        static void Cover(Transform root)
        {
            var t = new GameObject("Cover").transform;
            t.SetParent(root, false);

            // Alveoli: big half-buried air sacs. Main cover and sightline blockers.
            for (int i = 0; i < 34; i++)
            {
                float size = Random.Range(7f, 15f);
                var p = FreeSpot(size * 0.5f);
                Greybox.Prim(PrimitiveType.Sphere, "Alveolus", t, new Vector3(p.x, -size * 0.2f, p.z),
                    new Vector3(size, size * Random.Range(0.75f, 1f), size), AlveolusColor, ColliderMode.Keep, 0.08f);
            }

            // Bronchioles: long fallen tubes, good for crouch-height cover lines.
            for (int i = 0; i < 14; i++)
            {
                var p = FreeSpot(6f);
                var tube = Greybox.Prim(PrimitiveType.Cylinder, "Bronchiole", t, new Vector3(p.x, 1.1f, p.z),
                    new Vector3(2.2f, Random.Range(6f, 12f), 2.2f), BronchiColor, ColliderMode.Mesh);
                tube.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 180f), 0f);
            }

            // A central ridge splits the field into lanes.
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                Greybox.Prim(PrimitiveType.Cube, "SeptumWall", t, new Vector3(i * 26f, 2.5f, 0f),
                    new Vector3(16f, 5f, 2f), WallColor);
            }
        }

        static Outpost BuildOutpost(Transform root, string id, Vector3 pos, Team owner, bool hq)
        {
            var go = new GameObject((hq ? "HQ_" : "Outpost_") + id);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;

            float radius = hq ? 12f : 9f;
            var ring = Greybox.Prim(PrimitiveType.Cylinder, "CaptureRing", go.transform, new Vector3(0f, 0.03f, 0f),
                new Vector3(radius * 2f, 0.02f, radius * 2f), Color.grey, ColliderMode.None, 0.4f);
            Greybox.Prim(PrimitiveType.Cylinder, "Platform", go.transform, new Vector3(0f, 0.15f, 0f),
                new Vector3(5f, 0.15f, 5f), new Color(0.85f, 0.75f, 0.8f), ColliderMode.Mesh);
            Greybox.Prim(PrimitiveType.Cylinder, "Mast", go.transform, new Vector3(0f, 4f, 0f),
                new Vector3(0.3f, 4f, 0.3f), new Color(0.9f, 0.9f, 0.95f), ColliderMode.Mesh);

            var pivot = new GameObject("FlagPivot").transform;
            pivot.SetParent(go.transform, false);
            var flag = Greybox.Prim(PrimitiveType.Cube, "Flag", pivot, new Vector3(1.1f, 0f, 0f),
                new Vector3(2f, 1.2f, 0.08f), Color.grey, ColliderMode.None, 0.8f);

            // Low barricades around the post.
            if (!hq)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * 90f + 45f;
                    var b = Greybox.Prim(PrimitiveType.Cube, "Barricade", go.transform,
                        Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.6f, radius * 0.7f), new Vector3(3.5f, 1.2f, 0.8f),
                        new Color(0.7f, 0.5f, 0.55f));
                    b.transform.localRotation = Quaternion.Euler(0f, a, 0f);
                }
            }

            var light = new GameObject("Beacon").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 6f, 0f);
            light.type = LightType.Point;
            light.range = 16f;
            light.intensity = 2f;

            var outpost = go.AddComponent<Outpost>();
            outpost.radius = radius;
            outpost.isHQ = hq;
            outpost.Init(id, owner, flag.GetComponent<Renderer>(), ring.GetComponent<Renderer>(), light, pivot);
            return outpost;
        }
    }
}
