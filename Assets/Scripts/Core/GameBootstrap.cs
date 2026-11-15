using System.Collections;
using UnityEngine;

namespace Leukocyte
{
    /// <summary>Drop this on an empty GameObject in an empty scene and press Play.
    /// It builds the whole Femoral Artery greybox level from code.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap I { get; private set; }

        public int seed = 1337;
        GameObject level;

        void Awake() { I = this; }

        void Start() { Build(); }

        void Build()
        {
            Random.InitState(seed);
            level = LevelBuilder.Build();
        }

        public void Restart() { StartCoroutine(RestartRoutine()); }

        IEnumerator RestartRoutine()
        {
            if (level) Destroy(level);
            yield return null;
            Build();
        }
    }
}
