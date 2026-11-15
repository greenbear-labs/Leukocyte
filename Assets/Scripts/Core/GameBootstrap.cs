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
        GUIStyle title, subtitle, button, caption;

        void Awake() { I = this; }

        void Start()
        {
            // Our levels bring their own camera; hide any default scene camera.
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
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
            if (level) Destroy(level);
            yield return null;
            if (menuCamera) menuCamera.SetActive(false);
            Random.InitState(seed + (int)mode);
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
        }

        void OnGUI()
        {
            if (!inMenu) return;
            EnsureStyles();
            float W = Screen.width, H = Screen.height, cx = W * 0.5f;

            GUI.Label(new Rect(0, H * 0.12f, W, 90), "LEUKOCYTE", title);
            GUI.Label(new Rect(0, H * 0.12f + 84, W, 30), "The Immune Corps needs you. The Host is under attack.", subtitle);

            float bw = 420f, bh = 64f, y = H * 0.4f;
            if (GUI.Button(new Rect(cx - bw * 0.5f, y, bw, bh), "ARTERY DEFENSE", button)) Play(GameMode.ArteryDefense);
            GUI.Label(new Rect(cx - bw * 0.5f, y + bh + 4, bw, 40),
                "Solo. Hold the femoral artery against five escalating germ waves.", caption);

            y += bh + 60f;
            if (GUI.Button(new Rect(cx - bw * 0.5f, y, bw, bh), "TISSUE WAR", button)) Play(GameMode.TissueWar);
            GUI.Label(new Rect(cx - bw * 0.5f, y + bh + 4, bw, 60),
                "Squad battle on the Alveoli Front. Capture outposts with your AI troopers and drain the infection's reinforcements.", caption);

            y += bh + 80f;
            if (GUI.Button(new Rect(cx - 100, y, 200, 40), "QUIT")) Application.Quit();
        }
    }
}
