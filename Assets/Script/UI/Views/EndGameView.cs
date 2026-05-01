using System.Threading.Tasks;
using PurrLobby;
using PurrNet;
using Script.States;
using UI;
using UnityEngine;

namespace Script.UI.Views
{
    public class EndGameView : GameView
    {
        [Header("End Game Information")]
        [SerializeField] private GameObject m_hostTools;
        [SerializeField] private GameObject m_clientTools;
        private bool m_alreadyPressed = false;

        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
        }

        protected void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<EndGameView>();
        }

        public void SetupEndGameUI(bool _childWin) { }

        public void EnableHostTools()
        {
            m_hostTools.SetActive(true);
            m_clientTools.SetActive(false);
        }
        
        public void BackToLobby()
        {  
            if (!InstanceHandler.TryGetInstance(out EndGameState endGameState))
                return;
            if (m_alreadyPressed)
                return;
            m_alreadyPressed = true;
            _ = WaitCleanUp(endGameState);
        }

        private async Task WaitCleanUp(EndGameState endGameState)
        {
            string newLobbyId = await FindAnyObjectByType<LobbyManager>().CleanLobby();
            endGameState.StopGame(newLobbyId);
        }
    }
}