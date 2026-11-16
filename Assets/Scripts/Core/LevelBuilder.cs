using UnityEngine;
using UnityEngine.Rendering;

namespace Leukocyte
{
    /// <summary>Artery Defense map. Builds the Femoral Artery greybox: vessel, cover, tissue nodes, breach, player and managers.
    /// The artery runs along +Z. The player starts at the Lymph Checkpoint (low Z) and germs pour in
    /// from the Wound Breach at the far end (high Z).</summary>
    public static class LevelBuilder
    {
        static readonly Color WallColor = new Color(0.55f, 0.08f, 0.1f);
        static readonly Color RibColor = new Color(0.38f, 0.04f, 0.07f);
        static readonly Color FloorColor = new Color(0.45f, 0.06f, 0.08f);
        static readonly Color FogColor = new Color(0.22f, 0.02f, 0.04f);
        static readonly Color PlateletColor = new Color(0.95f, 0.8f, 0.55f);
        static readonly Color FibrinColor = new Color(0.85f, 0.7f, 0.6f);
        static readonly Color NodeColor = new Color(1f, 0.45f, 0.65f);
        static readonly Color CorpsBlue = new Color(0.55f, 0.8f, 1f);

        public static readonly Vector3 PlayerSpawn = new Vector3(0f, 0.05f, 6f);
        public static readonly Vector3 BreachCenter = new Vector3(0f, 4f, Level.Length - 4f);

        public static GameObject Build()
        {
            var root = new GameObject("Level_ArteryDefense");
            Level.Root = root.transform;
            Level.SetBounds(new Vector3(-Level.HalfWidth + 0.8f, 0.6f, 1.5f),
                new Vector3(Level.HalfWidth - 0.8f, Level.Height - 1f, Level.Length - 1.5f));

            Atmosphere();
            Vessel(root.transform);
            Lights(root.transform);
            Cover(root.transform);
            TissueNodes(root.transform);
            Landmarks(root.transform);
            BloodFlow(root.transform);

            var gm = root.AddComponent<GameManager>();
            // Colliders built this frame aren't moved/scaled in the physics scene until the next
            // simulation step; sync now so the floor exists before the player's first move.
            Physics.SyncTransforms();
            var player = PlayerRig(root.transform, PlayerSpawn, Quaternion.identity, FogColor);
            var waves = root.AddComponent<WaveManager>();
            var hud = root.AddComponent<Hud>();
            gm.Init(GameMode.ArteryDefense, player, hud, PlayerSpawn, waves);
            waves.Init(BreachCenter);
            return root;
        }

        static void Atmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.16f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = 0.018f;
        }

        static void Vessel(Transform root)
        {
            var t = new GameObject("Artery").transform;
            t.SetParent(root, false);
            float L = Level.Length, W = Level.HalfWidth * 2f, H = Level.Height, mid = L * 0.5f;

            Greybox.Prim(PrimitiveType.Cube, "Floor", t, new Vector3(0, -0.5f, mid), new Vector3(W, 1, L), FloorColor);
            Greybox.Prim(PrimitiveType.Cube, "Ceiling", t, new Vector3(0, H + 0.5f, mid), new Vector3(W, 1, L), WallColor);
            Greybox.Prim(PrimitiveType.Cube, "WallL", t, new Vector3(-Level.HalfWidth - 0.5f, H * 0.5f, mid), new Vector3(1, H, L), WallColor);
            Greybox.Prim(PrimitiveType.Cube, "WallR", t, new Vector3(Level.HalfWidth + 0.5f, H * 0.5f, mid), new Vector3(1, H, L), WallColor);
            Greybox.Prim(PrimitiveType.Cube, "CapNear", t, new Vector3(0, H * 0.5f, -0.5f), new Vector3(W, H, 1), WallColor);
            Greybox.Prim(PrimitiveType.Cube, "CapFar", t, new Vector3(0, H * 0.5f, L + 0.5f), new Vector3(W, H, 1), WallColor);

            // Muscle rings every 8 m give a sense of scale and motion.
            for (float z = 4f; z < L; z += 8f)
            {
                Greybox.Prim(PrimitiveType.Cube, "RibL", t, new Vector3(-Level.HalfWidth + 0.2f, H * 0.5f, z), new Vector3(0.6f, H, 0.8f), RibColor);
                Greybox.Prim(PrimitiveType.Cube, "RibR", t, new Vector3(Level.HalfWidth - 0.2f, H * 0.5f, z), new Vector3(0.6f, H, 0.8f), RibColor);
                Greybox.Prim(PrimitiveType.Cube, "RibTop", t, new Vector3(0, H - 0.2f, z), new Vector3(W, 0.6f, 0.8f), RibColor);
            }
        }

