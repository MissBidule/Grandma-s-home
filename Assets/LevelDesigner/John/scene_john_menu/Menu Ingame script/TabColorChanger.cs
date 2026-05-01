using UnityEngine;
using UnityEngine.UI;

public class SetDefaultTabColor : MonoBehaviour
{
    [Header("Couleurs")]
    public Color activeColor = new Color(1f, 0.6f, 0f); // Orange
    public Color normalColor = Color.white;            // Blanc

    [Header("Assigner les objets")]
    public Image tabImage;
    public Toggle tabToggle;
    
    // AJOUT : Le ScrollRect � remonter
    public ScrollRect associatedScrollRect;     private AudioClip m_buttonClickSound;
    void Start()
    {
        // Load default button click sound
        if (m_buttonClickSound == null)
            m_buttonClickSound = Resources.Load<AudioClip>("Audio/MenuButtonSFX");
        
        if (tabToggle != null && tabImage != null)
        {
            UpdateVisual(tabToggle.isOn);
            tabToggle.onValueChanged.AddListener(UpdateVisual);
        }
    }

    void UpdateVisual(bool isOn)
    {
        // 1. Change la couleur
        //tabImage.color = isOn ? activeColor : normalColor;

        // 2. AJOUT : Si on active l'onglet, on reset le scroll en haut
        if (isOn && associatedScrollRect != null)
        {
            PlayButtonClickSound();
            associatedScrollRect.verticalNormalizedPosition = 1f;
            Debug.Log("Scroll Reset pour : " + gameObject.name);
        }
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