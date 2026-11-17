using UnityEngine;

namespace Leukocyte
{
    /// <summary>Artery Defense HUD: Host vitals and wave info.</summary>
    public class Hud : HudBase
    {
        static readonly Color HostColor = new Color(1f, 0.45f, 0.65f);

        protected override void DrawModeHud(GameManager gm, float W, float H)
        {
            var waves = gm.Waves;

            var hostCol = hostFlashTimer > 0f && Mathf.Repeat(Time.time * 8f, 1f) > 0.5f ? new Color(0.5f, 1f, 0.3f) : HostColor;
            Bar(new Rect(W * 0.5f - 220, 16, 440, 24), gm.HostHealth / gm.hostMax, hostCol,
                $"HOST VITALS  {Mathf.CeilToInt(gm.HostHealth)}%", small);

            string waveText = waves.CurrentWave == 0 ? "Infection incoming" : $"Wave {waves.CurrentWave}/{waves.TotalWaves}";
            GUI.Label(new Rect(16, 12, 400, 24), waveText, small);
            GUI.Label(new Rect(16, 34, 400, 24), $"Germs remaining: {waves.Remaining}", small);
            GUI.Label(new Rect(16, 56, 400, 24), $"Kills: {gm.Kills}   Score: {KillFeed.Score}", small);

            if (gm.State == GameState.Playing && !gm.Player.Hp.IsDead && waves.InBreak)
            {
                string next = waves.CurrentWave == 0 ? "First wave" : $"Wave {waves.CurrentWave + 1}";
                GUI.Label(new Rect(0, H * 0.25f, W, 60), $"{next} in {Mathf.CeilToInt(waves.BreakTimer)}", medium);
            }
        }
    }
}
