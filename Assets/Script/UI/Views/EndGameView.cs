using PurrNet;
using Script.States;
using TMPro;
using UI;
using UnityEngine;

namespace Script.UI.Views
{
    public class EndGameView : GameView
    {
        [Header("End Game Information")]
        [SerializeField] private TMP_Text m_winnerText;
        [SerializeField] private GameObject m_hostTools;

        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
        }

        protected void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<EndGameView>();
        }

        public void SetupEndGameUI(bool _childWin)
        {
            m_winnerText.text = _childWin ? "The children have won." : "The ghosts have won";
        }

        public void EnableHostTools()
        {
            m_hostTools.SetActive(true);
        }
        
        public void ResetGame()
        {
            m_hostTools.SetActive(false);
            if (!InstanceHandler.TryGetInstance(out EndGameState endGameState))
                return;
            endGameState.ResetGame();
        }

        public void BackToLobby()
        {
            if (!InstanceHandler.TryGetInstance(out EndGameState endGameState))
                return;
            endGameState.BackToLobby();
        }
    }
}