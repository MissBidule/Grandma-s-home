using UnityEngine;

namespace PurrLobby
{
    public class LobbyView : MonoBehaviour
    {
        [SerializeField] private CodeButton codeButton;
        [SerializeField] private LobbyManager lobbyManager;

        public void InitCodeButton()
        {
            codeButton.Init(lobbyManager.CurrentLobby.LobbyId);
        }
    }
}
