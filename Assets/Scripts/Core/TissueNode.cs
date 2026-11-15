using System.Collections.Generic;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Vital tissue the germs try to reach. Each germ that touches it drains Host Health.</summary>
    public class TissueNode : MonoBehaviour
    {
        public static readonly List<TissueNode> All = new List<TissueNode>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public float radius = 1.6f;

        Renderer body;
        Color baseColor;
        Vector3 baseScale;
        float infectFlash;
        float phase;

        void Awake()
        {
            body = GetComponentInChildren<Renderer>();
            baseColor = body.material.color;
            baseScale = body.transform.localScale;
            phase = Random.value * 6f;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Infect(float hostDamage)
        {
            infectFlash = 0.5f;
            if (GameManager.I) GameManager.I.DamageHost(hostDamage);
        }

        void Update()
        {
            float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 2.2f + phase);
            body.transform.localScale = baseScale * pulse;
            if (infectFlash > 0f) infectFlash -= Time.deltaTime;
            body.material.color = Color.Lerp(baseColor, new Color(0.4f, 0.9f, 0.2f), Mathf.Clamp01(infectFlash * 2f));
        }
    }
}
