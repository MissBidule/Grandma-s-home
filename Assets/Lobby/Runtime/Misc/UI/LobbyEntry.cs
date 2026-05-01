using TMPro;
using UnityEngine;

namespace PurrLobby
{
    public class LobbyEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text playersText;

        private Lobby _room;
        private LobbyManager _lobbyManager;
        private AudioClip m_buttonClickSound;
        
        public void Init(Lobby room, LobbyManager lobbyManager)
        {
            lobbyNameText.text = room.Name.Length > 0 ? room.Name : room.LobbyId;
            playersText.text = $"{room.Members.Count}/{room.MaxPlayers}";
            _room = room;
            _lobbyManager = lobbyManager;
            // Load button click sound
            if (m_buttonClickSound == null)
                m_buttonClickSound = Resources.Load<AudioClip>("Audio/MenuButtonSFX");
            //WHEN LEAVE GO BACK PROBLEM
            //TODO
        }

        public void OnClick()
        {
            PlayButtonClickSound();
            _lobbyManager.JoinLobby(_room.LobbyId);
        }

        private void PlayButtonClickSound()
        {
            if (m_buttonClickSound != null)
            {
                var go = new GameObject("ButtonClickSound");
                var audioSource = go.AddComponent<AudioSource>();
                audioSource.clip = m_buttonClickSound;
                audioSource.spatialBlend = 0f;
                audioSource.volume = 0.5f;
                audioSource.Play();
                Destroy(go, m_buttonClickSound.length);
            }
        }
    }
}
