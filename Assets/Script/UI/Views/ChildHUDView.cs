using System.Collections;
using PurrNet;
using Script.UI.Views;
using TMPro;
using UI;
using UnityEngine;

public class ChildHUDView : GameView
{
    [Header("HUD Parameters")]
    [SerializeField] private TMP_Text m_hudMessage;
    [SerializeField] private GameObject m_hudMessagePanel;
    
    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    protected void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ChildHUDView>();
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
}
