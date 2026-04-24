using System.Linq;
using System;
using PurrNet;
using TMPro;
using UnityEngine;
using WebSocketSharp;
using UnityEngine.UI;
using System.Collections;

namespace PurrLobby
{
    public class UpdateLobby : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_lobbyMaxPlayers;
        [SerializeField] private TMP_InputField m_lobbyName;
        [SerializeField] private TextMeshProUGUI m_serverType;
        [SerializeField] private LobbyManager m_lobbyManager;
        [SerializeField] private const int c_maxPlayersInLobby = 12;
        bool m_lockServerButton = false;

        public void OnServerTypeClicked()
        {
            if (m_lockServerButton) return;
            m_lockServerButton = true;
            if (m_serverType.text == "Public") m_serverType.text = "Private";
            else m_serverType.text = "Public";
            m_lobbyManager.UpdateLobbyType(m_serverType.text == "Private");
            StartCoroutine(ServerButtonCD());
        }

        IEnumerator ServerButtonCD()
        {
            yield return new WaitForSeconds(.5f);
            m_lockServerButton = false;
        }

        public void SaveChanges()
        {
            if (!m_lobbyMaxPlayers.text.IsNullOrEmpty()) {
                if (Convert.ToInt32(m_lobbyMaxPlayers.text) > c_maxPlayersInLobby) m_lobbyMaxPlayers.text = c_maxPlayersInLobby.ToString();
                if (Convert.ToInt32(m_lobbyMaxPlayers.text) < 2) m_lobbyMaxPlayers.text = "2";
                m_lobbyManager.UpdateLobbyMaxPlayer(Convert.ToInt32(m_lobbyMaxPlayers.text));
                m_lobbyMaxPlayers.placeholder.GetComponent<TextMeshProUGUI>().text = "Max players (" + m_lobbyMaxPlayers.text + ")";
                m_lobbyMaxPlayers.text = "";
            }
            if (!m_lobbyName.text.IsNullOrEmpty()) {
                m_lobbyManager.UpdateLobbyName(m_lobbyName.text);
                m_lobbyName.text = "";
            }
        }
    }
}
