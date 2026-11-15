using System.Collections;
using UnityEngine;

namespace Leukocyte
{
    public enum GameState { Playing, Won, Lost }

    /// <summary>Match rules: Host Health, player lives, win and lose.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public float hostMax = 100f;
        public int startingLives = 3;
        public float respawnDelay = 3f;

        public GameState State { get; private set; } = GameState.Playing;
        public float HostHealth { get; private set; }
        public int Lives { get; private set; }
        public int Kills { get; set; }
        public float RespawnTimer { get; private set; }
        public string EndReason { get; private set; } = "";

        public PlayerController Player { get; private set; }
        public WaveManager Waves { get; private set; }
        public Hud Hud { get; private set; }

        Vector3 spawnPoint;

        void Awake()
        {
            I = this;
            HostHealth = hostMax;
            Lives = startingLives;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        public void Init(PlayerController player, WaveManager waves, Hud hud, Vector3 spawn)
        {
            Player = player;
            Waves = waves;
            Hud = hud;
            spawnPoint = spawn;
            player.Hp.Died += OnPlayerDied;
        }

        public void DamageHost(float amount)
        {
            if (State != GameState.Playing) return;
            HostHealth = Mathf.Max(0f, HostHealth - amount);
            if (Hud) Hud.HostHit();
            if (HostHealth <= 0f) Lose("The infection reached the tissue. The Host has gone critical.");
        }

        void OnPlayerDied()
        {
            if (State != GameState.Playing) return;
            Lives--;
            if (Lives <= 0) Lose("Your trooper was overwhelmed. No reinforcements left.");
            else StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            RespawnTimer = respawnDelay;
            while (RespawnTimer > 0f)
            {
                RespawnTimer -= Time.deltaTime;
                yield return null;
            }
            RespawnTimer = 0f;
            if (State == GameState.Playing) Player.Respawn(spawnPoint);
        }

        public void Win()
        {
            if (State != GameState.Playing) return;
            State = GameState.Won;
            EndReason = "Infection cleared. The femoral artery is secure.";
            ReleaseCursor();
        }

        void Lose(string reason)
        {
            if (State != GameState.Playing) return;
            State = GameState.Lost;
            EndReason = reason;
            ReleaseCursor();
        }

        static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (State != GameState.Playing && Input.GetKeyDown(KeyCode.Return) && GameBootstrap.I)
                GameBootstrap.I.Restart();
        }
    }
}
