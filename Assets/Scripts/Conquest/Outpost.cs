using UnityEngine;

namespace Leukocyte
{
    /// <summary>A capturable outpost. Control runs from -1 (Infection) to +1 (Immune).
    /// Standing in the ring with no enemies present pushes control toward your side;
    /// a post goes neutral when control crosses zero and flips when it reaches the end.</summary>
    public class Outpost : MonoBehaviour
    {
        public string letter = "A";
        public float radius = 9f;
        public float captureSeconds = 10f;
        public bool isHQ;

        public float Control { get; private set; }
        public Team? Owner { get; private set; }
        public int ImmuneCount { get; private set; }
        public int InfectionCount { get; private set; }
        public bool Contested => ImmuneCount > 0 && InfectionCount > 0;

        Renderer flag;
        Renderer ring;
        Light glow;
        Transform flagPivot;

        public void Init(string id, Team? startOwner, Renderer flagRenderer, Renderer ringRenderer, Light light, Transform pivot)
        {
            letter = id;
            Owner = startOwner;
            Control = startOwner == Team.Immune ? 1f : startOwner == Team.Infection ? -1f : 0f;
            flag = flagRenderer;
            ring = ringRenderer;
            glow = light;
            flagPivot = pivot;
            RefreshVisuals();
        }

        public bool Contains(Vector3 p)
        {
            Vector3 d = p - transform.position;
            return d.y < 8f && d.y > -2f && d.x * d.x + d.z * d.z < radius * radius;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (!gm || gm.State != GameState.Playing) return;

            int immune = 0, infection = 0;
            foreach (var c in Combatant.All)
            {
                if (!c.Alive || !Contains(c.transform.position)) continue;
                if (c.team == Team.Immune) immune++;
                else infection++;
            }
            ImmuneCount = immune;
            InfectionCount = infection;

            if (isHQ) return;

            float rate = Time.deltaTime / captureSeconds;
            if (immune > 0 && infection == 0) Control += rate * Mathf.Min(immune, 3);
            else if (infection > 0 && immune == 0) Control -= rate * Mathf.Min(infection, 3);
            else if (immune == 0 && infection == 0 && Owner.HasValue)
            {
                // Slowly recover toward the owner when nobody is around.
                float target = Owner == Team.Immune ? 1f : -1f;
                Control = Mathf.MoveTowards(Control, target, rate * 0.5f);
            }
            Control = Mathf.Clamp(Control, -1f, 1f);

            var before = Owner;
            if (Owner == Team.Immune && Control <= 0f) Owner = null;
            else if (Owner == Team.Infection && Control >= 0f) Owner = null;
            if (Control >= 1f) Owner = Team.Immune;
            else if (Control <= -1f) Owner = Team.Infection;

            if (before != Owner && ConquestManager.I) ConquestManager.I.OnOutpostChanged(this, before);
            RefreshVisuals();
        }

        void RefreshVisuals()
        {
            Color c = Owner == Team.Immune ? new Color(0.35f, 0.65f, 1f)
                : Owner == Team.Infection ? new Color(0.45f, 0.9f, 0.2f)
                : new Color(0.75f, 0.75f, 0.75f);
            if (flag) flag.material.color = c;
            if (ring)
            {
                Color pull = Control >= 0f ? new Color(0.35f, 0.65f, 1f) : new Color(0.45f, 0.9f, 0.2f);
                ring.material.color = Color.Lerp(new Color(0.75f, 0.75f, 0.75f), pull, Mathf.Abs(Control));
            }
            if (glow) glow.color = c;
            if (flagPivot) flagPivot.localPosition = new Vector3(0f, 1.5f + 5f * Mathf.Abs(Control), 0f);
        }
    }
}
