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

//
    //[ObserversRpc(runLocally:true)]
    public void AddPoint(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.point++;
        scores[playerID] = ScoreData;

        m_score = ScoreData.point;
        RefreshUI();
    }

    public void AddTransformghost(PlayerID playerID)
    {
        Debug.Log("line 3");
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.tranformghost++;
        scores[playerID] = ScoreData;
        Debug.Log("line 6");
        //RefreshUI();
    }

    //

    private void CheckForDictonaryEntry(PlayerID playerID)
    {
        Debug.Log("line4");
        if(!scores.ContainsKey(playerID))
        {
            scores.Add(playerID, new ScoreData());
            Debug.Log("line 5");
        }
        
    }

    public struct ScoreData
    {
        // point it s when we sucess in sabotage
        // transformghost it s when we tranform to a object (it dont make sense it is just for teste)
        public int point; // kills
        public int tranformghost; //deaths

        public override string ToString()
        {
            return $"{point}/{tranformghost}";
        }
    }

    private void RefreshUI()
    {
        if (m_scoreText != null)
        {
            m_scoreText.text = $"Score : {m_score}";
        }
    }


    //public static ScoreManager m_Instance;

    

    //[SerializeField] private SyncVar<int> m_score=new(0);

    //public int m_Score => m_score;




    /*
    @brief      Initialise the singleton and refresh the ui
    @return     void
    */
    //private void Awake()
    //{
        //if (m_Instance != null && m_Instance != this)
        //{
            //Destroy(gameObject);
          //  return;
        //}

        //m_Instance = this;
        //RefreshUI();
    //}

    /**
    @brief      Adds points to the score
    @param      _value: value to add
    @return     void
    */
    //[ServerRpc(requireOwnership:false)]
    //public void Add(int _value)
    //{
        //m_score.value += _value;
        //Debug.Log("valeur ajoute a m_score");
      //  RefreshUI();
    //}

    /**
    @brief      resets score
    @return     void
    */
    //public void ResetScore()
    //{
      //  m_score = 0;
        //RefreshUI();
    //}

    /**
    @brief      updates the UI
    @return     void
    */
    
}
