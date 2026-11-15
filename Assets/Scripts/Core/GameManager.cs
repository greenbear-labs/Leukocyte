using System.Collections;
using UnityEngine;

namespace Leukocyte
{
    public enum GameState { Playing, Won, Lost }
    public enum GameMode { ArteryDefense, TissueWar }

    /// <summary>Match rules shared by both modes: state, player respawns, win and lose.
    /// Artery Defense uses Host Health and lives; Tissue War hands scoring to the ConquestManager.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public float hostMax = 100f;
        public int startingLives = 3;
        public float respawnDelay = 3f;

        public GameMode Mode { get; private set; }
        public GameState State { get; private set; } = GameState.Playing;
        public float HostHealth { get; private set; }
        public int Lives { get; private set; }
        public int Kills { get; set; }
        public float RespawnTimer { get; private set; }
        public string EndReason { get; private set; } = "";

        public PlayerController Player { get; private set; }
        public WaveManager Waves { get; private set; }
        public HudBase Hud { get; private set; }

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

        public void Init(GameMode mode, PlayerController player, HudBase hud, Vector3 spawn, WaveManager waves = null)
        {
            Mode = mode;
            Player = player;
            Hud = hud;
            Waves = waves;
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
            if (Mode == GameMode.TissueWar)
            {
                if (ConquestManager.I) ConquestManager.I.OnUnitDied(Team.Immune);
                if (State == GameState.Playing) StartCoroutine(RespawnRoutine());
                return;
            }

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
            if (State != GameState.Playing) yield break;

            if (Mode == GameMode.TissueWar && ConquestManager.I)
            {
                var spot = ConquestManager.I.PlayerSpawn(out var facing);
                Player.Respawn(spot, facing);
            }
            else
            {
                Player.Respawn(spawnPoint);
            }
        }

        public void Win(string reason)
        {
            if (State != GameState.Playing) return;
            State = GameState.Won;
            EndReason = reason;
            ReleaseCursor();
        }

        public void Lose(string reason)
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
            if (!GameBootstrap.I) return;
            bool menuKey = Input.GetKeyDown(KeyCode.M) && (State != GameState.Playing || !Player.InputEnabled);
            if (menuKey) GameBootstrap.I.ReturnToMenu();
            else if (State != GameState.Playing && Input.GetKeyDown(KeyCode.Return)) GameBootstrap.I.Restart();
        }
    }
}
