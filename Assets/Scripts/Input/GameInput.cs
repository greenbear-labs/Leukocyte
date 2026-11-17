using UnityEngine;

namespace Leukocyte
{
    /// <summary>One place the game reads input from: keyboard and mouse on computers,
    /// the on-screen TouchControls on phones and tablets.</summary>
    public static class GameInput
    {
        /// <summary>True on phones/tablets, or as soon as a touch is seen.</summary>
        public static bool TouchMode { get; private set; }

        /// <summary>Touch play has no cursor lock; this stands in for "paused".</summary>
        public static bool TouchPaused { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            TouchMode = false;
            TouchPaused = false;
        }

        public static void Refresh()
        {
            if (!TouchMode && (Application.isMobilePlatform || Input.touchCount > 0)) TouchMode = true;
        }

        static TouchControls T => TouchControls.I;

        /// <summary>Whether gameplay input is live (cursor captured, or touch play not paused).</summary>
        public static bool Active => TouchMode ? !TouchPaused : Cursor.lockState == CursorLockMode.Locked;

        public static Vector2 Move
        {
            get
            {
                if (TouchMode) return T ? T.Move : Vector2.zero;
                return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            }
        }

        /// <summary>Look delta in "mouse axis" units (same scale as Input.GetAxis("Mouse X")).</summary>
        public static Vector2 Look
        {
            get
            {
                if (TouchMode) return T ? T.Look : Vector2.zero;
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
        }

        public static bool Sprint => TouchMode ? T && T.Sprint : Input.GetKey(KeyCode.LeftShift);
        public static bool Jump => TouchMode ? T && T.Pressed(TouchControls.Button.Jump) : Input.GetButtonDown("Jump");
        public static bool FireHeld => TouchMode ? T && T.Held(TouchControls.Button.Fire) : Input.GetMouseButton(0);
        public static bool FirePressed => TouchMode ? T && T.Pressed(TouchControls.Button.Fire) : Input.GetMouseButtonDown(0);
        public static bool Zoom => TouchMode ? T && T.Toggled(TouchControls.Button.Zoom) : Input.GetMouseButton(1);
        public static bool Reload => TouchMode ? T && T.Pressed(TouchControls.Button.Reload) : Input.GetKeyDown(KeyCode.R);
        public static bool Ability => TouchMode ? T && T.Pressed(TouchControls.Button.Ability) : Input.GetKeyDown(KeyCode.G);
        public static bool Grab => TouchMode ? T && T.Pressed(TouchControls.Button.Grab) : Input.GetKeyDown(KeyCode.F);
        public static bool Squad => TouchMode ? T && T.Pressed(TouchControls.Button.Squad) : Input.GetKeyDown(KeyCode.Q);

        public static void ClearZoom()
        {
            if (T) T.SetToggle(TouchControls.Button.Zoom, false);
        }
    }
}
