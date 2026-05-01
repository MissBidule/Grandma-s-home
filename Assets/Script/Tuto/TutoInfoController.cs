using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutoInfoController : MonoBehaviour
{
    public enum Category { Ghost, Child, End, All }

    [Header("Pages assign in order")]
    [SerializeField] private GameObject[] m_ghostPages;
    [SerializeField] private GameObject[] m_childPages;
    [SerializeField] private GameObject[] m_endPages;
    [SerializeField] private GameObject[] m_allPages;

    [Header("Navigation buttons")]
    [SerializeField] private Button m_nextButton;
    [SerializeField] private Button m_prevButton;
    [SerializeField] private Button m_confirmButton;

    private GameObject[] m_pages;
    private Category m_currentCategory;
    private PlayerInput m_playerInput;
    private int m_currentPage;
    private Action m_onComplete;
    private CursorLockMode m_previousLockMode;
    private bool m_previousCursorVisible;
    private CanvasGroup m_canvasGroup;

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void SetVisible(bool _visible)
    {
        if (m_canvasGroup == null) return;
        m_canvasGroup.alpha          = _visible ? 1f : 0f;
        m_canvasGroup.interactable   = _visible;
        m_canvasGroup.blocksRaycasts = _visible;
    }

    public void Show(Category _category, PlayerInput _playerInput, Action _onComplete)
    {
        m_currentCategory = _category;
        m_pages = _category switch
        {
            Category.Ghost => m_ghostPages,
            Category.Child => m_childPages,
            Category.End => m_endPages,
            Category.All => m_allPages
        };

        if (m_pages == null || m_pages.Length == 0)
        {
            _onComplete?.Invoke();
            return;
        }

        m_playerInput = _playerInput;
        m_onComplete = _onComplete;
        m_currentPage = 0;

        m_previousLockMode = Cursor.lockState;
        m_previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        m_playerInput?.DeactivateInput();

        SetVisible(true);
        RefreshPage();
        SelectCurrentButton();
    }

    private void LateUpdate()
    {
        if (m_canvasGroup == null || m_canvasGroup.alpha <= 0f) return;
        if (PauseMenuView.IsPaused) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnNext()
    {
        if (m_currentPage >= m_pages.Length - 1) { Complete(); return; }
        m_currentPage++;
        RefreshPage();
        SelectCurrentButton();
    }

    public void OnPrev()
    {
        if (m_currentPage <= 0) return;
        m_currentPage--;
        RefreshPage();
        SelectCurrentButton();
    }

    public bool IsVisible => m_canvasGroup != null && m_canvasGroup.alpha > 0f;

    public void Complete()
    {
        Cursor.lockState = m_previousLockMode;
        Cursor.visible = m_previousCursorVisible;

        m_playerInput?.ActivateInput();

        SetVisible(false);
        m_onComplete?.Invoke();
    }

    public void Close()
    {
        Cursor.lockState = m_previousLockMode;
        Cursor.visible = m_previousCursorVisible;
        m_playerInput?.ActivateInput();
        SetVisible(false);
    }

    private void SelectCurrentButton()
    {
        bool isLast = m_currentPage == m_pages.Length - 1;
        GameObject toSelect = (isLast && m_confirmButton != null)
            ? m_confirmButton.gameObject
            : m_nextButton.gameObject;
        EventSystem.current?.SetSelectedGameObject(toSelect);
    }

    private void RefreshPage()
    {
        foreach (var p in m_ghostPages) if (p) p.SetActive(false);
        foreach (var p in m_childPages) if (p) p.SetActive(false);
        foreach (var p in m_endPages)   if (p) p.SetActive(false);
        foreach (var p in m_allPages)   if (p) p.SetActive(false);

        for (int i = 0; i < m_pages.Length; i++)
            if (m_pages[i] != null)
                m_pages[i].SetActive(i == m_currentPage);

        bool isFirst = m_currentPage == 0;
        bool isLast = m_currentPage == m_pages.Length - 1;

        m_prevButton.gameObject.SetActive(!isFirst);

        if (m_confirmButton != null)
        {
            bool showConfirm = isLast || m_currentCategory == Category.All;
            m_nextButton.gameObject.SetActive(!isLast);
            m_confirmButton.gameObject.SetActive(showConfirm);
        }
        else
        {
            m_nextButton.gameObject.SetActive(true);
        }
    }
}
