using UnityEngine;

namespace PurrLobby
{
    public class LobbyView : View
    {
        [SerializeField] private CodeButton codeButton;
        [SerializeField] private LobbyNameButton lobbyButton;
        [SerializeField] private LobbyManager lobbyManager;

        public override void OnShow()
        {
            codeButton.Init(lobbyManager.CurrentLobby.LobbyId);
            lobbyButton.Init(lobbyManager.CurrentLobby.Name);
        }
    }
}
