using System.Collections;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Drop this on an empty GameObject in an empty scene and press Play.
    /// Shows the main menu, then builds the chosen mode's level from code.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap I { get; private set; }

        public int seed = 1337;

        GameObject level;
        GameObject menuCamera;
        GameMode lastMode;
        bool inMenu = true;
        GUIStyle title, subtitle, button, caption, classButton, selectedClass;

        void Awake()
        {
            I = this;
            gameObject.AddComponent<TouchControls>();
            GameInput.Refresh();
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = 60;
                QualitySettings.shadows = ShadowQuality.Disable;
            }
        }

        void Start()
        {
            // Our levels bring their own camera; hide any default scene camera.
            foreach (var cam in Camera.allCameras)
                if (cam.gameObject != gameObject) cam.gameObject.SetActive(false);
            ShowMenu();
        }

        void ShowMenu()
        {
            inMenu = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (!menuCamera)
            {
                menuCamera = new GameObject("MenuCamera");
                var cam = menuCamera.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.16f, 0.02f, 0.05f);
                menuCamera.AddComponent<AudioListener>();
            }
            menuCamera.SetActive(true);
        }

        public void Play(GameMode mode)
        {
            lastMode = mode;
            StartCoroutine(BuildRoutine(mode));
        }

        public void Restart() { StartCoroutine(BuildRoutine(lastMode)); }

        public void ReturnToMenu() { StartCoroutine(MenuRoutine()); }

        IEnumerator MenuRoutine()
        {
            if (level) Destroy(level);
            yield return null;
            ShowMenu();
        }

        IEnumerator BuildRoutine(GameMode mode)
        {
            inMenu = false;
            GameInput.TouchPaused = false;
            if (level) Destroy(level);
            yield return null;
            if (menuCamera) menuCamera.SetActive(false);
            Random.InitState(seed + (int)mode);
            KillFeed.Reset();
            level = mode == GameMode.TissueWar ? ConquestBuilder.Build() : LevelBuilder.Build();
        }

        void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(0.85f, 0.93f, 1f);
            subtitle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            subtitle.normal.textColor = new Color(1f, 0.75f, 0.8f);
            button = new GUIStyle(GUI.skin.button) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            caption = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.UpperCenter, wordWrap = true };
            caption.normal.textColor = new Color(0.9f, 0.85f, 0.88f);
            classButton = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            selectedClass = new GUIStyle(classButton);
            selectedClass.normal.textColor = new Color(0.5f, 0.85f, 1f);
            selectedClass.hover.textColor = new Color(0.5f, 0.85f, 1f);
        }

        void OnGUI()
        {
            if (!inMenu) return;
            EnsureStyles();
            UiScale.Apply();
            float W = UiScale.Width, H = UiScale.Height, cx = W * 0.5f;

            GUI.Label(new Rect(0, H * 0.06f, W, 90), "LEUKOCYTE", title);
            GUI.Label(new Rect(0, H * 0.06f + 84, W, 30), "The Immune Corps needs you. The Host is under attack.", subtitle);

            float bw = 420f, bh = 60f, y = H * 0.06f + 140f;
            if (GUI.Button(new Rect(cx - bw * 0.5f, y, bw, bh), "ARTERY DEFENSE", button)) Play(GameMode.ArteryDefense);
            GUI.Label(new Rect(cx - bw * 0.5f, y + bh + 4, bw, 40),
                "Solo. Hold the femoral artery through five germ waves, then kill the Spore Mother.", caption);

            y += bh + 60f;
            if (GUI.Button(new Rect(cx - bw * 0.5f, y, bw, bh), "TISSUE WAR", button)) Play(GameMode.TissueWar);
            GUI.Label(new Rect(cx - bw * 0.5f, y + bh + 4, bw, 60),
                "Squad battle on the Alveoli Front. Capture outposts with your AI troopers and drain the infection's reinforcements.", caption);

            y += bh + 80f;
            GUI.Label(new Rect(0, y, W, 24), "CHOOSE YOUR CELL", subtitle);
            y += 30f;
            float cw = 260f, gap = 14f, x0 = cx - (3 * cw + 2 * gap) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                var p = ClassProfile.Get((TrooperClass)i);
                bool sel = ClassProfile.Selected == p.id;
                var r = new Rect(x0 + i * (cw + gap), y, cw, 40);
                string label = (sel ? "> " : "") + p.title + "  " + p.role;
                if (GUI.Button(r, label, sel ? selectedClass : classButton)) ClassProfile.Selected = p.id;
                GUI.Label(new Rect(r.x, r.yMax + 4, cw, 60), p.blurb, caption);
            }

            y += 120f;
            if (Application.platform != RuntimePlatform.WebGLPlayer && GUI.Button(new Rect(cx - 100, y, 200, 40), "QUIT")) Application.Quit();
        }
    }
}
