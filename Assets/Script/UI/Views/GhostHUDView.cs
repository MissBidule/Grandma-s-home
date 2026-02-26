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
        [SerializeField] private Image m_sprintIcon;
        [SerializeField] private Image m_sprintCooldownOverlay;
        
        
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

        public void SprintActivate()
        {
            if (m_sprintIcon.TryGetComponent(out Image sprintIcon))
                sprintIcon.color = Color.red;
            ShowMessage("Sprint Start");
        }

        /*
         * @brief Start the cooldown effect of the sprint thing
         * @param the time in seconds
         */
        public void StartSprintCooldown(float _time)
        {
            if (m_sprintIcon.TryGetComponent(out Image sprintIcon))
                sprintIcon.color = Color.white;
            
            m_sprintCooldownOverlay.fillAmount = 1f;
            StartCoroutine(SprintCooldown(_time));
        }

        private IEnumerator SprintCooldown(float _timer)
        {
            float elapsed = 0f;
            float startFill = 1.0f;
        
            while (elapsed < _timer)
            {
                elapsed += Time.deltaTime;
                m_sprintCooldownOverlay.fillAmount = Mathf.Lerp(startFill, 0f, elapsed / _timer);
                yield return null;
            }
            
            m_sprintCooldownOverlay.fillAmount = 0f;
            
            ShowMessage("Sprint cooled-down");
        }
    }
}
