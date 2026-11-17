using UnityEngine;

namespace Leukocyte
{
    /// <summary>All IMGUI layouts are authored for a 720 px tall screen and scaled to fit,
    /// so text stays readable on phones and big monitors alike.</summary>
    public static class UiScale
    {
        public const float DesignHeight = 720f;

        public static float Factor => Mathf.Max(0.5f, Screen.height / DesignHeight);
        public static float Width => Screen.width / Factor;
        public static float Height => Screen.height / Factor;

        /// <summary>Call at the top of OnGUI.</summary>
        public static void Apply()
        {
            float s = Factor;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        }

        /// <summary>Screen pixels (origin bottom-left, like Input and WorldToScreenPoint) to GUI coordinates.</summary>
        public static Vector2 FromScreen(Vector2 p) => new Vector2(p.x / Factor, (Screen.height - p.y) / Factor);
    }
}
