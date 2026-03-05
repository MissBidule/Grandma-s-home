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
    [SerializeField] private int m_score=0;


    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void Update()
    {
        SyncPoint();
      //  RefreshUI();
    }
    //
    //[ObserversRpc(runLocally:true)]
    [ServerRpc(requireOwnership:false)]
    public void AddPoint(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.point++;  // ça add par personne donc ça fait genre 
                                // G1 sabote 2 fois donc Point de G1 et G2 = 2 (fais par G1)
                                // mais si G1 sabote 1 fois et G2 sabote 1 fois donc Point = 1 (fais par G1) / 1 (fais par G2)
        scores[playerID] = ScoreData; // ça se fait que sur le server?

        
        //RefreshUI();
    }

    public void ResetPoint(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.point=0;
        scores[playerID] = ScoreData;

        m_score = ScoreData.point; // faire plutot la sum depuis la liste
       // RefreshUI();
    }

    public void SyncPoint()
    {

        foreach (var entry in scores)
        {
            m_score += entry.Value.point;
        }
        RefreshUI();
        //foreach (var key in scores.Keys){
          // m_score=scores[key]; 
        //}
        //for(PlayerID i=0;i<0;i++){
        //m_score += scores[i];
        //}
        //var ScoreData = scores[playerID];
        //ScoreData.point++;  
        //foreach( PlayerID playerID in scores){
        //int sum = scores[playerID].point;
        //}
        
    }

    public void ResetTransformghost(PlayerID playerID)
    {
        CheckForDictonaryEntry(playerID);

        var ScoreData = scores[playerID];
        ScoreData.tranformghost=0;
        scores[playerID] = ScoreData;
        //RefreshUI();
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
