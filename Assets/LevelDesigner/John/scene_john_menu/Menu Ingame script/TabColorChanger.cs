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
    
    // AJOUT : Le ScrollRect à remonter
    public ScrollRect associatedScrollRect; 

    void Start()
    {
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
            associatedScrollRect.verticalNormalizedPosition = 1f;
            Debug.Log("Scroll Reset pour : " + gameObject.name);
        }
    }
}