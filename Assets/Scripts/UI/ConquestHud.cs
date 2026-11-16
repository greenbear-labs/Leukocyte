using UnityEngine;

namespace Leukocyte
{
    /// <summary>Tissue War HUD: tickets, outpost strip, capture status, minimap and spawn picker.</summary>
    public class ConquestHud : HudBase
    {
        protected override string LivesLine(GameManager gm) => $"Score: {KillFeed.Score}";

        protected override float KillFeedTop => 220f;

        protected override void Update()
        {
            base.Update();
            var cm = ConquestManager.I;
            var gm = GameManager.I;
            if (!cm || !gm || !gm.Player.Hp.IsDead) return;
            var options = cm.SpawnOptions(Team.Immune);
            for (int i = 0; i < options.Count && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) cm.PlayerSpawnChoice = i;
        }

        protected override void DrawModeHud(GameManager gm, float W, float H)
        {
            var cm = ConquestManager.I;
            if (!cm) return;

            // Tickets.
            float cx = W * 0.5f;
            int immune = cm.Tickets[(int)Team.Immune], infection = cm.Tickets[(int)Team.Infection];
            Bar(new Rect(cx - 250, 14, 200, 22), immune / (float)cm.startTickets, ImmuneBlue, "", small);
            Bar(new Rect(cx + 50, 14, 200, 22), infection / (float)cm.startTickets, InfectionGreen, "", small);
            GUI.Label(new Rect(cx - 250, 36, 200, 22), $"IMMUNE CORPS  {immune}", tiny);
            GUI.Label(new Rect(cx + 50, 36, 200, 22), $"INFECTION  {infection}", tiny);

            // Outpost strip.
            int n = cm.Outposts.Count;
            float size = 34f, gap = 8f;
            float x0 = cx - (n * size + (n - 1) * gap) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var o = cm.Outposts[i];
                var r = new Rect(x0 + i * (size + gap), 62, size, size);
                Fill(r, new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), TeamColor(o.Owner) * new Color(1, 1, 1, 0.85f));
                // Control meter under each icon.
                float c = o.Control;
                Fill(new Rect(r.x, r.yMax + 2, r.width, 4), new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(r.x + r.width * 0.5f, r.yMax + 2, r.width * 0.5f * c, 4), c >= 0 ? ImmuneBlue : InfectionGreen);
                if (c < 0) Fill(new Rect(r.x + r.width * 0.5f + r.width * 0.5f * c, r.yMax + 2, -r.width * 0.5f * c, 4), InfectionGreen);
                if (o.Contested && Mathf.Repeat(Time.time * 3f, 1f) > 0.5f) Fill(new Rect(r.x, r.y, r.width, 3), Color.red);
                GUI.Label(r, o.letter, medium);
            }

            // Capture status for the post the player stands in.
            var player = gm.Player;
            if (!player.Hp.IsDead)
            {
                foreach (var o in cm.Outposts)
                {
                    if (!o.Contains(player.transform.position)) continue;
                    string status;
                    if (o.Contested) status = $"OUTPOST {o.letter}: CONTESTED";
                    else if (o.Owner == Team.Immune && o.Control >= 0.999f) status = $"OUTPOST {o.letter}: SECURE";
                    else if (o.Control >= 0f && o.Owner != Team.Immune) status = $"CAPTURING {o.letter}";
                    else status = $"NEUTRALIZING {o.letter}";
                    GUI.Label(new Rect(0, H * 0.62f, W, 30), status, medium);
                    Bar(new Rect(cx - 150, H * 0.62f + 32, 300, 12), (o.Control + 1f) * 0.5f, ImmuneBlue, "", small);
                    break;
                }
            }

            if (Time.time - cm.LastEventTime < 3f)
                GUI.Label(new Rect(0, 110, W, 30), cm.LastEvent, medium);

            GUI.Label(new Rect(16, 12, 300, 24), "TISSUE WAR: Alveoli Front", small);
            GUI.Label(new Rect(16, 34, 300, 24), $"Held: {cm.Held(Team.Immune)}  vs  {cm.Held(Team.Infection)}", small);
            GUI.Label(new Rect(16, 56, 300, 24), $"Kills: {gm.Kills}", small);

            DrawMinimap(cm, gm, W);
        }

        void DrawMinimap(ConquestManager cm, GameManager gm, float W)
        {
            const float size = 190f;
            var r = new Rect(W - size - 16, 16, size, size);
            Fill(r, new Color(0f, 0f, 0f, 0.5f));
            float half = ConquestBuilder.HalfSize;

            System.Func<Vector3, Vector2> ToMap = p => new Vector2(
                r.x + (p.x + half) / (2f * half) * r.width,
                r.y + (1f - (p.z + half) / (2f * half)) * r.height);

            foreach (var o in cm.Outposts)
            {
                var m = ToMap(o.transform.position);
                Fill(new Rect(m.x - 7, m.y - 7, 14, 14), TeamColor(o.Owner));
                GUI.Label(new Rect(m.x - 10, m.y - 10, 20, 20), o.letter, tiny);
            }
            foreach (var hq in new[] { cm.ImmuneHQ, cm.InfectionHQ })
            {
                var m = ToMap(hq.transform.position);
                Fill(new Rect(m.x - 9, m.y - 4, 18, 8), TeamColor(hq.Owner) * new Color(1, 1, 1, 0.6f));
            }
            foreach (var c in Combatant.All)
            {
                if (!c.Alive || c == gm.Player.GetComponent<Combatant>()) continue;
                var m = ToMap(c.transform.position);
                Fill(new Rect(m.x - 2, m.y - 2, 4, 4), c.team == Team.Immune ? ImmuneBlue : new Color(1f, 0.35f, 0.3f));
            }

            var pm = ToMap(gm.Player.transform.position);
            Fill(new Rect(pm.x - 3, pm.y - 3, 6, 6), Color.white);
            Vector3 f = gm.Player.transform.forward;
            Fill(new Rect(pm.x + f.x * 7 - 1.5f, pm.y - f.z * 7 - 1.5f, 3, 3), Color.white);
        }

        protected override void DrawRespawn(GameManager gm, float W, float H)
        {
            var cm = ConquestManager.I;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.35f));
            GUI.Label(new Rect(0, H * 0.3f, W, 50), $"Trooper down. Redeploying in {Mathf.CeilToInt(gm.RespawnTimer)}", medium);
            if (!cm) return;

            var options = cm.SpawnOptions(Team.Immune);
            GUI.Label(new Rect(0, H * 0.3f + 44, W, 30), "Choose a spawn point (number keys). Default is the front line.", small.Center());
            for (int i = 0; i < options.Count && i < 9; i++)
            {
                string label = options[i].isHQ ? "Corps HQ" : $"Outpost {options[i].letter}";
                bool chosen = cm.PlayerSpawnChoice == i;
                var row = new Rect(W * 0.5f - 120, H * 0.3f + 80 + i * 30, 240, 26);
                Fill(row, chosen ? new Color(0.3f, 0.55f, 0.9f, 0.8f) : new Color(0f, 0f, 0f, 0.5f));
                GUI.Label(row, $"[{i + 1}]  {label}", tiny);
            }
        }
    }

    static class GuiStyleExtensions
    {
        public static GUIStyle Center(this GUIStyle s) => new GUIStyle(s) { alignment = TextAnchor.UpperCenter };
    }
}
