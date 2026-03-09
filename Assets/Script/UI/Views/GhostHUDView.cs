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
        
        public bool m_canScare = true;
        
        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
        }

        protected void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<GhostHUDView>();
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

        public void DashActivate()
        {
            m_dashIcon.color = Color.red;
            ShowMessage("Dash Start");
        }
        
        public void DashDisabled()
        {
            m_dashIcon.color = Color.white;
            ShowMessage("Dash End");
            m_dashCooldownOverlay.fillAmount = 1f;
            m_dash_disabled = true;
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
            m_sabotageScoreSlider.value = _sabotageScore;
            m_sabotageScoreSlider.maxValue = _maxScoreSabotage;
            m_scoreSabotage.text = _sabotageScore+"$";
            
            m_brokenScoreSlider.value = _brokenScore;
            m_brokenScoreSlider.maxValue = _maxScoreBroken;
            m_scoreBroken.text = _brokenScore+"$";
        }
    }
}