        static void Lights(Transform root)
        {
            var t = new GameObject("Lights").transform;
            t.SetParent(root, false);
            for (float z = 8f; z < Level.Length; z += 18f)
            {
                var l = new GameObject("VesselGlow").AddComponent<Light>();
                l.transform.SetParent(t, false);
                l.transform.localPosition = new Vector3(0, Level.Height - 2f, z);
                l.type = LightType.Point;
                l.range = 24f;
                l.intensity = 1.6f;
                l.color = new Color(1f, 0.55f, 0.5f);
            }

            var sun = new GameObject("Fill").AddComponent<Light>();
            sun.transform.SetParent(t, false);
            sun.transform.rotation = Quaternion.Euler(60f, -20f, 0f);
            sun.type = LightType.Directional;
            sun.intensity = 0.25f;
            sun.color = new Color(1f, 0.7f, 0.7f);
            sun.shadows = LightShadows.None;
        }

        static void Cover(Transform root)
        {
            var t = new GameObject("Cover").transform;
            t.SetParent(root, false);

            // Fibrin strands: floor-to-ceiling pillars.
            for (int i = 0; i < 12; i++)
            {
                var p = RandomCoverSpot(14f, 120f);
                Greybox.Prim(PrimitiveType.Cylinder, "FibrinStrand", t, new Vector3(p.x, Level.Height * 0.5f, p.z),
                    new Vector3(0.9f, Level.Height * 0.5f, 0.9f), FibrinColor, ColliderMode.Mesh);
            }

            // Platelet discs: low ones you can hop onto, high ones that block sightlines.
            for (int i = 0; i < 10; i++)
            {
                var p = RandomCoverSpot(12f, 125f);
                float y = i % 2 == 0 ? 0.9f : Random.Range(2.5f, 4.5f);
                var disc = Greybox.Prim(PrimitiveType.Cylinder, "PlateletDisc", t, new Vector3(p.x, y, p.z),
                    new Vector3(3.5f, 0.25f, 3.5f), PlateletColor, ColliderMode.Mesh);
                disc.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(0f, 360f), Random.Range(-6f, 6f));
            }

