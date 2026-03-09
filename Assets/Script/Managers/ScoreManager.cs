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
    private float m_timer;
    public Action<string> m_noticeHouseDestroy;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

/*
 * @details Refreshes the canvas that displays the score (this needs to be removed and replaced with a view).
 *          This makes the server call the Sabotage Bonus every second and checks if the total Sabotage points (excluding bonuses) have reached 0. 
 *          If so, it resets the Sabotage dictionary.
 *          Invoke a event if the final Score Sabotage > the max
 * @return void
*/
    private void Update()
    {
        RefreshUI();

        if (!isServer) return;


        m_timer += Time.deltaTime;

        if (m_timer >= 1f)
        {
            m_timer = 0f;
            SabotageBonus();
        }

        if (GetFinalScoreSabotage() > m_maxScoreSabotage)
        {
            m_noticeHouseDestroy?.Invoke("sabotage");
        }

        float sabotagePoints = 0; // peut etre a mettre avant la verification du serveur?
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

/*
 * @details This function calculates the bonus points for sabotage (the bonus is 0.3).
 * @return void
*/

    private void SabotageBonus()
    {
        float totalSabotage = 0;

        foreach (var entry in m_scoresSabotage)
        {
            totalSabotage += entry.Value.pointSabotage;
        }

        m_sabotageBonusTotal.value += totalSabotage * 0.3f;  
    }

/*
 * @details This function adds +1 to the Sabotage score
* @param _playerID: Id of the player.
 * @return void
*/
    [ServerRpc(requireOwnership:false)]
    public void AddPointSabotage(PlayerID _playerID) 
    {
        CheckForDictonaryEntrySabotage(_playerID);

        var ScoreData = m_scoresSabotage[_playerID];
        ScoreData.pointSabotage++; 
        m_scoresSabotage[_playerID] = ScoreData; 
         
    }

/*
 * @details This function deducts 1 from the Sabotage score if it is not equal to 0
 * @param _playerID: Id of the player.
 * @return void
*/
    [ServerRpc(requireOwnership:false)]
    public void SubPointSabotage(PlayerID _playerID)
    {
        CheckForDictonaryEntrySabotage(_playerID);

        float sabotagePoints = 0;
        foreach (var entry in m_scoresSabotage)
        {
            sabotagePoints += entry.Value.pointSabotage;
        }

        var ScoreData = m_scoresSabotage[_playerID];

        if (sabotagePoints > 0)   
        {
            ScoreData.pointSabotage--;
        }

        m_scoresSabotage[_playerID] = ScoreData;     
    }

/*
 * @details This function calcule and return the Final sabotage score
 * @return float : The Final Sabotage Score : score of sabotage + bonus sabotage
*/
    private float GetFinalScoreSabotage() 
    {
        float sabotagePoints = 0;
        foreach (var entry in m_scoresSabotage)
        {
            sabotagePoints += entry.Value.pointSabotage;
        }
        return sabotagePoints + m_sabotageBonusTotal.value;
    }

/*
 * @details This function add +1 to the broken score 
 *          Invoke a event if totalBroken > max broken
 * @param _playerID: Id of the player.
 * @return void
*/
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

/*
 * @details This function reset the Sabotage score
 * @return void
*/
    [ServerRpc(requireOwnership:false)]
    public void ResetScore()   
    {
        m_scoresSabotage.Clear();
    }

/*
 * @details This function check who change the Sabotage score
 * @param _playerID: Id of the player.
 * @return void
*/
    private void CheckForDictonaryEntrySabotage(PlayerID _playerID)
    {
        if(!m_scoresSabotage.ContainsKey(_playerID))
        {
            m_scoresSabotage.Add(_playerID, new ScoreData());
        }
    } 

/*
 * @details This function check who change the Broken score
 * @param _playerID: Id of the player.
 * @return void
*/
    private void CheckForDictonaryEntryBroken(PlayerID playerID)
    {
        if(!m_scoresBroken.ContainsKey(playerID)) 
        {
            m_scoresBroken.Add(playerID, new ScoreData2());
        } 
    } 
/*
 * @details This function refresh the Score UI
 * @return void
*/
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
