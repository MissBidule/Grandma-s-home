using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
 * @brief       Switch player info between ghost and child
 */
public class SwitchPlayer : MonoBehaviour
{
    [SerializeField]
    private Button m_switchBtn;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_switchBtn.onClick.AddListener(OnSwitchBtnClick);
        gameObject.layer = LayerMask.NameToLayer("Child");
    }

    /*
     * @brief OnSwitchBtnClick change the player type between ghost and child
     * @return void
     */
    private void OnSwitchBtnClick()
    {
        if (GetComponent<PlayerBehavior>().m_playerType == PlayerType.Child)
        {
            m_switchBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Child";
            gameObject.layer = LayerMask.NameToLayer("Ghost");
            GetComponent<PlayerBehavior>().m_playerType = PlayerType.Ghost;
            GetComponent<GhostBehavior>().Setup();
        }
        else if (GetComponent<PlayerBehavior>().m_playerType == PlayerType.Ghost)
        {
            m_switchBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Ghost";
            gameObject.layer = LayerMask.NameToLayer("Child");
            GetComponent<PlayerBehavior>().m_playerType = PlayerType.Child;
            GetComponent<ChildBehavior>().Setup();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
