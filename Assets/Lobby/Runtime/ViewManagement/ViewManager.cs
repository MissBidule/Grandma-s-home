using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PurrLobby
{
    public class ViewManager : MonoBehaviour
    {
        [SerializeField] private List<View> allViews = new();
        [SerializeField] private View defaultView;
        [SerializeField] List<GameObject> objectsForHost = new();

        // Tracks the view that was active before opening Options via gamepad
        private View m_viewBeforeOptions;

        private void Start()
        {
            foreach (var view in allViews)
            {
                HideViewInternal(view);
            }
            ShowViewInternal(defaultView);
        }

        private bool m_wasGamepadConnected;

        private View GetCurrentActiveView()
        {
            foreach (var v in allViews)
                if (v != null && v.canvasGroup.interactable) return v;
            return null;
        }

        private void Update()
        {
            var gp = Gamepad.current;
            bool isGamepad = gp != null;

            // Auto-select first button when a gamepad is plugged in
            if (isGamepad && !m_wasGamepadConnected)
            {
                var current = GetCurrentActiveView();
                if (current != null)
                    SelectFirstIn(current.gameObject);
            }
            m_wasGamepadConnected = isGamepad;

            if (!isGamepad) return;

            if (gp.startButton.wasPressedThisFrame)
            {
                var optionsView = GetViewOfType<OptionsView>();
                if (optionsView != null && optionsView.canvasGroup.interactable)
                    CloseOptionsGamepad();
                else
                    OpenOptionsGamepad();
                return;
            }

            if (gp.buttonEast.wasPressedThisFrame)
                HandleGamepadBack();
        }

        private void HandleGamepadBack()
        {
            var optionsView = GetViewOfType<OptionsView>();
            if (optionsView != null && optionsView.canvasGroup.interactable)
            {
                CloseOptionsGamepad();
                return;
            }
        }

        private void OpenOptionsGamepad()
        {
            // Remember which view is currently visible so we can return to it
            m_viewBeforeOptions = null;
            foreach (var v in allViews)
            {
                if (v.canvasGroup.interactable) { m_viewBeforeOptions = v; break; }
            }
            ShowView<OptionsView>();
        }

        private void CloseOptionsGamepad()
        {
            if (m_viewBeforeOptions != null)
            {
                foreach (var v in allViews)
                    HideViewInternal(v);
                ShowViewInternal(m_viewBeforeOptions);
                m_viewBeforeOptions = null;
            }
            else
            {
                // Fallback: show default view
                foreach (var v in allViews)
                    HideViewInternal(v);
                ShowViewInternal(defaultView);
            }
        }

        private View GetViewOfType<T>() where T : View
        {
            foreach (var v in allViews)
                if (v is T) return v;
            return null;
        }

        public void ShowView<T>(bool hideOthers = true) where T : View
        {
            foreach (var view in allViews)
            {
                if (!view)
                    continue;
                if (view.GetType() == typeof(T))
                {
                    ShowViewInternal(view);
                }
                else
                {
                    if(hideOthers)
                        HideViewInternal(view);
                }
            }
        }

        public void HideView<T>() where T : View
        {
            foreach (var view in allViews)
            {
                if(view.GetType() == typeof(T))
                    HideViewInternal(view);
            }
        }

        private void ShowViewInternal(View view)
        {
            view.canvasGroup.alpha = 1;
            view.canvasGroup.interactable = true;
            view.canvasGroup.blocksRaycasts = true;
            view.OnShow();
            view.OnViewShow?.Invoke();

            if (Gamepad.current != null)
                SelectFirstIn(view.gameObject);
        }

        private void HideViewInternal(View view)
        {
            if(!view)
                return;

            // Deselect if the current selection lives inside this view
            var current = EventSystem.current?.currentSelectedGameObject;
            if (current != null && current.transform.IsChildOf(view.transform))
                EventSystem.current.SetSelectedGameObject(null);

            if (view.canvasGroup)
            {
                view.canvasGroup.alpha = 0;
                view.canvasGroup.interactable = false;
                view.canvasGroup.blocksRaycasts = false;
            }

            view.OnHide();
            view.OnViewHide?.Invoke();
        }

        private static void SelectFirstIn(GameObject root)
        {
            var first = root.GetComponentInChildren<Selectable>(false);
            if (first != null)
                EventSystem.current?.SetSelectedGameObject(first.gameObject);
        }

        #region Events

        public void showHostObjects(bool isHost)
        {
            foreach (var obj in objectsForHost)
            {
                if (obj)
                    obj.SetActive(isHost);
            }
        }

        #endregion
    }

    [RequireComponent(typeof(CanvasGroup))]
    public abstract class View : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        public CanvasGroup canvasGroup => _canvasGroup;

        public UnityEvent OnViewShow = new();
        public UnityEvent OnViewHide = new();

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void OnShow() {}
        public virtual void OnHide() {}
    }
}
