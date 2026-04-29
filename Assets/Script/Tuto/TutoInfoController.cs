using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutoInfoController : MonoBehaviour
{
    [Header("Pages assign in order")]
    [SerializeField] private GameObject[] m_pages;

    [Header("Navigation buttons")]
    [SerializeField] private Button m_nextButton;
    [SerializeField] private Button m_prevButton;
    [SerializeField] private Button m_confirmButton;

    private PlayerInput m_playerInput;
    private int m_currentPage;
    private Action m_onComplete;
    private CursorLockMode m_previousLockMode;
    private bool m_previousCursorVisible;

    public void Show(PlayerInput _playerInput, Action _onComplete)
    {
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

        gameObject.SetActive(true);
        RefreshPage();
        SelectCurrentButton();
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

    public void Complete()
    {
        Cursor.lockState = m_previousLockMode;
        Cursor.visible = m_previousCursorVisible;

        m_playerInput?.ActivateInput();

        gameObject.SetActive(false);
        m_onComplete?.Invoke();
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
        for (int i = 0; i < m_pages.Length; i++)
            if (m_pages[i] != null)
                m_pages[i].SetActive(i == m_currentPage);

        bool isFirst = m_currentPage == 0;
        bool isLast = m_currentPage == m_pages.Length - 1;

        m_prevButton.gameObject.SetActive(!isFirst);

        if (m_confirmButton != null)
        {
            m_nextButton.gameObject.SetActive(!isLast);
            m_confirmButton.gameObject.SetActive(isLast);
        }
        else
        {
            m_nextButton.gameObject.SetActive(true);
        }
    }
}
