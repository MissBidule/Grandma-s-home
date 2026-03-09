using TMPro;
using UnityEngine;

/**
@brief       Script pour g�rer l'affichage des messages d'interaction dans le jeu
@details     La classe \c InteractPromptUI g�re l'affichage du texte d'interaction.
*/
public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance;

    //[SerializeField] private TMP_Text m_promptText;

    [SerializeField] private GameObject m_canvasPrefab;
    private GameObject m_currentCanvas;
    private TMP_Text m_promptText;
    private void Awake()
    {
        m_Instance = this;
    }

    /**
    @brief      Affiche le message d'interaction
    @param      _message: texte � afficher
    @return     void
    */
    public void Show(string _message)
    {
        if (m_currentCanvas == null)
        {
            m_currentCanvas = Instantiate(m_canvasPrefab);

            m_promptText = m_currentCanvas.GetComponentInChildren<TMP_Text>();
        }

        m_promptText.text = _message;
    }

    /**
    @brief      Masque le message d'interaction
    @return     void
    */
    public void Hide()
    {
        if (m_currentCanvas == null) return;

        Destroy(m_currentCanvas);
        m_currentCanvas = null;
    }
}
