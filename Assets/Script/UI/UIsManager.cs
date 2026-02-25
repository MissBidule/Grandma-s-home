using UnityEngine;
using PurrNet;
using System.Collections;
using UnityEngine.Serialization;

namespace UI
{
    public class UIsManager : MonoBehaviour
    {
        [Header("Game Views")]
        [SerializeField] private GameView[] m_gameViews;
        [SerializeField] private GameView m_defaultView;
        
        [Header("Camera and Listener")]
        [SerializeField] private Camera m_UICamera;
        [SerializeField] private AudioListener m_UIAudioListener;

        [Header("Fade Parameters")]
        [SerializeField] private bool m_fadeViews;
        [SerializeField] [Tooltip("In seconds")] private float m_fadeDuration = 0.75f;
        
        /**
         * @brief Register the Instance and disable the views
         */
        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
        
            foreach (var view in m_gameViews)
            {
                HideViewInternal(view);
            }

            ShowViewInternal(m_defaultView);
        }

        /**
         * @brief Object Destroy function Unregister the UIManager
         */
        private void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<UIsManager>();
        }

        /*
         * @brief Toggle the Camera and Audio listener setup for when there are no players instantiated
         */
        public void ToggleUIVision()
        {
            m_UICamera.enabled = !m_UICamera.enabled;
            m_UIAudioListener.enabled = !m_UIAudioListener.enabled;
        }

        /*
         * @brief Show The View of the given Type
         * @params bool hideOthers -> Hide the other views if true
         */
        public void ShowView<T>(bool hideOthers = true) where T : GameView
        {
            foreach (var view in m_gameViews)
            {
                if (view is T)
                {
                    if (m_fadeViews)
                        StartCoroutine(FadeIn(view));
                    else
                        ShowViewInternal(view);
                }
                else
                {
                    if (hideOthers) {
                        if (m_fadeViews)
                            StartCoroutine(FadeOut(view));
                        else
                            HideViewInternal(view);
                    }
                }
            }
        }

        /*
         * @brief Hide The View of the given Type
         */
        public void HideView<T>() where T : GameView
        {
            foreach (var view in m_gameViews)
            {
                if (view is T)
                {
                    if (m_fadeViews)
                        StartCoroutine(FadeOut(view));
                    else
                        HideViewInternal(view);
                }
            }
        }

        /*
         * @brief Show the View Directly
         */
        private void ShowViewInternal(GameView _view)
        {
            _view.canvasGroup.alpha = 1f;
            _view.OnShow();
        }
        
        /*
         * @brief Hide the View Directly
         */
        private void HideViewInternal(GameView _view)
        {
            _view.canvasGroup.alpha = 0f;
            _view.OnHide();
        }
        
        /*
         * @brief Fade In the View
         */
        private IEnumerator FadeIn(GameView _view)
        {
            float elapsed = 0f;
            float startAlpha = _view.canvasGroup.alpha;
        
            while (elapsed < m_fadeDuration)
            {
                elapsed += Time.deltaTime;
                _view.canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / m_fadeDuration);
                yield return null;
            }
        
            _view.canvasGroup.alpha = 1f;
        }
        
        /*
         * @brief Fade Out the View
         */
        private IEnumerator FadeOut(GameView _view)
        {
            float elapsed = 0f;
            float startAlpha = _view.canvasGroup.alpha;
        
            while (elapsed < m_fadeDuration)
            {
                elapsed += Time.deltaTime;
                _view.canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / m_fadeDuration);
                yield return null;
            }
        
            _view.canvasGroup.alpha = 0f;
        }
        
    }

    public abstract class GameView : MonoBehaviour
    {
        public CanvasGroup canvasGroup;

        public abstract void OnShow();
        public abstract void OnHide();
    }
}