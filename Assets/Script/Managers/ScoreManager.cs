using System;
using System.Linq.Expressions;
using PurrNet;
using TMPro;
using UnityEngine;

/*
 * @brief  Contains class declaration for ScoreManager
 * @details Script that handles the score and updates theUI text
 */
public class ScoreManager : NetworkBehaviour
{
    [SerializeField] private SyncDictionary<PlayerID, ScoreData> scores = new();
    [SerializeField] private SyncVar<float> sabotageBonusTotal = new();
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

        foreach (var entry in scores)
        {
            totalSabotage += entry.Value.pointSabotage;
        }

        sabotageBonusTotal.value += totalSabotage * 0.3f;  
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPointSabotage(PlayerID playerID) 
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointSabotage++;
        scores[playerID] = ScoreData; 
         
    }

    [ServerRpc(requireOwnership:false)]
    public void SubPointSabotage(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];

        if (ScoreData.pointSabotage > 0)   // a voir
        {
            ScoreData.pointSabotage--;
        }

        scores[playerID] = ScoreData;     
    }

    private float GetFinalScore() 
    {
        float sabotagePoints = 0;
        foreach (var entry in scores)
        {
            Debug.Log("Player sabotage: " + entry.Value.pointSabotage);
            sabotagePoints += entry.Value.pointSabotage;
        }
        Debug.Log("SabotageSum = " + sabotagePoints);
        Debug.Log("Bonus = " + sabotageBonusTotal.value);
        return sabotagePoints + sabotageBonusTotal.value;
    }

    //
    [ServerRpc(requireOwnership:false)]
    public void AddPointBroken(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointBroken++;
        scores[playerID] = ScoreData;  

        if(ScoreData.pointBroken > m_maxScoreBroken)
        {
            m_noticeHouseDestroy?.Invoke("broken");
        }
    }


    [ServerRpc(requireOwnership:false)]
    public void ResetScore()   //on ne devrait pas avoir besoins de ca
    {
        scores.Clear();
    }


    private void CheckForDictonaryEntry(PlayerID playerID)
    {
        if(!scores.ContainsKey(playerID))
        {
            scores.Add(playerID, new ScoreData());
        }
        
    } 
    private void RefreshUI() // a retirer
    {
        if (m_scoreText != null)
        {
            m_scoreText.text = $"Score : {GetFinalScore():0.00}";
        }

    }

   // if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
     //       {
         //       scoreManager.Fonc();
       //     }

       // rajouter le parametre (RPCInfo info = default) si on a besoins de scoreManager.Fonc(info)
    
}
