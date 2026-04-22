using System.Collections.Generic;
using PurrLobby;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using static PurrLobby.RoleKeeper;

public class LeaderboardUI : GameView
{
    [SerializeField] private GameObject m_leaderboardCanvasPrefab;
    [SerializeField] private GameObject m_playerPrefab;
    [SerializeField] private Transform m_ghostCategory;
    [SerializeField] private Transform m_childCategory;

    private GameObject m_canvasInstance;
    private readonly List<GameObject> m_players = new();
    private readonly List<string> m_playersID = new();
    private RoleKeeper m_roleKeeper;

    void Start()
    {
        m_roleKeeper = FindAnyObjectByType<RoleKeeper>();
        if (m_leaderboardCanvasPrefab != null) BuildFromJohnPrefab();
        InitLeaderboard();
    }

    public void setLeaderboardCamera(Camera _camera)
    {
        Canvas leaderboardCanvas = m_canvasInstance.GetComponent<Canvas>();
        leaderboardCanvas.worldCamera = _camera;
        leaderboardCanvas.planeDistance = 0.58f;
    }

    private void BuildFromJohnPrefab()
    {
        m_canvasInstance = Instantiate(m_leaderboardCanvasPrefab, transform);
        m_canvasInstance.name = "Canvas_Leaderboard";
        if (m_canvasInstance.TryGetComponent<Canvas>(out var c)) DestroyImmediate(c);
        if (m_canvasInstance.TryGetComponent<CanvasScaler>(out var cs)) DestroyImmediate(cs);
        if (m_canvasInstance.TryGetComponent<GraphicRaycaster>(out var gr)) DestroyImmediate(gr);
        var rt = m_canvasInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        var bg = m_canvasInstance.transform.Find("Main_Scoreboard_Bg");
        if (bg == null) return;
        var teams = bg.Find("Teams_Container");
        if (teams == null || teams.childCount < 2) return;

        var team0 = teams.GetChild(0);
        var team1 = teams.GetChild(1);

        m_ghostCategory = team0.Find("Player_List_Bg");
        m_childCategory = team1.Find("Player_List_Bg");

        SetHeader(team0, "GHOST");
        SetHeader(team1, "CHILD");

        ClearChildren(m_ghostCategory);
        ClearChildren(m_childCategory);
    }

    private static void SetHeader(Transform team, string text)
    {
        var tmp = team.Find("Header_Area/Text (TMP)")?.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = text;
    }

    private static void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }

    public void InitLeaderboard()
    {
        if (m_roleKeeper == null || m_playerPrefab == null) return;
        int ghostIdx = 0, childIdx = 0;
        foreach (Role p in m_roleKeeper.GetPlayersAllInfo())
        {
            var parent = p.m_isGhost ? m_ghostCategory : m_childCategory;
            if (parent == null) continue;
            var row = Instantiate(m_playerPrefab, parent);
            var rank = row.transform.Find("Rank")?.GetComponent<TMP_Text>();
            var name = row.transform.Find("Player Name")?.GetComponent<TMP_Text>();
            var ping = row.transform.Find("Ping")?.GetComponent<TMP_Text>();
            int rankNum = p.m_isGhost ? ++ghostIdx : ++childIdx;
            if (rank != null) rank.text = rankNum.ToString();
            if (name != null) name.text = p.m_username;
            if (ping != null) ping.text = "-";
            m_players.Add(row);
            m_playersID.Add(p.m_roleId);
        }
    }

    public void UpdateDisconnected()
    {
        var disconnected = m_roleKeeper.GetDisconnectedPlayers();
        for (int i = 0; i < m_playersID.Count; i++)
        {
            if (disconnected.Exists(x => x.Equals(m_playersID[i])))
                foreach (var tmp in m_players[i].GetComponentsInChildren<TMP_Text>(true))
                    tmp.color = Color.grey;
        }
    }
}
