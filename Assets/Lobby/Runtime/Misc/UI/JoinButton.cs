using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace PurrLobby
{
    public class JoinButton : MonoBehaviour
    {
        [SerializeField] private TMP_InputField roomIdInput;
        [SerializeField] private LobbyManager lobbyManager;
        [SerializeField] private UnityEvent onStartJoin;
        private AudioClip m_buttonClickSound;
        
        private void Start()
        {
            // Load button click sound
            if (m_buttonClickSound == null)
                m_buttonClickSound = Resources.Load<AudioClip>("Audio/MenuButtonSFX");
        }
        
        public void JoinRoom()
        {
            if (string.IsNullOrEmpty(roomIdInput.text))
            {
                Debug.LogWarning($"Can't start join, room ID is empty.");
                return;
            }
            
            PlayButtonClickSound();
            onStartJoin?.Invoke();
            lobbyManager.JoinLobby(roomIdInput.text);
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
