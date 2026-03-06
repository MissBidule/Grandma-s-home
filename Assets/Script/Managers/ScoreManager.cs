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
    [SerializeField] private SyncDictionary<PlayerID, ScoreData> scoresSecNotRes = new();
    //[SerializeField] private SyncDictionary<PlayerID, ScoreData> scoresfinal = new();
    [SerializeField] private TMP_Text m_scoreText;
    [SerializeField] private float m_scoreSabotageFinal=0;
    [SerializeField] private float m_scoreSabotage;
    [SerializeField] private int m_scoreBroken;


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
        public float pointSabotage;         // point it s when we sucess in sabotage
        public int pointBroken;   // transformghost it s when we tranform to a object (it dont make sense it is just for teste)

        public override string ToString()
        {
            return $"{pointSabotage}/{pointBroken}";
        }
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPointSabotage(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointSabotage++;
        var ScoreData2 = scoresSecNotRes[playerID];
        ScoreData2.pointSabotage++;
        //scores[playerID] = ScoreData;  ??????????????? qu est ce que j ai fait wtf
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPointBroken(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointBroken++;
        //scores[playerID] = ScoreData;   ??????????????? qu est ce que j ai fait wtf
    }

    [ServerRpc(requireOwnership:false)]
    public void SubPointSabotage(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointSabotage--;
        if (ScoreData.pointSabotage<0)
        {
            ScoreData.pointSabotage=0;
        }

        //scores[playerID] = ScoreData;     ??????????????? qu est ce que j ai fait wtf
    }

    [ServerRpc(requireOwnership:false)]
    public void SubPointBroken(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointBroken--;
        if (ScoreData.pointBroken<0)
        {
            ScoreData.pointBroken=0;
        }
        //scores[playerID] = ScoreData;    ??????????????? qu est ce que j ai fait wtf
    }

    [ServerRpc(requireOwnership:false)]
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

// debuter un chronos qui fait : 
            //foreach (var entry2 in scoresSecNotRes)
           // {
             //   m_scoreSabotage += entry2.Value.pointBroken*0.03f; 
           // }
           //    entry2+=entry1.Value.pointBroken*0.03f
           // la liste 2 fait la list 1 *0.03f tout les x sec 
           // 
// arreter le chronos
    public void SyncPoint()
    {
        m_scoreSabotage=0;
        foreach (var entry in scores)
        {
            m_scoreSabotage += entry.Value.pointBroken;
            foreach (var entry2 in scoresSecNotRes)
            {
                m_scoreSabotage += entry2.Value.pointBroken;//*0.03f;  // c nul ca choque et decu
            }
        }
        m_scoreBroken=0;
        foreach (var entry in scores)
        {
            m_scoreBroken += entry.Value.pointBroken;
        }
    }

// faudra rajouter les differents trucs
    private void RefreshUI()
    {
        if (m_scoreText != null)
        {
            m_scoreText.text = $"Score : {m_scoreSabotage}";
        }

    }

   // if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
     //       {
         //       scoreManager.Fonc();
       //     }

       // rajouter le parametre (RPCInfo info = default) si on a besoins de scoreManager.Fonc(info)
    
}
