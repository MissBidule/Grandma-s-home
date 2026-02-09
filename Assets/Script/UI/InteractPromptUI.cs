using TMPro;
using UnityEngine;

/**
@brief       Script pour gérer l'affichage des messages d'interaction dans le jeu
@details     La classe \c InteractPromptUI gère l'affichage du texte d'interaction.
*/
public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance;

    [SerializeField] private TMP_Text m_promptText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Hide();
    }

    /**
    @brief      Affiche le message d'interaction
    @param      _message: texte à afficher
    @return     void
    */
    public void Show(string _message)
    {
        if (m_promptText == null) return;

        m_promptText.text = _message;
        m_promptText.gameObject.SetActive(true);
    }

    /**
    @brief      Masque le message d'interaction
    @return     void
    */
    public void Hide()
    {
        if (m_promptText == null) return;

        m_promptText.gameObject.SetActive(false);
    }
}
