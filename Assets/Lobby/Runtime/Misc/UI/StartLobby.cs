using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    public class StartLobby : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_lobbyName;
        [SerializeField] private LobbyManager m_lobbyManager;
        [SerializeField] private ViewManager m_viewManager;
        public void CreateLobby()
        {
            string lobbyName = m_lobbyName.text;
            m_lobbyManager.CreateRoom(lobbyName);
            m_viewManager.OnRoomCreateClicked();
        }
    }
}
