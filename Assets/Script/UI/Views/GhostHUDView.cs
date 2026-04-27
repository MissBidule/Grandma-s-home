using System;
using PurrNet;
using System.Collections;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
namespace Script.UI.Views
{
    public class GhostHUDView : GameView
    {
        [Header("Message Panel")]
        [SerializeField] private TMP_Text m_hudMessage;
        [SerializeField] private GameObject m_hudMessagePanel;
        
        [Header("Skills Icons")]
        [SerializeField] private Image m_dashIcon;
        [SerializeField] private Image m_dashCooldownOverlay;
        [SerializeField] private Image m_scaryIcon;
        [SerializeField] private Image m_scaryCooldownOverlay;
        
        [Header("Score parameters")]
        [SerializeField] private Slider m_sabotageScoreSlider;
        [SerializeField] private TMP_Text m_scoreSabotage;
        
        [SerializeField] private Slider m_brokenScoreSlider;
        [SerializeField] private TMP_Text m_scoreBroken;
        
        
        // TODO find way to unserielize
        public bool m_dash_disabled = false;
        private bool m_dash_active = false;

        public bool m_canScare = true;

        private Coroutine m_messageCoroutine;

        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
        }

        protected void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<GhostHUDView>();
        }

        public void ShowMessage(string _message, float _duration = 3f)
        {
            if (!gameObject.activeSelf) return;
            if (m_messageCoroutine != null) StopCoroutine(m_messageCoroutine);
            m_hudMessagePanel.SetActive(true);
            m_hudMessage.text = _message;
            m_messageCoroutine = StartCoroutine(DisappearMessage(_duration));
        }

        private IEnumerator DisappearMessage(float _timer)
        {
            yield return new WaitForSeconds(_timer);
            m_hudMessagePanel.SetActive(false);
            m_hudMessage.text = "";
        }

        public void DashActivate()
        {
            if (m_dash_active) return;
            m_dash_active = true;
            m_dashIcon.color = Color.red;
            ShowMessage("Dash Start");
        }

        public void DashDisabled()
        {
            m_dashIcon.color = Color.white;
            if (m_dash_active)
            {
                m_dash_active = false;
                ShowMessage("Dash End", 1.5f);
            }
            m_dashCooldownOverlay.fillAmount = 1f;
            m_dash_disabled = true;
        }

        public void DashReady()
        {
            m_dash_active = false;
            if (m_dashCooldownOverlay != null)
                m_dashCooldownOverlay.fillAmount = 0f;
            m_dash_disabled = false;
        }

        /*
         * @brief Start the cooldown effect of the dash thing
         * @param the time in seconds
         */
        public void StartDashCooldown(float _time)
        {
            m_dashIcon.color = Color.white;

            m_dashCooldownOverlay.fillAmount = 1f;
            StartCoroutine(IconCooldown(m_dashCooldownOverlay, _time, "Dash cooled-down"));
        }

        public void ScaredActivate(float _timer)
        {
            if (!m_canScare) return;
            if (!gameObject.activeInHierarchy) return;
            m_canScare = false;
            m_scaryCooldownOverlay.fillAmount = 1f;
            StartCoroutine(IconCooldown(m_scaryCooldownOverlay, _timer, "You can scare again"));
        } 

        private IEnumerator IconCooldown(Image _overlay, float _timer, string _endMessage)
        {
            float elapsed = 0f;
            float startFill = 1.0f;
        
            while (elapsed < _timer)
            {
                elapsed += Time.deltaTime;
                _overlay.fillAmount = Mathf.Lerp(startFill, 0f, elapsed / _timer);
                yield return null;
            }
            
            _overlay.fillAmount = 0f;
            
            ShowMessage(_endMessage);
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
                    if (fillImg != null && fillImg.type == Image.Type.Filled)
                        fillImg.fillAmount = ratio;
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
}
