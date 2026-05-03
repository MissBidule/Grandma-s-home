using System;
using System.Linq.Expressions;
using PurrNet;
using Script.UI.Views;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/*
 * @brief  Contains class declaration for ScoreManager
 * @details Script that handles the score and call an event when the House is destroy and updates the UI text
*/
public class ScoreManager : NetworkBehaviour
{
    private float m_maxScoreSabotage=1.0f;
    private SyncVar<float> m_currentScoreSabotage = new();
    private int m_maxScoreBroken = 750;
    private SyncVar<int> m_currentScoreBroken = new();
    private int m_nbSabotaged = 0;
    
    private float m_timer;
    
    public Action<bool> m_noticeHouseDestroyed; // false if only sabotaged
    private bool m_sabotagedCalled = false;

    /* VALUES ! ALL IN PERCENT % ! */
    [SerializeField] private float m_repairValue = 0.2f;
    [SerializeField] private float m_sabotageValue = 0.1f;
    // This number is multiplied by the number of sabotage.
    // It is applied every second.
    [SerializeField] private float m_sabotageBonus = 0.01f;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        InstanceHandler.UnregisterInstance<ScoreManager>();
    }

    /*
     * @details Refreshes the canvas that displays the score (this needs to be removed and replaced with a view).
     *          This makes the server call the Sabotage Bonus every second and checks if the total Sabotage points (excluding bonuses) have reached 0. 
     *          If so, it resets the Sabotage dictionary.
     *          Invokes an event if the final Score Sabotage > the max
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
            ApplySabotageBonus();
        }

        if (m_currentScoreSabotage > m_maxScoreSabotage && !m_sabotagedCalled)
        {
            m_noticeHouseDestroyed?.Invoke(false);
            m_sabotagedCalled = true;
        }
    }

    /*
     * @details This function calculates the bonus points for sabotage (the bonus is 0.3).
     * @return void
     */
    private void ApplySabotageBonus()
    {
        m_currentScoreSabotage.value += m_sabotageBonus * m_nbSabotaged;  
    }

    /*
     * @details This function adds +1 to the Sabotage score
     * @param _playerID: Id of the player.
     * @return void
     */
    [ServerRpc(requireOwnership:false)]
    public void AddPointSabotage() 
    {
        m_currentScoreSabotage.value += m_sabotageValue;
        m_nbSabotaged++;
    }

    /*
     * @details This function deducts 1 from the Sabotage score if it is not equal to 0.
     * @param _playerID: Id of the player.
     * @return void
     */
    [ServerRpc(requireOwnership:false)]
    public void SubPointSabotage(PlayerID _playerID)
    {
        m_currentScoreSabotage.value -= m_repairValue;
        if (m_currentScoreSabotage.value < 0)
            m_currentScoreSabotage.value = 0;
        m_nbSabotaged--;
    }

    /*
     * @details This function adds +1 to the broken score.
     *          Invokes an event if totalBroken > max broken
     * @param _playerID: Id of the player.
     * @param _valeurObjectBroken: The monetary value of the item that was broken.
     * @return void
     */
    [ServerRpc(requireOwnership:false)]
    public void AddPointBroken(int _valeurObjectBroken)
    {
        m_currentScoreBroken.value += _valeurObjectBroken;
        if(m_currentScoreBroken.value >= m_maxScoreBroken)
        {
            m_noticeHouseDestroyed?.Invoke(true);
        }
    }

    /*
     * @details This function resets the scores.
     * @return void
     */
    [ServerRpc(requireOwnership:false)]
    public void ResetScore()   
    {
        m_currentScoreBroken.value = 0;
        m_currentScoreSabotage.value = 0;
    }
    
    /*
     * @details This function refreshes the Score UI.
     * @return void
     */
    private void RefreshUI()
    {
        if (InstanceHandler.TryGetInstance(out GhostHUDView ghostHUDView)) {
            ghostHUDView.UpdateScore(m_currentScoreSabotage.value, m_maxScoreSabotage, m_currentScoreBroken.value, m_maxScoreBroken);
        }
        if (InstanceHandler.TryGetInstance(out ChildHUDView childHUDView)){
            childHUDView.UpdateScore(m_currentScoreSabotage.value, m_maxScoreSabotage, m_currentScoreBroken.value, m_maxScoreBroken);
        }
    }
}
