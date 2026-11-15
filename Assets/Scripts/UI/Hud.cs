using UnityEngine;

namespace Leukocyte
{
    /// <summary>Immediate-mode HUD: crosshair, hit markers, Host vitals, player HP, ammo, wave info and screens.</summary>
    public class Hud : MonoBehaviour
    {
        float hitTimer;
        bool hitWasKill;
        float hurtTimer;
        float hostFlashTimer;
        bool subscribed;

        GUIStyle big, medium, small, smallRight;

        static readonly Color HostColor = new Color(1f, 0.45f, 0.65f);
        static readonly Color HpColor = new Color(0.55f, 0.85f, 1f);

        public void ShowHitMarker(bool kill)
        {
            hitTimer = kill ? 0.2f : 0.1f;
            hitWasKill = kill;
        }

        public void HostHit() { hostFlashTimer = 0.5f; }

        void Update()
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

        void EnsureStyles()
        {
            if (big != null) return;
            big = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            medium = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperLeft };
            smallRight = new GUIStyle(small) { alignment = TextAnchor.UpperRight };
            foreach (var s in new[] { big, medium, small, smallRight }) s.normal.textColor = Color.white;
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Bar(Rect r, float fraction, Color color, string label, GUIStyle style)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(fraction), r.height - 4), color);
            GUI.Label(new Rect(r.x + 6, r.y - 1, r.width, r.height), label, style);
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (!gm || !gm.Player) return;
            EnsureStyles();

            float W = Screen.width, H = Screen.height;
            var player = gm.Player;
            var rifle = player.Rifle;
            var waves = gm.Waves;

            if (hurtTimer > 0f) Fill(new Rect(0, 0, W, H), new Color(0.8f, 0f, 0f, hurtTimer));

            // Crosshair and hit marker.
            if (gm.State == GameState.Playing && !player.Hp.IsDead)
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

            // Host vitals.
            var hostCol = hostFlashTimer > 0f && Mathf.Repeat(Time.time * 8f, 1f) > 0.5f ? new Color(0.5f, 1f, 0.3f) : HostColor;
            Bar(new Rect(W * 0.5f - 220, 16, 440, 24), gm.HostHealth / gm.hostMax, hostCol,
                $"HOST VITALS  {Mathf.CeilToInt(gm.HostHealth)}%", small);

            // Wave info.
            string waveText = waves.CurrentWave == 0 ? "Infection incoming" : $"Wave {waves.CurrentWave}/{waves.TotalWaves}";
            GUI.Label(new Rect(16, 12, 400, 24), waveText, small);
            GUI.Label(new Rect(16, 34, 400, 24), $"Germs remaining: {waves.Remaining}", small);
            GUI.Label(new Rect(16, 56, 400, 24), $"Kills: {gm.Kills}", small);

            // Player status.
            Bar(new Rect(16, H - 44, 280, 26), player.Hp.Current / player.Hp.max, HpColor,
                $"TROOPER  {Mathf.CeilToInt(player.Hp.Current)}", small);
            GUI.Label(new Rect(16, H - 70, 300, 24), $"Reinforcements: {gm.Lives}", small);

            string ammo = rifle.Reloading ? "RELOADING..." : $"{rifle.Ammo} / {rifle.magazineSize}";
            GUI.Label(new Rect(W - 316, H - 64, 300, 24), "ENZYME RIFLE", smallRight);
            GUI.Label(new Rect(W - 316, H - 42, 300, 30), ammo, new GUIStyle(smallRight) { fontSize = 24, fontStyle = FontStyle.Bold });

            // Center messages.
            if (gm.State == GameState.Playing)
            {
                if (player.Hp.IsDead)
                    GUI.Label(new Rect(0, H * 0.4f, W, 60), $"Trooper down. Respawning in {Mathf.CeilToInt(gm.RespawnTimer)}", medium);
                else if (waves.InBreak)
                {
                    string next = waves.CurrentWave == 0 ? "First wave" : $"Wave {waves.CurrentWave + 1}";
                    GUI.Label(new Rect(0, H * 0.25f, W, 60), $"{next} in {Mathf.CeilToInt(waves.BreakTimer)}", medium);
                }

                if (!player.InputEnabled)
                {
                    Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.5f));
                    GUI.Label(new Rect(0, H * 0.4f, W, 60), "Click to deploy", big);
                    GUI.Label(new Rect(0, H * 0.4f + 60, W, 120),
                        "WASD move   Shift sprint   Space jump   Mouse aim   LMB fire   R reload   Esc release mouse", medium);
                }
            }
            else
            {
                Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
                string title = gm.State == GameState.Won ? "HOST DEFENDED" : "HOST LOST";
                GUI.Label(new Rect(0, H * 0.35f, W, 60), title, big);
                GUI.Label(new Rect(0, H * 0.35f + 60, W, 40), gm.EndReason, medium);
                GUI.Label(new Rect(0, H * 0.35f + 100, W, 40), $"Germs destroyed: {gm.Kills}", medium);
                GUI.Label(new Rect(0, H * 0.35f + 150, W, 40), "Press Enter to redeploy", medium);
            }
        }
    }
}
