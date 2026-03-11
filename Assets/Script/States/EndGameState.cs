using PurrNet;
using PurrNet.Logging;
using PurrNet.StateMachine;
using Script.UI.Views;
using System;
using UI;
using UnityEngine;

namespace Script.States
{
    public class EndGameState : StateNode<bool>
    {
        private PlayerSpawningState m_spawnState;
        
        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
            
            foreach (StateNode state in machine.states)
            {
                if (state is PlayerSpawningState playerSpawningState)
                    m_spawnState = playerSpawningState;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InstanceHandler.UnregisterInstance<EndGameState>();
        }

        public override void Enter(bool _childWin, bool _asServer)
        {
            base.Enter(_asServer);
            
            PurrLogger.Log($"End Game childWin {_childWin} | Server asServer {_asServer}");
            
            if (!_asServer)
                return;
            
            SetupEndGameUI(_childWin);
            
            if (!InstanceHandler.TryGetInstance(out EndGameView endGameView))
                return;
            endGameView.EnableHostTools();
        }
        
        public void ResetGame()
        {
            if (!isServer)
                return;
            PurrLogger.Log("Reset Game");
            if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
                return;
            scoreManager.ResetScore();
            
            if (!InstanceHandler.TryGetInstance(out UIsManager uisManager))
                return;
            uisManager.HideView<EndGameView>();
            machine.SetState(m_spawnState);
        }

        [ObserversRpc]
        private void SetupEndGameUI(bool _childWin)
        {
            // Free the cursor (it's way harder to click on the button without it)
            Cursor.lockState = CursorLockMode.None;
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