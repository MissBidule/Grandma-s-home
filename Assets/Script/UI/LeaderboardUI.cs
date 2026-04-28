using System.Collections.Generic;
using PurrLobby;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using static PurrLobby.RoleKeeper;

/*
 * @brief       Contains class declaration for LeaderboardUI
 * @details     GameView that builds the in-game scoreboard from John's prefab and suppresses the X-ray outline while shown.
 */
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
    private OutlineSuppressor.Handle m_outlineHandle;

    /*
     * @brief Suppresses the sabotable-objects outline while the leaderboard is shown
     * @return void
    */
    public override void OnShow()
    {
        base.OnShow();
        m_outlineHandle = OutlineSuppressor.Acquire(m_outlineHandle);
    }

    /*
     * @brief Restores the sabotable-objects outline when the leaderboard is hidden
     * @return void
    */
    public override void OnHide()
    {
        base.OnHide();
        OutlineSuppressor.Release(ref m_outlineHandle);
    }

    /*
     * @brief  Safety net that releases the outline handle if the GameObject is disabled while still showing
     * @return void
    */
    private void OnDisable()
    {
        OutlineSuppressor.Release(ref m_outlineHandle);
    }

    void Start()
    {
        m_roleKeeper = FindAnyObjectByType<RoleKeeper>();
        if (m_leaderboardCanvasPrefab != null) BuildFromJohnPrefab();
        InitLeaderboard();
    }

    private void BuildFromJohnPrefab()
    {
        m_canvasInstance = Instantiate(m_leaderboardCanvasPrefab, transform);
        m_canvasInstance.name = "Canvas_Leaderboard";
        if (m_canvasInstance.TryGetComponent<GraphicRaycaster>(out var gr)) 
        if (m_canvasInstance.TryGetComponent<Canvas>(out var c)) DestroyImmediate(c);
        if (m_canvasInstance.TryGetComponent<CanvasScaler>(out var cs)) DestroyImmediate(cs);DestroyImmediate(gr);
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
        if (bg != null)
        {
            RectTransform bgRT = bg.GetComponent<RectTransform>();
            // Force le fond à s'étirer sur tout le parent (le LeaderboardUI)
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = new Vector2(50, 50); // Marge interne de 50 pixels
            bgRT.offsetMax = new Vector2(-50, -50);
        }

        var teams = bg.Find("Teams_Container");
        if (teams == null || teams.childCount < 2) return;

        var team0 = teams.GetChild(0);
        var team1 = teams.GetChild(1);

        m_childCategory = team0.Find("Player_List_Bg");
        m_ghostCategory = team1.Find("Player_List_Bg");

        SetHeader(team0, "CHILDS");
        SetHeader(team1, "GHOSTS");

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
