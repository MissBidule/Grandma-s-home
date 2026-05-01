using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PurrLobby
{
    public class SceneChangeButton : MonoBehaviour
    {
        [PurrScene, SerializeField] private string scene;
        private AudioClip m_buttonClickSound;
        
        private void Start()
        {
            // Load button click sound
            if (m_buttonClickSound == null)
                m_buttonClickSound = Resources.Load<AudioClip>("Audio/MenuButtonSFX");
        }

        public void ChangeScene()
        {
            PlayButtonClickSound();
            FindAnyObjectByType<LobbyDataHolder>().SetCurrentLobby(default);
            Destroy(FindAnyObjectByType<LobbyManager>().gameObject);
            SceneManager.LoadSceneAsync(scene);
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
