using UnityEngine;

namespace Leukocyte
{
    public enum ColliderMode { Keep, None, Mesh }

    /// <summary>Helpers for building greybox geometry from Unity primitives.
    /// Uses each primitive's default material so it works in Built-in, URP and HDRP.</summary>
    public static class Greybox
    {
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

            var mat = go.GetComponent<Renderer>().material;
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
