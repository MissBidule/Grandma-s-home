using UnityEngine;
using UnityEngine.UI;

public class SetDefaultTabColor : MonoBehaviour
{
    [Header("Couleurs")]
    public Color activeColor = new Color(1f, 0.6f, 0f); // Orange
    public Color normalColor = Color.white;            // Blanc/Gris

    [Header("Assigner les objets")]
    public Image tabImage;
    public Toggle tabToggle;

    void Start()
    {
        if (tabToggle != null && tabImage != null)
        {
            // 1. On règle la couleur tout de suite au démarrage
            UpdateVisual(tabToggle.isOn);

            // 2. On dit au Toggle d'appeler "UpdateVisual" à chaque fois qu'on clique
            tabToggle.onValueChanged.AddListener(UpdateVisual);
        }
    }

    // Cette fonction change la couleur selon l'état (vrai ou faux)
    void UpdateVisual(bool isOn)
    {
        tabImage.color = isOn ? activeColor : normalColor;
    }
}