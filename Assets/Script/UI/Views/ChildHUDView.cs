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
        if (!gameObject.activeSelf) return;
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
        float sabotageRemaining = Mathf.Max(0f, _maxScoreSabotage - _sabotageScore);
        if (m_sabotageScoreSlider != null)
        {
            m_sabotageScoreSlider.maxValue = _maxScoreSabotage;
            m_sabotageScoreSlider.value = sabotageRemaining;
            if (m_sabotageScoreSlider.fillRect != null)
            {
                float ratio = _maxScoreSabotage > 0f ? sabotageRemaining / _maxScoreSabotage : 0f;
                var fillImg = m_sabotageScoreSlider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    if (fillImg.type != Image.Type.Filled)
                    {
                        fillImg.type = Image.Type.Filled;
                        fillImg.fillMethod = Image.FillMethod.Horizontal;
                        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                    }
                    fillImg.fillAmount = ratio;
                }
            }
        }
        if (m_scoreSabotage != null)
            m_scoreSabotage.text = sabotageRemaining.ToString("F2");

        float brokenRemaining = Mathf.Max(0f, _maxScoreBroken - _brokenScore);
        if (m_brokenScoreSlider != null)
        {
            m_brokenScoreSlider.maxValue = _maxScoreBroken;
            m_brokenScoreSlider.value = brokenRemaining;
        }
        if (m_scoreBroken != null)
            m_scoreBroken.text = brokenRemaining.ToString("F2") + "$";
    }
}
