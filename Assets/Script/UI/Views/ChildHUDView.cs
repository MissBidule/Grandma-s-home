using System.Collections;
using PurrNet;
using PurrNet.Logging;
using Script.UI.Views;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class ChildHUDView : GameView
{
    [Header("Message Panel")]
    [SerializeField] private TMP_Text m_hudMessage;
    [SerializeField] private GameObject m_hudMessagePanel;
    
    [Header("Scarred Debuff")]
    [SerializeField] private Image m_scaredIcon;
    [SerializeField] private Image m_scaredCooldownOverlay;
    public bool m_isScared = false;
    
    [Header("Score parameters")]
    [SerializeField] private Slider m_sabotageScoreSlider;
    [SerializeField] private TMP_Text m_scoreSabotage;
        
    [SerializeField] private Slider m_brokenScoreSlider;
    [SerializeField] private TMP_Text m_scoreBroken;
    
    
    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        if (m_hudMessagePanel == null)
            PurrLogger.LogWarning("hudMessagePanel is null");
        // TODO The other null check 
        
    }

    protected void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ChildHUDView>();
    }
    
    public void ShowMessage(string _message)
    {
        m_hudMessagePanel.SetActive(true);
        m_hudMessage.text = _message;
        StartCoroutine(DisappearMessage(3));
    }

    private IEnumerator DisappearMessage(float _timer)
    {
        yield return new WaitForSeconds(_timer);
        m_hudMessagePanel.SetActive(false);
        m_hudMessage.text = "";
    }

    public void StartScared(float _timer)
    {
        if (m_isScared) return; // Prevent to start everything several times
        m_isScared = true;
        m_scaredIcon.enabled = true;
        m_scaredCooldownOverlay.enabled = true;
        m_scaredCooldownOverlay.fillAmount = 1f;
        ShowMessage("You've been scared!");
        StartCoroutine(DebuffOverlay(_timer));
    }

    private IEnumerator DebuffOverlay(float _timer)
    {
        float elapsed = 0f;
        float startFill = 1.0f;
        
        while (elapsed < _timer)
        {
            elapsed += Time.deltaTime;
            m_scaredCooldownOverlay.fillAmount = Mathf.Lerp(startFill, 0f, elapsed / _timer);
            yield return null;
        }
            
        m_scaredCooldownOverlay.fillAmount = 0f;
            
        ShowMessage("You are not scared!");
        m_scaredIcon.enabled = false;
        m_scaredCooldownOverlay.enabled = false;
    }
    
    public void UpdateScore(float _sabotageScore, float _maxScoreSabotage, int _brokenScore, float _maxScoreBroken)
    {
        m_sabotageScoreSlider.value = _sabotageScore;
        m_sabotageScoreSlider.maxValue = _maxScoreSabotage;
        m_scoreSabotage.text = _sabotageScore+"$";
            
        m_brokenScoreSlider.value = _brokenScore;
        m_brokenScoreSlider.maxValue = _maxScoreBroken;
        m_scoreBroken.text = _brokenScore+"$";
    }
}
