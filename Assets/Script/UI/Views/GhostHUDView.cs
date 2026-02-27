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
        [Header("HUD Parameters")]
        [SerializeField] private TMP_Text m_hudMessage;
        [SerializeField] private GameObject m_hudMessagePanel;
        [SerializeField] private Image m_dashIcon;
        [SerializeField] private Image m_dashCooldownOverlay;
        
        
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
            if (m_dashIcon.TryGetComponent(out Image dashIcon))
                dashIcon.color = Color.red;
            ShowMessage("Dash Start");
        }
        
        public void DashDisabled()
        {
            if (m_dashIcon.TryGetComponent(out Image dashIcon))
                dashIcon.color = Color.white;
            ShowMessage("Dash End");
            m_dashCooldownOverlay.fillAmount = 1f;
        }

        /*
         * @brief Start the cooldown effect of the dash thing
         * @param the time in seconds
         */
        public void StartDashCooldown(float _time)
        {
            if (m_dashIcon.TryGetComponent(out Image dashIcon))
                dashIcon.color = Color.white;
            
            m_dashCooldownOverlay.fillAmount = 1f;
            StartCoroutine(DashCooldown(_time));
        }

        private IEnumerator DashCooldown(float _timer)
        {
            float elapsed = 0f;
            float startFill = 1.0f;
        
            while (elapsed < _timer)
            {
                elapsed += Time.deltaTime;
                m_dashCooldownOverlay.fillAmount = Mathf.Lerp(startFill, 0f, elapsed / _timer);
                yield return null;
            }
            
            m_dashCooldownOverlay.fillAmount = 0f;
            
            ShowMessage("Dash cooled-down");
        }
    }
}
