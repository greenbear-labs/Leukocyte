using UnityEngine;

namespace Leukocyte
{
    /// <summary>Shared IMGUI HUD pieces: crosshair, hit markers, damage vignette, HP, ammo, pause and end screens.</summary>
    public abstract class HudBase : MonoBehaviour
    {
        protected float hitTimer;
        protected bool hitWasKill;
        protected float hurtTimer;
        protected float hostFlashTimer;
        bool subscribed;

        protected GUIStyle big, medium, small, smallRight, tiny;

        protected static readonly Color ImmuneBlue = new Color(0.45f, 0.75f, 1f);
        protected static readonly Color InfectionGreen = new Color(0.55f, 0.95f, 0.25f);
        protected static readonly Color NeutralGrey = new Color(0.7f, 0.7f, 0.7f);

        public void ShowHitMarker(bool kill)
        {
            hitTimer = kill ? 0.2f : 0.1f;
            hitWasKill = kill;
        }

        public void HostHit() { hostFlashTimer = 0.5f; }

        protected virtual void Update()
        {
            var gm = GameManager.I;
            if (!subscribed && gm && gm.Player)
            {
                gm.Player.Hp.Damaged += _ => hurtTimer = 0.35f;
                subscribed = true;
            }

            float dt = Time.deltaTime;
            hitTimer -= dt;
            hurtTimer -= dt;
            hostFlashTimer -= dt;
        }

        protected void EnsureStyles()
        {
            if (big != null) return;
            big = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            medium = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperLeft };
            smallRight = new GUIStyle(small) { alignment = TextAnchor.UpperRight };
            tiny = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            foreach (var s in new[] { big, medium, small, smallRight, tiny }) s.normal.textColor = Color.white;
        }

        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        protected static void Bar(Rect r, float fraction, Color color, string label, GUIStyle style)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(fraction), r.height - 4), color);
            if (!string.IsNullOrEmpty(label)) GUI.Label(new Rect(r.x + 6, r.y - 1, r.width, r.height), label, style);
        }

        protected static Color TeamColor(Team? team) =>
            team == Team.Immune ? ImmuneBlue : team == Team.Infection ? InfectionGreen : NeutralGrey;

        void OnGUI()
        {
            var gm = GameManager.I;
            if (!gm || !gm.Player) return;
            EnsureStyles();

            float W = Screen.width, H = Screen.height;
            var player = gm.Player;

            if (hurtTimer > 0f) Fill(new Rect(0, 0, W, H), new Color(0.8f, 0f, 0f, hurtTimer));

            if (gm.State == GameState.Playing && !player.Hp.IsDead) DrawCrosshair(W, H);

            DrawModeHud(gm, W, H);
            DrawPlayerStatus(gm, W, H);

            if (gm.State == GameState.Playing)
            {
                if (player.Hp.IsDead) DrawRespawn(gm, W, H);
                if (!player.InputEnabled) DrawPause(W, H);
            }
            else
            {
                DrawEnd(gm, W, H);
            }
        }

        protected abstract void DrawModeHud(GameManager gm, float W, float H);

        protected virtual void DrawRespawn(GameManager gm, float W, float H)
        {
            GUI.Label(new Rect(0, H * 0.4f, W, 60), $"Trooper down. Respawning in {Mathf.CeilToInt(gm.RespawnTimer)}", medium);
        }

        protected virtual string LivesLine(GameManager gm) => $"Reinforcements: {gm.Lives}";

        void DrawCrosshair(float W, float H)
        {
            float cx = W * 0.5f, cy = H * 0.5f;
            var ch = new Color(1f, 1f, 1f, 0.85f);
            Fill(new Rect(cx - 12, cy - 1, 8, 2), ch);
            Fill(new Rect(cx + 4, cy - 1, 8, 2), ch);
            Fill(new Rect(cx - 1, cy - 12, 2, 8), ch);
            Fill(new Rect(cx - 1, cy + 4, 2, 8), ch);
            if (hitTimer > 0f)
            {
                var hc = hitWasKill ? new Color(1f, 0.3f, 0.3f) : Color.white;
                GUI.Label(new Rect(cx - 50, cy - 25, 100, 50), "X", new GUIStyle(medium) { normal = { textColor = hc } });
            }
        }

        void DrawPlayerStatus(GameManager gm, float W, float H)
        {
            var player = gm.Player;
            var rifle = player.Rifle;
            Bar(new Rect(16, H - 44, 280, 26), player.Hp.Current / player.Hp.max, ImmuneBlue,
                $"TROOPER  {Mathf.CeilToInt(player.Hp.Current)}", small);
            string lives = LivesLine(gm);
            if (!string.IsNullOrEmpty(lives)) GUI.Label(new Rect(16, H - 70, 300, 24), lives, small);

            string ammo = rifle.Reloading ? "RELOADING..." : $"{rifle.Ammo} / {rifle.magazineSize}";
            GUI.Label(new Rect(W - 316, H - 64, 300, 24), "ENZYME RIFLE", smallRight);
            GUI.Label(new Rect(W - 316, H - 42, 300, 30), ammo, new GUIStyle(smallRight) { fontSize = 24, fontStyle = FontStyle.Bold });
        }

        void DrawPause(float W, float H)
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.5f));
            GUI.Label(new Rect(0, H * 0.38f, W, 60), "Click to deploy", big);
            GUI.Label(new Rect(0, H * 0.38f + 60, W, 40),
                "WASD move   Shift sprint   Space jump   Mouse aim   LMB fire   R reload   Esc release mouse", medium);
            GUI.Label(new Rect(0, H * 0.38f + 100, W, 40), "M  main menu", medium);
        }

        void DrawEnd(GameManager gm, float W, float H)
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
            string title = gm.State == GameState.Won ? "VICTORY" : "DEFEAT";
            GUI.Label(new Rect(0, H * 0.35f, W, 60), title, big);
            GUI.Label(new Rect(0, H * 0.35f + 60, W, 40), gm.EndReason, medium);
            GUI.Label(new Rect(0, H * 0.35f + 100, W, 40), $"Enemies destroyed: {gm.Kills}", medium);
            GUI.Label(new Rect(0, H * 0.35f + 150, W, 40), "Enter  play again      M  main menu", medium);
        }
    }
}
