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
    [SerializeField] private TMP_Text m_scoreText;
    [SerializeField] private int m_score;


    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void Update()
    {
        SyncPoint();
        RefreshUI();
    }


    public struct ScoreData
    {
        // point it s when we sucess in sabotage
        // transformghost it s when we tranform to a object (it dont make sense it is just for teste)
        public int point;
        public int tranformghost; 

        public override string ToString()
        {
            return $"{point}/{tranformghost}";
        }
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPoint(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.point++;
        scores[playerID] = ScoreData; 
    }

    [ServerRpc(requireOwnership:false)]
    public void AddTransformghost(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.tranformghost++;
        scores[playerID] = ScoreData;
    }


// ca marche surement pas

    public void ResetScore()
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


    public void SyncPoint()
    {
        m_score=0;
        foreach (var entry in scores)
        {
            m_score += entry.Value.point;
        }
    }
    private void RefreshUI()
    {
        if (m_scoreText != null)
        {
            m_scoreText.text = $"Score : {m_score}";
        }
    }
    
}
