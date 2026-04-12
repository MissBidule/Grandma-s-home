using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    public class TutoStartLobby : MonoBehaviour
    {
        [SerializeField] private LobbyManager m_lobbyManager;
        public void CreateLobby()
        {
            Debug.Log("il est là");
            SceneSwitcher sceneSwitcher = m_lobbyManager.GetComponent<SceneSwitcher>();
            sceneSwitcher._isTuto=true;
            m_lobbyManager.m_loadingCanvas.gameObject.SetActive(true);
            m_lobbyManager.CreateRoom();
        }
    }
}
