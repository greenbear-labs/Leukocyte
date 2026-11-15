using UnityEngine;

namespace Leukocyte
{
    /// <summary>Shared level state: the root object for the current match and the volume floating units stay inside.</summary>
    public static class Level
    {
        // Femoral Artery dimensions (Artery Defense mode).
        public const float Length = 140f;
        public const float HalfWidth = 9f;
        public const float Height = 12f;

        public static Transform Root;
        public static Vector3 Min = new Vector3(-HalfWidth + 0.8f, 0.6f, 1.5f);
        public static Vector3 Max = new Vector3(HalfWidth - 0.8f, Height - 1f, Length - 1.5f);

        public static void SetBounds(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public static Vector3 Clamp(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, Min.x, Max.x);
            p.y = Mathf.Clamp(p.y, Min.y, Max.y);
            p.z = Mathf.Clamp(p.z, Min.z, Max.z);
            return p;
        }
    }
}
