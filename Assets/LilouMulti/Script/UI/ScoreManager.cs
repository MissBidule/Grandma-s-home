using PurrNet;
using TMPro;
using UnityEngine;

/**
@brief       Script g�rant le score
@details     La classe \c ScoreManager centralise le score et met � jour le texte UI
*/
public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TMP_Text m_scoreText;

    private int m_score;

    /*
    @brief      Initialise le singleton et rafra�chit l'UI
    @return     void
    */
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RefreshUI();
    }

    /**
    @brief      Ajoute des points au score
    @param      _value: valeur � ajouter
    @return     void
    */
    public void Add(int _value)
    {
        m_score += _value;
        RefreshUI();
    }

    /**
    @brief      Remet le score � z�ro
    @return     void
    */
    public void ResetScore()
    {
        m_score = 0;
        RefreshUI();
    }

    /**
    @brief      Met � jour l'UI
    @return     void
    */
    private void RefreshUI()
    {
        if (m_scoreText != null)
        {
            m_scoreText.text = $"Score : {m_score}";
        }
    }
}
