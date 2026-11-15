using UnityEngine;

namespace Leukocyte
{
    public enum ColliderMode { Keep, None, Mesh }

    /// <summary>Helpers for building greybox geometry from Unity primitives.
    /// Materials come from Assets/Resources/Leukocyte (created by the editor setup) so their
    /// shaders are included in player builds. Falls back to the primitive's default material.</summary>
    public static class Greybox
    {
        static Material litBase;
        static Material emissiveBase;
        static bool loaded;

        static void LoadMaterials()
        {
            if (loaded) return;
            loaded = true;
            litBase = Resources.Load<Material>("Leukocyte/GreyboxLit");
            emissiveBase = Resources.Load<Material>("Leukocyte/GreyboxEmissive");
            if (!litBase) Debug.LogWarning("[Leukocyte] Missing Resources/Leukocyte/GreyboxLit material. Run Leukocyte > Create Femoral Artery Scene in the editor.");
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent,
            Vector3 localPos, Vector3 localScale, Color color,
            ColliderMode colliderMode = ColliderMode.Keep, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            if (colliderMode != ColliderMode.Keep)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
                if (colliderMode == ColliderMode.Mesh)
                    go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }

            LoadMaterials();
            var renderer = go.GetComponent<Renderer>();
            var template = emission > 0f && emissiveBase ? emissiveBase : litBase;
            Material mat;
            if (template)
            {
                mat = new Material(template);
                renderer.sharedMaterial = mat;
            }
            else
            {
                mat = renderer.material;
            }
            mat.color = color;
            if (emission > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
            }
            return go;
        }

        public static void SetColor(Renderer r, Color c)
        {
            r.material.color = c;
        }
    }
}
