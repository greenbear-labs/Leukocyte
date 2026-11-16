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

            UiScale.Apply();
            float W = UiScale.Width, H = UiScale.Height;
            var player = gm.Player;
            if (TouchControls.I)
            {
                TouchControls.I.ShowSquad = ConquestManager.I != null;
                TouchControls.I.ShowZoom = player.Rifle.Profile.zoomFov > 0f;
            }

            if (hurtTimer > 0f) Fill(new Rect(0, 0, W, H), new Color(0.8f, 0f, 0f, hurtTimer));

            if (gm.State == GameState.Playing && !player.Hp.IsDead)
            {
                DrawMarks(player, W, H);
                if (player.Rifle.Zoomed) DrawScope(W, H);
                DrawCrosshair(W, H);
                DrawDamageIndicator(player, W, H);
                DrawAbilities(player, W, H);
            }
            DrawKillFeed(W, H);
            DrawBossBar(W);

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
            GUI.Label(new Rect(0, H * 0.3f, W, 60), $"Trooper down. Respawning in {Mathf.CeilToInt(gm.RespawnTimer)}", medium);
            DrawClassPicker(gm, W, H * 0.3f + 60);
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
            string burst = $"[G] {ab.AbilityName}  x{ab.BurstCharges}";
            GUI.Label(new Rect(x, y, 300, 22), burst, smallRight);
            if (ab.BurstCharges < ab.burstCharges)
                Fill(new Rect(x + 150, y + 20, 150 * Mathf.Clamp01(ab.BurstRechargeProgress), 3), new Color(1f, 0.85f, 0.35f));
            string grab = ab.GrabReady >= 1f ? "[F] PHAGOCYTE GRAB  READY" : "[F] PHAGOCYTE GRAB";
            GUI.Label(new Rect(x, y + 24, 300, 22), grab, smallRight);
            if (ab.GrabReady < 1f)
                Fill(new Rect(x + 150, y + 44, 150 * ab.GrabReady, 3), ImmuneBlue);
            if (ConquestManager.I)
            {
                int squad = ab.SquadCount;
                GUI.Label(new Rect(x, y - 24, 300, 22), squad > 0 ? $"[Q] SQUAD FOLLOWING ({squad})" : "[Q] CALL SQUAD", smallRight);
            }
        }

        /// <summary>Red diamonds over enemies marked by Antibody Tag, visible through cover.</summary>
        void DrawMarks(PlayerController player, float W, float H)
        {
            var cam = player.Cam;
            foreach (var c in Combatant.All)
            {
                if (!c.Alive || !c.Marked) continue;
                Vector3 sp = cam.WorldToScreenPoint(c.AimPoint + Vector3.up * 0.5f);
                if (sp.z <= 0f) continue;
                Vector2 g = UiScale.FromScreen(sp);
                float x = g.x, y = g.y;
                var old = GUI.matrix;
                GUIUtility.RotateAroundPivot(45f, new Vector2(x, y));
                Fill(new Rect(x - 6, y - 6, 12, 12), new Color(1f, 0.25f, 0.3f, 0.9f));
                Fill(new Rect(x - 3, y - 3, 6, 6), new Color(0.2f, 0f, 0f, 0.9f));
                GUI.matrix = old;
            }
        }

        void DrawBossBar(float W)
        {
            var boss = SporeMother.Current;
            if (!boss || boss.Hp.IsDead) return;
            var r = new Rect(W * 0.5f - 230, 140, 460, 20);
            Bar(r, boss.Hp.Current / boss.Hp.max, new Color(0.85f, 0.4f, 0.95f), "", small);
            GUI.Label(new Rect(r.x, r.yMax, r.width, 22), "SPORE MOTHER", tiny);
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

        void DrawScope(float W, float H)
        {
            float r = Mathf.Min(W, H) * 0.45f;
            var dark = new Color(0f, 0f, 0f, 0.85f);
            Fill(new Rect(0, 0, W * 0.5f - r, H), dark);
            Fill(new Rect(W * 0.5f + r, 0, W * 0.5f - r, H), dark);
            Fill(new Rect(W * 0.5f - r, 0, 2 * r, H * 0.5f - r), dark);
            Fill(new Rect(W * 0.5f - r, H * 0.5f + r, 2 * r, H * 0.5f - r), dark);
            Fill(new Rect(W * 0.5f - r, H * 0.5f - 0.5f, 2 * r, 1), new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(W * 0.5f - 0.5f, H * 0.5f - r, 1, 2 * r), new Color(0f, 0f, 0f, 0.6f));
        }

        /// <summary>Class picker shown on the pause and respawn screens. Tab cycles; applies at next deploy.</summary>
        protected void DrawClassPicker(GameManager gm, float W, float y)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
            {
                ClassProfile.Selected = (TrooperClass)(((int)ClassProfile.Selected + 1) % 3);
                Event.current.Use();
            }
            var current = gm.Player.Class;
            float bw = 220f, gap = 12f, x0 = W * 0.5f - (3 * bw + 2 * gap) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                var p = ClassProfile.Get((TrooperClass)i);
                bool sel = ClassProfile.Selected == p.id;
                var r = new Rect(x0 + i * (bw + gap), y, bw, 64);
                Fill(r, sel ? new Color(0.3f, 0.55f, 0.9f, 0.85f) : new Color(0f, 0f, 0f, 0.55f));
                GUI.Label(new Rect(r.x, r.y + 4, r.width, 24), $"{p.title}  ({p.role})", tiny);
                GUI.Label(new Rect(r.x, r.y + 26, r.width, 34), p.weapon.name, tiny);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) ClassProfile.Selected = p.id;
            }
            string note = ClassProfile.Selected != current.id ? "New class applies when you next deploy"
                : GameInput.TouchMode ? "Tap a class to change" : "Tab or click to change class";
            GUI.Label(new Rect(0, y + 70, W, 22), note, small.Centered());
        }

        void DrawPlayerStatus(GameManager gm, float W, float H)
        {
            var player = gm.Player;
            var rifle = player.Rifle;
            Bar(new Rect(16, H - 44, 280, 26), player.Hp.Current / player.Hp.max, ImmuneBlue,
                $"{player.Class.title}  {Mathf.CeilToInt(player.Hp.Current)}", small);
            string lives = LivesLine(gm);
            if (!string.IsNullOrEmpty(lives)) GUI.Label(new Rect(16, H - 70, 300, 24), lives, small);

            string ammo = rifle.Reloading ? "RELOADING..." : $"{rifle.Ammo} / {rifle.magazineSize}";
            GUI.Label(new Rect(W - 316, H - 64, 300, 24), rifle.WeaponName, smallRight);
            GUI.Label(new Rect(W - 316, H - 42, 300, 30), ammo, new GUIStyle(smallRight) { fontSize = 24, fontStyle = FontStyle.Bold });
        }

        protected GUIStyle bigButton;

        void EnsureButtonStyle()
        {
            if (bigButton != null) return;
            bigButton = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
        }

        void DrawPause(float W, float H)
        {
            EnsureButtonStyle();
            bool touch = GameInput.TouchMode;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(new Rect(0, H * 0.12f, W, 60), touch ? "PAUSED" : "Click to deploy", big);
            DrawClassPicker(GameManager.I, W, H * 0.12f + 70);

            float y = H * 0.12f + 180;
            if (touch)
            {
                GUI.Label(new Rect(0, y, W, 60),
                    "Left thumb: move (push to the edge to sprint).  Right thumb: drag to look.  Hold FIRE and drag to aim while shooting.\nBURST/TAG is your class ability, GRAB engulfs a weakened germ to heal, SQUAD calls nearby troopers.", small.Centered());
                if (GUI.Button(new Rect(W * 0.5f - 160, y + 80, 320, 56), "RESUME", bigButton)) PlayerController.LockCursor();
                if (GUI.Button(new Rect(W * 0.5f - 160, y + 150, 320, 48), "MAIN MENU", bigButton) && GameBootstrap.I) GameBootstrap.I.ReturnToMenu();
            }
            else
            {
                GUI.Label(new Rect(0, y, W, 30), "WASD move   Shift sprint   Space jump   Mouse aim   LMB fire   R reload", medium);
                GUI.Label(new Rect(0, y + 34, W, 30), "G class ability   F Phagocyte Grab (engulf a weakened germ to heal)", medium);
                GUI.Label(new Rect(0, y + 68, W, 30), "Q squad (Tissue War)   RMB zoom (T-Cell)   Esc release mouse   M main menu", medium);
                if (GUI.Button(new Rect(W * 0.5f - 120, y + 120, 240, 44), "MAIN MENU", bigButton) && GameBootstrap.I) GameBootstrap.I.ReturnToMenu();
            }
        }

        void DrawEnd(GameManager gm, float W, float H)
        {
            EnsureButtonStyle();
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
            string title = gm.State == GameState.Won ? "VICTORY" : "DEFEAT";
            GUI.Label(new Rect(0, H * 0.25f, W, 60), title, big);
            GUI.Label(new Rect(0, H * 0.25f + 60, W, 40), gm.EndReason, medium);
            GUI.Label(new Rect(0, H * 0.25f + 100, W, 40), $"Enemies destroyed: {gm.Kills}     Score: {KillFeed.Score}", medium);
            float y = H * 0.25f + 160;
            if (GUI.Button(new Rect(W * 0.5f - 250, y, 240, 56), "PLAY AGAIN", bigButton) && GameBootstrap.I) GameBootstrap.I.Restart();
            if (GUI.Button(new Rect(W * 0.5f + 10, y, 240, 56), "MAIN MENU", bigButton) && GameBootstrap.I) GameBootstrap.I.ReturnToMenu();
            if (!GameInput.TouchMode) GUI.Label(new Rect(0, y + 64, W, 30), "Enter  play again      M  main menu", small.Centered());
        }
    }

    static class GuiStyleCenterExtensions
    {
        public static GUIStyle Centered(this GUIStyle s) => new GUIStyle(s) { alignment = TextAnchor.UpperCenter };
    }
}
