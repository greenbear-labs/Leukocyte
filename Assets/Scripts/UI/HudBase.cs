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
        float damageIndicatorTimer;
        Vector3 damageFrom;
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
                gm.Player.Hp.Damaged += _ =>
                {
                    hurtTimer = 0.35f;
                    if (gm.Player.Hp.LastHitFrom.HasValue)
                    {
                        damageFrom = gm.Player.Hp.LastHitFrom.Value;
                        damageIndicatorTimer = 1.2f;
                    }
                };
                subscribed = true;
            }

            float dt = Time.deltaTime;
            hitTimer -= dt;
            hurtTimer -= dt;
            hostFlashTimer -= dt;
            damageIndicatorTimer -= dt;
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

            if (gm.State == GameState.Playing && !player.Hp.IsDead)
            {
                DrawCrosshair(W, H);
                DrawDamageIndicator(player, W, H);
                DrawAbilities(player, W, H);
            }
            DrawKillFeed(W, H);

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

        /// <summary>Red marker around the crosshair pointing toward the last damage source.</summary>
        void DrawDamageIndicator(PlayerController player, float W, float H)
        {
            if (damageIndicatorTimer <= 0f) return;
            Vector3 to = damageFrom - player.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            float angle = Vector3.SignedAngle(player.transform.forward, to, Vector3.up);
            var pivot = new Vector2(W * 0.5f, H * 0.5f);
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pivot);
            float a = Mathf.Clamp01(damageIndicatorTimer);
            Fill(new Rect(pivot.x - 40, pivot.y - 150, 80, 10), new Color(1f, 0.15f, 0.1f, 0.85f * a));
            Fill(new Rect(pivot.x - 20, pivot.y - 162, 40, 8), new Color(1f, 0.15f, 0.1f, 0.6f * a));
            GUI.matrix = old;
        }

        void DrawAbilities(PlayerController player, float W, float H)
        {
            var ab = player.Abilities;
            if (!ab) return;
            float x = W - 316, y = H - 112;
            string burst = $"[G] CYTOKINE BURST  x{ab.BurstCharges}";
            GUI.Label(new Rect(x, y, 300, 22), burst, smallRight);
            if (ab.BurstCharges < ab.burstCharges)
                Fill(new Rect(x + 150, y + 20, 150 * Mathf.Clamp01(ab.BurstRechargeProgress), 3), new Color(1f, 0.85f, 0.35f));
            string grab = ab.GrabReady >= 1f ? "[F] PHAGOCYTE GRAB  READY" : "[F] PHAGOCYTE GRAB";
            GUI.Label(new Rect(x, y + 24, 300, 22), grab, smallRight);
            if (ab.GrabReady < 1f)
                Fill(new Rect(x + 150, y + 44, 150 * ab.GrabReady, 3), ImmuneBlue);
        }

        protected virtual float KillFeedTop => 16f;

        void DrawKillFeed(float W, float H)
        {
            float y = KillFeedTop;
            for (int i = KillFeed.Entries.Count - 1; i >= 0; i--)
            {
                var e = KillFeed.Entries[i];
                float age = Time.time - e.time;
                if (age > 5f) continue;
                var c = e.color;
                c.a = Mathf.Clamp01(5f - age);
                var style = new GUIStyle(smallRight) { normal = { textColor = c } };
                GUI.Label(new Rect(W - 416, y, 400, 22), e.text, style);
                y += 22f;
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
                "WASD move   Shift sprint   Space jump   Mouse aim   LMB fire   R reload", medium);
            GUI.Label(new Rect(0, H * 0.38f + 130, W, 40),
                "G Cytokine Burst (grenade)   F Phagocyte Grab (engulf a weakened germ to heal)   Esc release mouse", medium);
            GUI.Label(new Rect(0, H * 0.38f + 170, W, 40), "M  main menu", medium);
        }

        void DrawEnd(GameManager gm, float W, float H)
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
            string title = gm.State == GameState.Won ? "VICTORY" : "DEFEAT";
            GUI.Label(new Rect(0, H * 0.35f, W, 60), title, big);
            GUI.Label(new Rect(0, H * 0.35f + 60, W, 40), gm.EndReason, medium);
            GUI.Label(new Rect(0, H * 0.35f + 100, W, 40), $"Enemies destroyed: {gm.Kills}     Score: {KillFeed.Score}", medium);
            GUI.Label(new Rect(0, H * 0.35f + 150, W, 40), "Enter  play again      M  main menu", medium);
        }
    }
}
