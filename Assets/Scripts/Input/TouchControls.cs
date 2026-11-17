using System.Collections.Generic;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>On-screen controls for phones and tablets.
    /// Left side: a floating move joystick (push to the edge to sprint).
    /// Right side: drag anywhere to look; FIRE (drag it to aim while shooting), JUMP, RELOAD,
    /// the class ability, GRAB, SQUAD and ZOOM buttons. A pause button sits top-left.</summary>
    [DefaultExecutionOrder(-100)]
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls I { get; private set; }

        public enum Button { Fire, Jump, Reload, Ability, Grab, Squad, Zoom, Pause }

        public float lookSensitivity = 0.11f;
        public float stickRadius = 70f;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Sprint { get; private set; }

        /// <summary>Set by the HUD each frame: which buttons exist right now.</summary>
        public bool ShowSquad { get; set; }
        public bool ShowZoom { get; set; }

        int stickFinger = -1;
        Vector2 stickOrigin, stickPos;
        readonly Dictionary<int, Vector2> lookFingers = new Dictionary<int, Vector2>();
        readonly Dictionary<int, Button> buttonFingers = new Dictionary<int, Button>();
        readonly HashSet<Button> held = new HashSet<Button>();
        readonly HashSet<Button> pressed = new HashSet<Button>();
        readonly HashSet<Button> toggled = new HashSet<Button>();
        int pressedFrame = -1;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        public bool Held(Button b) => held.Contains(b);
        public bool Pressed(Button b) => pressedFrame == Time.frameCount && pressed.Contains(b);
        public bool Toggled(Button b) => toggled.Contains(b);
        public void SetToggle(Button b, bool on) { if (on) toggled.Add(b); else toggled.Remove(b); }

        bool InMatch
        {
            get
            {
                var gm = GameManager.I;
                return gm && gm.State == GameState.Playing && gm.Player && !gm.Player.Hp.IsDead;
            }
        }

        void Update()
        {
            GameInput.Refresh();
            if (pressedFrame != Time.frameCount) pressed.Clear();
            Look = Vector2.zero;
            if (!GameInput.TouchMode) return;

            if (!InMatch || GameInput.TouchPaused)
            {
                ReleaseAll();
                return;
            }

            pressedFrame = Time.frameCount;
            Vector2 look = Vector2.zero;
            float s = UiScale.Factor;

            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                Vector2 gui = UiScale.FromScreen(t.position);

                switch (t.phase)
                {
                    case TouchPhase.Began:
                        if (TryButton(gui, out var b))
                        {
                            buttonFingers[t.fingerId] = b;
                            held.Add(b);
                            pressed.Add(b);
                            if (b == Button.Zoom) SetToggle(Button.Zoom, !Toggled(Button.Zoom));
                            if (b == Button.Pause) GameInput.TouchPaused = true;
                        }
                        else if (gui.x < UiScale.Width * 0.4f && stickFinger < 0)
                        {
                            stickFinger = t.fingerId;
                            stickOrigin = stickPos = gui;
                        }
                        else
                        {
                            lookFingers[t.fingerId] = gui;
                        }
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (t.fingerId == stickFinger) stickPos = gui;
                        else if (lookFingers.ContainsKey(t.fingerId) || (buttonFingers.TryGetValue(t.fingerId, out var hb) && hb == Button.Fire))
                        {
                            // Touch delta is in screen pixels; convert to design units so sensitivity is device-independent.
                            look += new Vector2(t.deltaPosition.x, t.deltaPosition.y) / s;
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (t.fingerId == stickFinger) stickFinger = -1;
                        lookFingers.Remove(t.fingerId);
                        if (buttonFingers.TryGetValue(t.fingerId, out var rb))
                        {
                            held.Remove(rb);
                            buttonFingers.Remove(t.fingerId);
                        }
                        break;
                }
            }

            if (stickFinger >= 0)
            {
                Vector2 d = stickPos - stickOrigin;
                d.y = -d.y; // GUI y grows downward
                Vector2 v = Vector2.ClampMagnitude(d / stickRadius, 1f);
                Move = v;
                Sprint = v.magnitude > 0.92f && v.y > 0.5f;
            }
            else
            {
                Move = Vector2.zero;
                Sprint = false;
            }

            float zoomScale = Toggled(Button.Zoom) ? 0.4f : 1f;
            Look = look * lookSensitivity * zoomScale;
        }

        void ReleaseAll()
        {
            stickFinger = -1;
            lookFingers.Clear();
            buttonFingers.Clear();
            held.Clear();
            Move = Vector2.zero;
            Sprint = false;
        }

        // ---- Layout (GUI design units, 720 tall) ------------------------

        Rect ButtonRect(Button b)
        {
            float W = UiScale.Width, H = UiScale.Height;
            switch (b)
            {
                case Button.Fire: return new Rect(W - 190, H - 250, 130, 130);
                case Button.Jump: return new Rect(W - 120, H - 110, 90, 90);
                case Button.Reload: return new Rect(W - 300, H - 150, 80, 80);
                case Button.Ability: return new Rect(W - 300, H - 250, 80, 80);
                case Button.Grab: return new Rect(W - 230, H - 105, 80, 80);
                case Button.Zoom: return new Rect(W - 100, H - 360, 80, 80);
                case Button.Squad: return new Rect(W - 400, H - 110, 80, 80);
                case Button.Pause: return new Rect(16, 90, 64, 44);
            }
            return default(Rect);
        }

        bool Visible(Button b) => b == Button.Squad ? ShowSquad : b == Button.Zoom ? ShowZoom : true;

        bool TryButton(Vector2 gui, out Button hit)
        {
            foreach (Button b in System.Enum.GetValues(typeof(Button)))
            {
                if (!Visible(b)) continue;
                var r = ButtonRect(b);
                // Round buttons: a little forgiving around the edge.
                if (b != Button.Pause)
                {
                    Vector2 c = r.center;
                    if ((gui - c).magnitude <= r.width * 0.6f) { hit = b; return true; }
                }
                else if (r.Contains(gui)) { hit = b; return true; }
            }
            hit = Button.Fire;
            return false;
        }

        GUIStyle label;

        void OnGUI()
        {
            if (!GameInput.TouchMode || !InMatch || GameInput.TouchPaused) return;
            UiScale.Apply();
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label.normal.textColor = Color.white;
            }

            var gm = GameManager.I;
            var ab = gm.Player.Abilities;
            string abilityText = ab && ab.ability == Ability.AntibodyTag ? "TAG" : "BURST";

            DrawButton(Button.Fire, "FIRE", new Color(0.35f, 0.75f, 1f));
            DrawButton(Button.Jump, "JUMP", Color.white);
            DrawButton(Button.Reload, "RELOAD", Color.white);
            DrawButton(Button.Ability, $"{abilityText}\n{(ab ? ab.BurstCharges : 0)}", new Color(1f, 0.85f, 0.35f));
            DrawButton(Button.Grab, "GRAB", new Color(0.6f, 0.9f, 0.5f));
            if (ShowSquad) DrawButton(Button.Squad, "SQUAD", new Color(0.45f, 0.75f, 1f));
            if (ShowZoom) DrawButton(Button.Zoom, Toggled(Button.Zoom) ? "UNZOOM" : "ZOOM", new Color(0.85f, 0.5f, 1f));

            var pr = ButtonRect(Button.Pause);
            HudBase.Fill(pr, new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(pr, "II", label);

            if (stickFinger >= 0)
            {
                Disc(stickOrigin, stickRadius, new Color(1f, 1f, 1f, 0.12f));
                Vector2 knob = stickOrigin + Vector2.ClampMagnitude(stickPos - stickOrigin, stickRadius);
                Disc(knob, 30f, new Color(1f, 1f, 1f, Sprint ? 0.55f : 0.35f));
            }
            else
            {
                float H = UiScale.Height;
                Disc(new Vector2(150f, H - 150f), stickRadius, new Color(1f, 1f, 1f, 0.06f));
                GUI.Label(new Rect(70f, H - 160f, 160f, 20f), "MOVE", label);
            }
        }

        void DrawButton(Button b, string text, Color tint)
        {
            var r = ButtonRect(b);
            bool down = held.Contains(b) || (b == Button.Zoom && Toggled(b));
            Disc(r.center, r.width * 0.5f, new Color(tint.r, tint.g, tint.b, down ? 0.55f : 0.25f));
            GUI.Label(r, text, label);
        }

        static Texture2D disc;

        static void Disc(Vector2 center, float radius, Color color)
        {
            if (!disc)
            {
                const int n = 64;
                disc = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                    float a = Mathf.Clamp01((1f - d) * 12f);
                    disc.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                disc.Apply();
            }
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), disc);
            GUI.color = old;
        }
    }
}
