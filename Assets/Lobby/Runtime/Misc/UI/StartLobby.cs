using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    public class StartLobby : MonoBehaviour
    {
        [SerializeField] private LobbyManager m_lobbyManager;
        public void CreateLobby()
        {
            m_lobbyManager.m_loadingCanvas.gameObject.SetActive(true);
            m_lobbyManager.CreateRoom();
        }
    }
}