            // Clot chunks: waist-high cover on the floor.
            for (int i = 0; i < 10; i++)
            {
                var p = RandomCoverSpot(12f, 125f);
                var clot = Greybox.Prim(PrimitiveType.Cube, "Clot", t, new Vector3(p.x, 0.7f, p.z),
                    new Vector3(Random.Range(2f, 3.5f), 1.4f, Random.Range(0.8f, 1.4f)), new Color(0.3f, 0.03f, 0.05f));
                clot.transform.localRotation = Quaternion.Euler(0f, Random.Range(-35f, 35f), 0f);
            }
        }

        static Vector3 RandomCoverSpot(float zMin, float zMax)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var p = new Vector3(Random.Range(-6.5f, 6.5f), 0f, Random.Range(zMin, zMax));
                bool clear = true;
                foreach (var n in NodeSpots)
                    if (Vector3.Distance(new Vector3(n.x, 0, n.z), p) < 4f) clear = false;
                if (clear) return p;
            }
            return new Vector3(0f, 0f, zMax);
        }

        static readonly Vector3[] NodeSpots =
        {
            new Vector3(-6.5f, 1.8f, 18f),
            new Vector3(6.5f, 1.8f, 32f),
            new Vector3(-6.5f, 1.8f, 46f),
        };

        static TissueNode[] TissueNodes(Transform root)
        {
            var t = new GameObject("TissueNodes").transform;
            t.SetParent(root, false);
            var nodes = new TissueNode[NodeSpots.Length];
            for (int i = 0; i < NodeSpots.Length; i++)
            {
                var go = new GameObject("TissueNode_" + (char)('A' + i));
                go.transform.SetParent(t, false);
                go.transform.localPosition = NodeSpots[i];
                Greybox.Prim(PrimitiveType.Sphere, "Pod", go.transform, Vector3.zero, Vector3.one * 3f, NodeColor, ColliderMode.Keep, 0.8f);
                Greybox.Prim(PrimitiveType.Cylinder, "Stalk", go.transform, new Vector3(0, -1.2f, 0), new Vector3(1.2f, 0.6f, 1.2f),
                    new Color(0.7f, 0.25f, 0.4f), ColliderMode.None);
                var glow = new GameObject("Glow").AddComponent<Light>();
                glow.transform.SetParent(go.transform, false);
                glow.type = LightType.Point;
                glow.range = 8f;
                glow.intensity = 1.5f;
                glow.color = NodeColor;
                nodes[i] = go.AddComponent<TissueNode>();
            }
            return nodes;
        }

        static void Landmarks(Transform root)
        {
            var t = new GameObject("Landmarks").transform;
            t.SetParent(root, false);

            // Lymph Checkpoint: the player's spawn pad.
            Greybox.Prim(PrimitiveType.Cylinder, "LymphCheckpoint", t, new Vector3(PlayerSpawn.x, 0.02f, PlayerSpawn.z),
                new Vector3(3f, 0.02f, 3f), CorpsBlue, ColliderMode.None, 1.2f);

            // Wound Breach: torn vessel wall where germs pour in.
            var breach = Greybox.Prim(PrimitiveType.Cylinder, "WoundBreach", t, new Vector3(0, BreachCenter.y, Level.Length - 0.1f),
                new Vector3(9f, 0.1f, 7f), new Color(0.25f, 0.6f, 0.15f), ColliderMode.None, 1.5f);
            breach.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var l = new GameObject("BreachGlow").AddComponent<Light>();
            l.transform.SetParent(t, false);
            l.transform.localPosition = BreachCenter;
            l.type = LightType.Point;
            l.range = 20f;
            l.intensity = 2.5f;
            l.color = new Color(0.4f, 1f, 0.3f);
        }

        static void BloodFlow(Transform root)
        {
            var t = new GameObject("BloodFlow").transform;
            t.SetParent(root, false);
            for (int i = 0; i < 90; i++)
            {
                var pos = new Vector3(Random.Range(-8f, 8f), Random.Range(0.5f, 11f), Random.Range(0f, Level.Length));
                var cell = Greybox.Prim(PrimitiveType.Sphere, "RedBloodCell", t, pos,
                    new Vector3(0.7f, 0.22f, 0.7f), new Color(0.8f, 0.1f, 0.12f), ColliderMode.None);
                cell.transform.localRotation = Random.rotation;
                cell.AddComponent<BloodCellDrift>();
            }
        }

        /// <summary>Player rig shared by both modes.</summary>
        public static PlayerController PlayerRig(Transform root, Vector3 position, Quaternion facing, Color clearColor)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(position, facing);

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.4f;

            go.AddComponent<Health>().SetMax(100f);
            go.AddComponent<Combatant>().team = Team.Immune;

            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = clearColor;
            camGo.AddComponent<AudioListener>();

            var pc = go.AddComponent<PlayerController>();
            pc.Init(cam);
            return pc;
        }
    }

    /// <summary>Ambient red blood cells tumbling downstream (toward low Z).</summary>
    public class BloodCellDrift : MonoBehaviour
    {
        Vector3 spin;
        float speed;

        void Start()
        {
            spin = Random.insideUnitSphere * 60f;
            speed = Random.Range(1.5f, 4f);
        }

        void Update()
        {
            var p = transform.position;
            p.z -= speed * Time.deltaTime;
            if (p.z < 0f) p.z += Level.Length;
            transform.position = p;
            transform.Rotate(spin * Time.deltaTime);
        }
    }
}
