using System;
using System.Linq.Expressions;
using PurrNet;
using TMPro;
using UnityEngine;

/*
 * @brief  Contains class declaration for ScoreManager
 * @details Script that handles the score and call an event when the House is destroy and updates the UI text
 */
public class ScoreManager : NetworkBehaviour
{
    [SerializeField] private SyncDictionary<PlayerID, ScoreData> m_scoresSabotage = new();
    [SerializeField] private SyncDictionary<PlayerID, ScoreData2> m_scoresBroken = new();
    [SerializeField] private SyncVar<float> m_sabotageBonusTotal = new();
    [SerializeField] private TMP_Text m_scoreText;
    [SerializeField] private int m_scoreBroken;
    [SerializeField] private float m_maxScoreSabotage=5.0f;
    [SerializeField] private int m_maxScoreBroken=5;
    private float timer;
    public Action<string> m_noticeHouseDestroy;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

/*
 * @details L'update Refreshes the canvas that displays the score (this needs to be removed and replaced with a view).
*           This makes the server calculate the Sabotage Bonus every second and checks if the total Sabotage points (excluding bonuses) have reached 0. 
*           If so, it resets the Sabotage dictionary.
 */
    private void Update()
    {
        RefreshUI();

        if (!isServer) return;


        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            timer = 0f;
            AddSabotageBonus();
        }

        if (GetFinalScoreSabotage() > m_maxScoreSabotage)
        {
            m_noticeHouseDestroy?.Invoke("sabotage");
        }

        float sabotagePoints = 0;
        foreach (var entry in m_scoresSabotage)
        {
            sabotagePoints += entry.Value.pointSabotage;
        }
        if(sabotagePoints==0)
        {
            ResetScore();
        }
    }

    public struct ScoreData
    {
        public float pointSabotage;      

        public override string ToString()
        {
            return $"{pointSabotage}";
        }
    }

    public struct ScoreData2
    {   
        public int pointBroken;   

        public override string ToString()
        {
            return $"{pointBroken}";
        }
    }

    private void AddSabotageBonus()
    {
        float totalSabotage = 0;

        foreach (var entry in m_scoresSabotage)
        {
            totalSabotage += entry.Value.pointSabotage;
        }

        m_sabotageBonusTotal.value += totalSabotage * 0.3f;  
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPointSabotage(PlayerID playerID) 
    {
        CheckForDictonaryEntrySabotage(playerID);

        var ScoreData = m_scoresSabotage[playerID];
        ScoreData.pointSabotage++;
        m_scoresSabotage[playerID] = ScoreData; 
         
    }

    [ServerRpc(requireOwnership:false)]
    public void SubPointSabotage(PlayerID playerID)
    {
        CheckForDictonaryEntrySabotage(playerID);

        float sabotagePoints = 0;
        foreach (var entry in m_scoresSabotage)
        {
            sabotagePoints += entry.Value.pointSabotage;
        }

        var ScoreData = m_scoresSabotage[playerID];

        if (sabotagePoints > 0)   
        {
            ScoreData.pointSabotage--;
        }

        m_scoresSabotage[playerID] = ScoreData;     
    }

    private float GetFinalScoreSabotage() 
    {
        float sabotagePoints = 0;
        foreach (var entry in m_scoresSabotage)
        {
            sabotagePoints += entry.Value.pointSabotage;
        }
        return sabotagePoints + m_sabotageBonusTotal.value;
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPointBroken(PlayerID playerID)
    {
        CheckForDictonaryEntryBroken(playerID);

        var ScoreData = m_scoresBroken[playerID];
        ScoreData.pointBroken++;
        m_scoresBroken[playerID] = ScoreData;  

        float totalBroken = 0;

        foreach (var entry in m_scoresBroken)
        {
            totalBroken += entry.Value.pointBroken;
        }

        if(totalBroken > m_maxScoreBroken)
        {
            m_noticeHouseDestroy?.Invoke("broken");
        }
    }


    [ServerRpc(requireOwnership:false)]
    public void ResetScore()   
    {
        m_scoresSabotage.Clear();
    }

    private void CheckForDictonaryEntrySabotage(PlayerID playerID)
    {
        if(!m_scoresSabotage.ContainsKey(playerID))
        {
            m_scoresSabotage.Add(playerID, new ScoreData());
        }
    } 

    private void CheckForDictonaryEntryBroken(PlayerID playerID)
    {
        if(!m_scoresBroken.ContainsKey(playerID)) 
        {
            m_scoresBroken.Add(playerID, new ScoreData2());
        }
        
    } 

    private void RefreshUI() // GetFinalScore() et totalBroken a utiliser sur une view plutot qu un canvas!
    {
        float totalBroken = 0;

        foreach (var entry in m_scoresBroken)
        {
            totalBroken += entry.Value.pointBroken;
        }
        if (m_scoreText != null)
        {
            m_scoreText.text = $"Score Sabotage : {GetFinalScoreSabotage():0.00}";
            //m_scoreText.text = $"Score : {totalBroken}";
        }

    }

}
