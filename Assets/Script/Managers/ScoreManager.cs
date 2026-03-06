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
    [SerializeField] private SyncDictionary<PlayerID, ScoreData> scoresSec = new();
    //[SerializeField] private SyncDictionary<PlayerID, ScoreData> scoresfinal = new();
    [SerializeField] private TMP_Text m_scoreText;
    [SerializeField] private float m_scoreSabotageFinal=0;
    [SerializeField] private float m_scoreSabotage;
    [SerializeField] private float m_scoreSabotageUnite;
    [SerializeField] private float m_scoreSabotageSecond;
    [SerializeField] private int m_scoreBroken;
    [SerializeField] private bool m_stopAddSec=false;


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
        var ScoreData2 = scores[playerID];
        ScoreData.pointSabotage++;
        //foreach (var entry in scores){
          //  m_scoreSabotageUnite += entry.Value.pointSabotage;
           // m_scoreSabotageSecond = entry.Value.pointSabotage *0.1f;
        //}
        //scores[playerID] = ScoreData;  ??????????????? qu est ce que j ai fait wtf
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPointBroken(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointBroken++;
        //scores[playerID] = ScoreData;   ??????????????? qu est ce que j ai fait wtf
        //AddPointSabotageParSeconde(playerID);
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

        var ScoreData2 = scores[playerID];
        ScoreData2.pointSabotage--;
        if (ScoreData2.pointSabotage<0)
        {
            ScoreData2.pointSabotage=0;
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

    //public void AddPointSabotageParSeconde(PlayerID playerID)
    //{
      //  var ScoreData = scores[playerID];
        //ScoreData.pointSabotage+=0.1f;
        //scores[playerID] = ScoreData;
        //if(m_stopAddSec==true)
          //  return;
    //}

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


    public void SyncPoint()
    {
        m_scoreSabotage=0;
        foreach (var entry in scores)
        {
            m_scoreSabotage = m_scoreSabotageUnite + m_scoreSabotageSecond;
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
