using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * @brief  Handles the tutorial UI: instruction text and fade-to-black overlay.
 */
public class TutoUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text m_instructionText;
    [SerializeField] private Image m_fadeOverlay;
    [SerializeField] private float m_fadeDuration = 0.5f;

    private void Awake()
    {
        if (m_fadeOverlay != null)
            SetFadeAlpha(0f);
    }

    public void ShowText(string _text)
    {
        if (m_instructionText == null) return;
        m_instructionText.gameObject.SetActive(true);
        m_instructionText.text = _text;
    }

    public void HideText()
    {
        if (m_instructionText != null)
            m_instructionText.gameObject.SetActive(false);
    }

    public void FadeAndSwitch(Action _onBlack, Action _onDone = null)
    {
        StartCoroutine(FadeRoutine(_onBlack, _onDone));
    }

    private IEnumerator FadeRoutine(Action _onBlack, Action _onDone)
    {
        yield return StartCoroutine(Fade(0f, 1f));
        _onBlack?.Invoke();
        yield return StartCoroutine(Fade(1f, 0f));
        _onDone?.Invoke();
    }

    private IEnumerator Fade(float _from, float _to)
    {
        float elapsed = 0f;
        while (elapsed < m_fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(_from, _to, elapsed / m_fadeDuration));
            yield return null;
        }
        SetFadeAlpha(_to);
    }

    private void SetFadeAlpha(float _alpha)
    {
        if (m_fadeOverlay == null) return;
        Color c = m_fadeOverlay.color;
        c.a = _alpha;
        m_fadeOverlay.color = c;
    }
}
