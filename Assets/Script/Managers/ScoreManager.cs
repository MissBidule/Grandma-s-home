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
    [SerializeField] private SyncDictionary<PlayerID, ScoreData> m_scores = new();
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

        if (GetFinalScore() > m_maxScoreSabotage)
        {
            m_noticeHouseDestroy?.Invoke("sabotage");
        }

        float sabotagePoints = 0;
        foreach (var entry in m_scores)
        {
            Debug.Log("Player sabotage: " + entry.Value.pointSabotage);
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
        public int pointBroken;   

        public override string ToString()
        {
            return $"{pointSabotage}/{pointBroken}";
        }
    }

    private void AddSabotageBonus()
    {
        float totalSabotage = 0;

        foreach (var entry in m_scores)
        {
            totalSabotage += entry.Value.pointSabotage;
        }

        m_sabotageBonusTotal.value += totalSabotage * 0.3f;  
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPointSabotage(PlayerID playerID) 
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = m_scores[playerID];
        ScoreData.pointSabotage++;
        m_scores[playerID] = ScoreData; 
         
    }

    [ServerRpc(requireOwnership:false)]
    public void SubPointSabotage(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);


        float sabotagePoints = 0;
        foreach (var entry in m_scores)
        {
            Debug.Log("Player sabotage: " + entry.Value.pointSabotage);
            sabotagePoints += entry.Value.pointSabotage;
        }

        var ScoreData = m_scores[playerID];

        if (sabotagePoints > 0)   
        {
            ScoreData.pointSabotage--;
        }

        m_scores[playerID] = ScoreData;     

    }

    private float GetFinalScore() 
    {
        float sabotagePoints = 0;
        foreach (var entry in m_scores)
        {
            Debug.Log("Player sabotage: " + entry.Value.pointSabotage);
            sabotagePoints += entry.Value.pointSabotage;
        }
        Debug.Log("SabotageSum = " + sabotagePoints);
        Debug.Log("Bonus = " + m_sabotageBonusTotal.value);
        return sabotagePoints + m_sabotageBonusTotal.value;
    }

    //
    [ServerRpc(requireOwnership:false)]
    public void AddPointBroken(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = m_scores[playerID];
        ScoreData.pointBroken++;
        m_scores[playerID] = ScoreData;  

        if(ScoreData.pointBroken > m_maxScoreBroken)
        {
            m_noticeHouseDestroy?.Invoke("broken");
        }
    }


    [ServerRpc(requireOwnership:false)]
    public void ResetScore()   
    {
        m_scores.Clear();
    }


    private void CheckForDictonaryEntry(PlayerID playerID)
    {
        if(!m_scores.ContainsKey(playerID))
        {
            m_scores.Add(playerID, new ScoreData());
        }
        
    } 
    private void RefreshUI() // GetFinalScore() a utiliser sur une view plutot qu un canvas!
    {
        if (m_scoreText != null)
        {
            m_scoreText.text = $"Score : {GetFinalScore():0.00}";
        }

    }

    // (RPCInfo info = default)
    // if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
    //       {
        //       scoreManager.Fonc(info.sender);
    //     }

}
