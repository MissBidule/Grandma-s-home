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
    //[SerializeField] private SyncDictionary<PlayerID, ScoreData> scoresSecNotRes = new(); // il sert a rien pour l'instant
    //[SerializeField] private SyncDictionary<PlayerID, ScoreData> scoresfinal = new();
    [SerializeField] private TMP_Text m_scoreText;
    //[SerializeField] private float m_scoreSabotageFinal=0;
    [SerializeField] private float m_scoreSabotage;
    [SerializeField] private int m_scoreBroken;
    [SerializeField] private SyncVar<float> sabotageBonusTotal = new(); // point timer
    private float timer;


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
        //SyncPoint();
        
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

    private void AddSabotageBonus()
    {
        float totalSabotage = 0;

        foreach (var entry in scores)
        {
            totalSabotage += entry.Value.pointSabotage;
        }

        sabotageBonusTotal.value += totalSabotage * 0.3f; // ca marche sur tous 
    }
    private float GetFinalScore() //c le return qui est important
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

    [ServerRpc(requireOwnership:false)]
    public void AddPointSabotage(PlayerID playerID) // ca marche sur tous
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointSabotage++;
        scores[playerID] = ScoreData; 

       // var ScoreData2 = scoresSecNotRes[playerID];
        //ScoreData2.pointSabotage++;
        //scoresSecNotRes[playerID] = ScoreData2;
         
    }

    [ServerRpc(requireOwnership:false)]
    public void AddPointBroken(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.pointBroken++;
        scores[playerID] = ScoreData;  
    }

    [ServerRpc(requireOwnership:false)]
    public void SubPointSabotage(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        //ScoreData.pointSabotage--;
        //if (ScoreData.pointSabotage<0)
        //{
        //  ScoreData.pointSabotage=0;
        //}

        if (ScoreData.pointSabotage > 0)   // a voir
        {
            ScoreData.pointSabotage--;
        }

        scores[playerID] = ScoreData;     
    }

    [ServerRpc(requireOwnership:false)]
    public void SubPointBroken(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        //ScoreData.pointBroken--;
        if (ScoreData.pointBroken > 0)
        {
            ScoreData.pointBroken--; // a voir
        }
        //if (ScoreData.pointBroken<0)
        //{
          //  ScoreData.pointBroken=0;
        //}
        scores[playerID] = ScoreData;    
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
    //public void SyncPoint() //telemment merdique comme fonction
    //{
        //m_scoreSabotage=0;
        //foreach (var entry in scores)
        //{
          //  m_scoreSabotage += entry.Value.pointSabotage;
            //foreach (var entry2 in scoresSecNotRes)
            //{
              //  m_scoreSabotage += entry2.Value.pointSabotage;//*0.03f;  // c nul ca choque et decu
            //}
        //}
        //m_scoreBroken=0;
        //foreach (var entry in scores)
       // {
      //      m_scoreBroken += entry.Value.pointBroken;
     //   }
   // }

// faudra rajouter les differents trucs
    private void RefreshUI()
    {
        if (m_scoreText != null)
        {
           // m_scoreText.text = $"Score : {m_scoreSabotage}";
            m_scoreText.text = $"Score : {GetFinalScore():0.00}";
        }

    }

   // if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
     //       {
         //       scoreManager.Fonc();
       //     }

       // rajouter le parametre (RPCInfo info = default) si on a besoins de scoreManager.Fonc(info)
    
}
