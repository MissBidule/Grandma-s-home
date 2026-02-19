using System;
using TMPro;
using UnityEngine;
using WebSocketSharp;

namespace PurrLobby
{
    public class UpdateLobby : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_lobbyMaxPlayers;
        [SerializeField] private TextMeshProUGUI m_serverType;
        [SerializeField] private LobbyManager m_lobbyManager;
        [SerializeField] private const int c_maxPlayersInLobby = 12;
        public void OnServerTypeClicked()
        {
            if (m_serverType.text == "Public") m_serverType.text = "Private";
            else m_serverType.text = "Public";
        }

        public void SaveChanges()
        {
            m_lobbyManager.UpdateLobbyType(m_serverType.text == "Private");
            if (!m_lobbyMaxPlayers.text.IsNullOrEmpty()) {
                if (Convert.ToInt32(m_lobbyMaxPlayers.text) > c_maxPlayersInLobby) m_lobbyMaxPlayers.text = c_maxPlayersInLobby.ToString();
                if (Convert.ToInt32(m_lobbyMaxPlayers.text) < 2) m_lobbyMaxPlayers.text = "2";
                m_lobbyManager.UpdateLobbyMaxPlayer(Convert.ToInt32(m_lobbyMaxPlayers.text));
                m_lobbyMaxPlayers.placeholder.GetComponent<TextMeshProUGUI>().text = "Max players (" + m_lobbyMaxPlayers.text + ")";
            }
            gameObject.SetActive(false);
        }
    }
}
