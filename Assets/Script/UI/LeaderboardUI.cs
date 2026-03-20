using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using PurrLobby;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using static PurrLobby.RoleKeeper;

public class LeaderboardUI : GameView
{
    [SerializeField] private GameObject m_playerPrefab;
    [SerializeField] private Transform m_ghostCategory;
    [SerializeField] private Transform m_childCategory;
    private List<GameObject> m_players = new List<GameObject>();
    private List<string> m_playersID = new List<string>();
    private RoleKeeper m_roleKeeper;

    void Start()
    {
        m_roleKeeper = FindAnyObjectByType<RoleKeeper>();
        InitLeaderboard();
    }

    public void InitLeaderboard()
    {
        List<Role> players = new List<Role>(m_roleKeeper.GetPlayersAllInfo());
        foreach (Role p in players)
        {
            var newPlayerTab = Instantiate(m_playerPrefab, p.m_isGhost ? m_ghostCategory : m_childCategory);
            newPlayerTab.GetComponentInChildren<TextMeshProUGUI>().text = p.m_username;
            //newPlayerTab.GetComponent<UnityEngine.UI.Image>().sprite = p.m_avatar;
            m_players.Add(newPlayerTab);
            m_playersID.Add(p.m_roleId);
        }
    }

    public void UpdateDisconnected()
    {
        List<string> disconnected = m_roleKeeper.GetDisconnectedPlayers();
        for (int i = 0; i < m_playersID.Count; i++)
        {
            if (disconnected.Exists(x => x.Equals(m_playersID[i])))
            {
                m_players[i].GetComponentInChildren<TextMeshProUGUI>().color = Color.grey;
            }
        }
    }
}
