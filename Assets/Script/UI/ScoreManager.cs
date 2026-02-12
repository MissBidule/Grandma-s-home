using PurrNet;
using TMPro;
using UnityEngine;

/*
 * @brief  Contains class declaration for ScoreManager
 * @details Script that handles the score and updates theUI text
 */
public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager m_Instance;

    [SerializeField] private TMP_Text m_scoreText;

    private int m_score;

    /*
    @brief      Initialise the singleton and refresh the ui
    @return     void
    */
    private void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        m_Instance = this;
        RefreshUI();
    }

    /**
    @brief      Adds points to the score
    @param      _value: value to add
    @return     void
    */
    public void Add(int _value)
    {
        m_score += _value;
        RefreshUI();
    }

    /**
    @brief      resets score
    @return     void
    */
    public void ResetScore()
    {
        m_score = 0;
        RefreshUI();
    }

    /**
    @brief      updates the UI
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
