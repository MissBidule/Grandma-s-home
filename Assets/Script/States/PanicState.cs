using System.Collections;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Logging;
using PurrNet.StateMachine;
using Script.UI.Views;
using UnityEngine;

namespace Script.States
{
    public class GhostGameStateData
    {
        public List<GhostController> Ghosts;
        public List<PlayerID> AliveGhosts;
        public List<PlayerID> DeadGhosts;

        public GhostGameStateData(List<GhostController> _ghosts, List<PlayerID> _aliveGhosts, List<PlayerID> _deadGhosts)
        {
            Ghosts = _ghosts;
            AliveGhosts = _aliveGhosts;
            DeadGhosts = _deadGhosts;
        }
    }

    public class PanicState : StateNode<GhostGameStateData>
    {

        [Header("Round Settings")]
        [SerializeField] [Tooltip("Duration of the round in minutes")]
        private float m_roundDuration = 1.0f;

        private List<GhostController> m_ghosts = new();
        private List<PlayerID> m_aliveGhosts = new();
        private List<PlayerID> m_deadGhosts = new();

        private Coroutine m_panicTimer;
        
        private EndGameState m_endGameState;

        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopTimer();
            
            UnregisteringListener();
        }

        private void StopTimer()
        {
            if (m_panicTimer != null)
                StopCoroutine(m_panicTimer);
        }

        private void UnregisteringListener()
        {
            if (!isServer) return;
            
            // Unsubscribe from ghost death events
            foreach (var ghost in m_ghosts)
            {
                if (ghost != null)
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

        public override void Enter(GhostGameStateData _ghostGameStateData, bool _asServer)
        {
            base.Enter(_asServer);

            if (!_asServer)
                return;
            
            foreach (StateNode state in machine.states)
            {
                switch (state)
                {
                    case EndGameState endGameState:
                        m_endGameState = endGameState;
                        break;
                }
            }

            GhostInitialize(_ghostGameStateData);

            if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                PurrLogger.LogError("No Score Manager found", this);
                return;
            }

            scoreManager.m_noticeHouseDestroyed += OnHouseDestroyed;

            m_panicTimer = StartCoroutine(PanicTimer(m_roundDuration * 60));

            RpcPanicTimer();
        }
        
        /*
         * @brief Display the start of panic state and setup for timer if needed in futur 
         */
        [ObserversRpc]
        private void RpcPanicTimer()
        {
            if (InstanceHandler.TryGetInstance(out GhostHUDView ghostHUDView))
                ghostHUDView.ShowMessage("The ghost sabotaged the house panic time.");
        
            if (InstanceHandler.TryGetInstance(out ChildHUDView childHUDView))
                childHUDView.ShowMessage("The ghost sabotaged the house panic time.");
            
            // TODO Do timer stuff
        }

        private void GhostInitialize(GhostGameStateData _ghostGameStateData)
        {
            m_ghosts = _ghostGameStateData.Ghosts;
            m_aliveGhosts = _ghostGameStateData.AliveGhosts;
            m_deadGhosts = _ghostGameStateData.DeadGhosts;

            foreach (GhostController ghostController in m_ghosts)
            {
                if (!ghostController.owner.HasValue)
                    return;

                ghostController.OnDeathChange += OnGhostDeathChange;
            }
        }

        /*
         * @brief The timer of the round, and sun mover
         * @param float _roundDuration !!! In seconds
         */
        private IEnumerator PanicTimer(float _duration)
        {
            SetupPanicMode();
            yield return new WaitForSeconds(_duration);
            PurrLogger.Log("Panic Timer ended", this);
            MoveToEnd(false);
        }

        private void SetupPanicMode()
        {
            // TODO Add all the light and gong stuff
        }

        private void OnGhostDeathChange(bool _deathOrRevive, PlayerID _playerID)
        {
            if (_deathOrRevive)
            {
                // Death Case
                if (m_aliveGhosts.Contains(_playerID))
                {
                    m_aliveGhosts.Remove(_playerID);
                    m_deadGhosts.Add(_playerID);
                }

                if (m_aliveGhosts.Count == 0)
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

        /*
         * @brief react if the house is destroyed and move to end state with child losing
         */
        private void OnHouseDestroyed(bool _destroyed)
        {
            if (_destroyed)
                MoveToEnd(false);
        }

        private void MoveToEnd(bool _childWin)
        {
            StopTimer();
            UnregisteringListener();
            PurrLogger.Log($"Moving to End of game step. ChildWin: {_childWin}");
            machine.SetState(m_endGameState, _childWin); // because the state machine decided it didn't want to go to next set anymore
        }
    }
}