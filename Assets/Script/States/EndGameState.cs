using PurrLobby;
using PurrNet;
using PurrNet.Logging;
using PurrNet.StateMachine;
using Script.Audio;
using Script.UI.Views;
using System;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Script.States
{
    public class EndGameState : StateNode<bool>
    {
        [PurrScene, SerializeField] private string m_lobbyScene;
        
        public bool IsGameOver { get; private set; }

        private PlayerSpawningState m_spawnState;
        private bool _hasAlreadySwitched = false;

        [SerializeField] private GameObject pauseMenu;

        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
            IsGameOver = false;
            
            _hasAlreadySwitched = false; // Reset flag on start to allow scene switching in new lobby sessions
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InstanceHandler.UnregisterInstance<EndGameState>();
        }

        public override void Enter(bool _childWin, bool _asServer)
        {
            IsGameOver = true;
            base.Enter(_asServer);

            foreach (StateNode state in machine.states)
            {
                if (state is PlayerSpawningState playerSpawningState)
                    m_spawnState = playerSpawningState;
            }

            PurrLogger.Log($"End Game childWin {_childWin} | Server asServer {_asServer}");

            ReleaseLocalPlayerControl();

            if (!_asServer)
                return;

            HidePause();

            SetupEndGameUI(_childWin);
            if (InteractPromptUI.m_Instance != null) InteractPromptUI.m_Instance.Hide();

            if (!InstanceHandler.TryGetInstance(out EndGameView endGameView))
                return;
            endGameView.EnableHostTools();
        }

        private static void ReleaseLocalPlayerControl()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Only disable PlayerInput on actual player avatars (have a PlayerControllerCore)
            // to avoid breaking UI or tutorial PlayerInput setups.
            foreach (var core in FindObjectsByType<PlayerControllerCore>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (core == null) continue;
                var pi = core.GetComponent<PlayerInput>();
                if (pi != null) pi.enabled = false;
            }
        }

        [ObserversRpc(runLocally: true)]
        public void HidePause()
        {
            if (pauseMenu != null) {
                pauseMenu.GetComponent<PauseMenuView>().Resume();
                //Destroy(pauseMenu);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (MusicLooper.Instance != null) MusicLooper.Instance.StopMusic();
        }
        
        [ObserversRpc]
        public void BackToLobby(string newLobbyId = "")
        {
            // Prevent duplicate scene switches
            if (_hasAlreadySwitched)
            {
                PurrLogger.LogWarning("SwitchScene already called - ignoring duplicate", this);
                return;
            }
            
            _hasAlreadySwitched = true;
            
            if (string.IsNullOrEmpty(m_lobbyScene))
            {
                PurrLogger.LogError("Next scene name is not set!", this);
                return;
            }

            if (!string.IsNullOrEmpty(newLobbyId))
            {
                FindAnyObjectByType<LobbyDataHolder>().SetNewID(newLobbyId);
            }

            PurrLogger.Log($"Switching to scene: {m_lobbyScene}", this);
            
            // Load game scene - ConnectionStarter in new scene will handle network initialization
            SceneManager.LoadSceneAsync(m_lobbyScene);
        }

        public void StopGame(string newLobbyId = "") {
            Destroy(FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include).gameObject);
            BackToLobby(newLobbyId);
        }

        public void BackToMenu()
        {
            StartCoroutine(FindAnyObjectByType<LobbyManager>().RemovePlayerAndDestroy());
            
            PurrLogger.Log("Returning to menu.", this);
            FindAnyObjectByType<LobbyDataHolder>().SetCurrentLobby(default);

            if (string.IsNullOrEmpty(m_lobbyScene))
            {
                PurrLogger.LogError("Next scene name is not set!", this);
                return;
            }

            PurrLogger.Log($"Switching to scene: {m_lobbyScene}", this);
            
            // Load game scene - ConnectionStarter in new scene will handle network initialization
            SceneManager.LoadSceneAsync(m_lobbyScene);
        }

        public void ServerLost()
        {
            PurrLogger.LogWarning("Server is not accessible. Returning to menu.", this);
            FindAnyObjectByType<LobbyDataHolder>().SetCurrentLobby(default);
            StopGame();
        }

        [ObserversRpc(runLocally: true)]
        private void SetupEndGameUI(bool _childWin)
        {
            // Free the cursor (it's way harder to click on the button without it)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ReleaseLocalPlayerControl();
            PurrLogger.Log("Setting up EndGameUI", this);
            if (!InstanceHandler.TryGetInstance(out EndGameView endGameView))
                return;
            
            endGameView.SetupEndGameUI(_childWin);
            
            if (!InstanceHandler.TryGetInstance(out UIsManager uisManager))
                return;
            
            uisManager.ShowView<EndGameView>();
            uisManager.ToggleUIVision();

        }
    }
}