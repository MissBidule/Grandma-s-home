using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
     * @brief  Contains class declaration for the TUTO version of StartLobby
     */

namespace PurrLobby
{
    public class TutoStartLobby : MonoBehaviour
    {
        [SerializeField] private LobbyManager m_lobbyManager;
        public void CreateLobby()
        {
            SceneSwitcher sceneSwitcher = m_lobbyManager.GetComponent<SceneSwitcher>();
            sceneSwitcher._isTuto=true;
            m_lobbyManager.m_loadingCanvas.gameObject.SetActive(true);
            m_lobbyManager.CreateRoom();
        }
    }
}
