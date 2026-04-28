using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PurrLobby;
using PurrNet;
using PurrNet.Logging;
using PurrNet.StateMachine;
using Script.Audio;
using UnityEngine;

namespace Script.States
{
    /*
     * @brief  Contains class declaration for the state RoundRunningState
     * @details Script that will handle the main gameplay loop sequence
     */
    public class RoundRunningState : StateNode<List<PlayerControllerCore>>
    {
        [Header("Round Settings")]
        [SerializeField] [Tooltip("Duration of the round in secondes")] private float m_roundDuration;
        public float m_remainingTime { get; private set; } // Accessible by all, only modifiable in this class.
        [SerializeField] [Tooltip("Start duration")] private float m_startDelay = 10f;

        [Header("Door")]
        [SerializeField] [Tooltip("The DOOR")] private StartingDoor m_startingDoor;
        // TODO Skybox & directional light reference.

        // Panic State Visibility
        [NonSerialized] public bool m_isPanic = false;

        // State Reference
        private PanicState m_panicState;
        private EndGameState m_endGameState;
        
        // Players Info
        private List<PlayerID> m_playerIds = new();
        private List<GhostController> m_ghosts = new();
        private List<ChildController> m_childs = new();
        
        private List<PlayerID> m_aliveGhosts = new();
        private List<PlayerID> m_deadGhosts = new();
        
        // Sabotage Info
        private List<SabotageObject> m_sabotageObjects;
        
        // Coroutine
        private Coroutine m_roundTimer;

        private RoleKeeper m_roleKeeper;

        public override void Enter(List<PlayerControllerCore> _players, bool _asServer)
        {
            base.Enter(_players, _asServer);
            
            if(!_asServer)
                return;

            foreach (StateNode state in machine.states)
            {
                switch (state)
                {
                    case PanicState panicState:
                        m_panicState = panicState;
                        break;
                    case EndGameState endGameState:
                        m_endGameState = endGameState;
                        break;
                }
            }
            
            // Play Music
            StartGameMusic();

            ClearLists();
            
            RegisteringListener(_players);

            m_roundTimer = StartCoroutine(RoundTimer(m_roundDuration));

            m_roleKeeper = FindAnyObjectByType<RoleKeeper>();
        }
        
        [ObserversRpc(bufferLast: true)]
        public void StartGameMusic()
        {
            if (MusicLooper.Instance == null)
                return;
            MusicLooper.Instance.PlayMusic(MusicTrack.Game);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopTimer();

            UnregisteringListener();
        }

        private void StopTimer()
        {
            if (m_roundTimer != null)
                StopCoroutine(m_roundTimer);
        }

        private void UnregisteringListener()
        {
            if (!isServer) return;
            
            // Unsubscribe from ghost death events
            foreach (GhostController ghost in m_ghosts.Where(_ghost => _ghost != null))
            {
                ghost.OnDeathChange -= OnGhostDeathChange;
            }
            
            // Unsubscribe from score events
            if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                PurrLogger.LogError("No Score Manager found", this);
                return;
            }

            scoreManager.m_noticeHouseDestroyed -= OnHouseDestroyed;
        }

        private void ClearLists()
        {
            m_playerIds.Clear();
            m_ghosts.Clear();
            m_childs.Clear();
            
            m_aliveGhosts.Clear();
            m_deadGhosts.Clear();
        }

        public void SetSabotageObjects(List<SabotageObject> _sabotageObjects)
        {
            m_sabotageObjects = _sabotageObjects;
        }

        private void RegisteringListener(List<PlayerControllerCore> _players)
        {
            foreach (PlayerControllerCore player in _players)
            {
                if (!player.owner.HasValue)
                    return;
                m_playerIds.Add(player.owner.Value);
                switch (player)
                {
                    case GhostController controller:
                        m_ghosts.Add(controller);
                        PurrLogger.Log($"Ghost {player.owner.Value} Registered", this);
                        controller.OnDeathChange += OnGhostDeathChange;
                        m_aliveGhosts.Add(player.owner.Value);
                        break;

                    case ChildController controller:
                        PurrLogger.Log($"Child {player.owner.Value} Registered", this);
                        m_childs.Add(controller);
                        break;
                    
                    default:
                        PurrLogger.LogError($"Unknown PType player ID : {player.owner.HasValue}");
                        break;
                }
            }
            
            if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                PurrLogger.LogError("No Score Manager found", this);
                return;
            }

            scoreManager.m_noticeHouseDestroyed += OnHouseDestroyed;
        }

        /*
         * @brief The timer of the round, and sun mover
         * @param float _roundDuration !!! In seconds
         */
        private IEnumerator RoundTimer(float _roundDuration)
        {
            // Call Ethan day/night cycle
            StartDayNight(_roundDuration + m_startDelay, DateTime.Now.Millisecond);
            
            // Wait for players to settle in the starting room before opening the doors
            yield return new WaitForSeconds(m_startDelay);

            m_startingDoor.OpenDoors();

            SabotageManager sabotageManager = FindAnyObjectByType<SabotageManager>();
            sabotageManager?.Initialize();
            
            PurrLogger.Log($"Round Duration {_roundDuration}s");

            m_remainingTime = _roundDuration;

            while (m_remainingTime > 0)
            {
                yield return new WaitForSeconds(1f);
                m_remainingTime -= 1f;
            }

            // Time ended
            PurrLogger.Log("Round Timer Ended", this);
            MoveToEnd(true);
        }

        [ObserversRpc(bufferLast:true)]
        public void StartDayNight(float _roundDuration, int _seed)
        {
            if (InstanceHandler.TryGetInstance(out LightTimer lightTimer))
                lightTimer.StartLightSystem(_roundDuration, _seed);
        }
        
        // Action Reactions
        private void OnGhostDeathChange(bool _deathOrRevive, PlayerID _playerID)
        {
            PurrLogger.Log($"Ghost death: {_deathOrRevive}, PlayerID: {_playerID}");
            if (_deathOrRevive)
            {
                // Death Case
                if (m_aliveGhosts.Contains(_playerID))
                {
                    m_aliveGhosts.Remove(_playerID);
                    m_deadGhosts.Add(_playerID);
                }

                if (m_deadGhosts.Count + m_roleKeeper.GetDisconnectedPlayers().Count >= m_ghosts.Count)
                {
                    MoveToEnd(true);
                }
            }
            else
            {
                // Revive Case
                if (!m_deadGhosts.Contains(_playerID)) return;
                m_deadGhosts.Remove(_playerID);
                m_aliveGhosts.Add(_playerID);
            }
        }

        private void OnHouseDestroyed(bool _isDestroyedOrJustSabotaged)
        {
            PurrLogger.Log($"House destroyed: {_isDestroyedOrJustSabotaged} false = sabotaged");
            if (_isDestroyedOrJustSabotaged)
            {
                MoveToEnd(false);
            }
            else
            {
                MoveToPanic();
            }
        }
        
        
        // State Movement
        
        private void MoveToPanic()
        {
            PurrLogger.Log($"Moving to Panic State");
            StopTimer();
            UnregisteringListener();
            m_isPanic = true;
            machine.SetState(m_panicState, new GhostGameStateData(m_ghosts, m_aliveGhosts, m_deadGhosts));
        }
        
        private void MoveToEnd(bool _childWin)
        {
            PurrLogger.Log($"Moving to End of game step child in:{_childWin}");
            StopTimer();
            UnregisteringListener();
            machine.SetState(m_endGameState, _childWin);
        }
    }

}