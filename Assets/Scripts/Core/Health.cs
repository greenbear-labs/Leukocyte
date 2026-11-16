using System;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Generic hit points for the player and germs.</summary>
    public class Health : MonoBehaviour
    {
        public float max = 100f;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>World position the most recent damage came from, if known.</summary>
        public Vector3? LastHitFrom { get; private set; }

        public event Action<float> Damaged;
        public event Action Died;

        void Awake() { Current = max; }

        public void SetMax(float value)
        {
            max = value;
            Current = value;
        }

        public void ResetHealth() { Current = max; }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(max, Current + amount);
        }

        public void Damage(float amount, Vector3 from)
        {
            if (IsDead || amount <= 0f) return;
            LastHitFrom = from;
            Damage(amount);
        }

        public void Damage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            if (Current <= 0f) Died?.Invoke();
        }
    }
}
