using UnityEngine;
using UnityEngine.UI;

public class ResetScrollOnEnable : MonoBehaviour
{
    private ScrollRect scrollRect;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    // S'exécute chaque fois que l'onglet est activé
    void OnEnable()
    {
        if (scrollRect != null)
        {
            // 1.0f signifie tout en haut, 0.0f tout en bas
            scrollRect.verticalNormalizedPosition = 1.0f;
        }
    }
}