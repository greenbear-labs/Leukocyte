using UnityEngine;

namespace Leukocyte
{
    /// <summary>Shared level dimensions for the Femoral Artery greybox.</summary>
    public static class Level
    {
        public const float Length = 140f;
        public const float HalfWidth = 9f;
        public const float Height = 12f;

        public static Transform Root;

        /// <summary>Keeps floating germs inside the artery.</summary>
        public static Vector3 Clamp(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, -HalfWidth + 0.8f, HalfWidth - 0.8f);
            p.y = Mathf.Clamp(p.y, 0.6f, Height - 1f);
            p.z = Mathf.Clamp(p.z, 1.5f, Length - 1.5f);
            return p;
        }
    }
}
