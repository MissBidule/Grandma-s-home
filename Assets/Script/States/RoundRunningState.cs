using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PurrNet;
using PurrNet.Logging;
using PurrNet.StateMachine;
using UnityEngine;

/*
 * @brief  Contains class declaration for the state RoundRuningState
 * @details Script that will handle the main gameplay loop sequence
 */
public class RoundRuningState : StateNode<List<PlayerControllerCore>>
{
    [Header("Round Settings")]
    [SerializeField] [Tooltip("Duration of the round in minutes")] private float m_roundDuration;
    [SerializeField] [Tooltip("Number of time the sky will move in the round.")] private int m_sunIncrementNumber = 12;
    // TODO Skybox & directional light reference.
    
    // State Reference
    private PanicState m_panicState;
    private EndGameState m_endGameState;
    
    // Players Info
    private List<PlayerID> m_playerIds = new();
    private List<GhostController> m_ghosts = new();
    private List<ChildController> m_childs = new();
    
    private List<PlayerID> m_aliveGhosts = new();
    private List<PlayerID> m_deadGhosts = new();
    
    // Coroutine
    private Coroutine m_roundTimer;
    
    public override void Enter(List<PlayerControllerCore> _players, bool _asServer)
    {
        base.Enter(_players, _asServer);
        
        if(!_asServer)
            return;

        foreach (StateNode state in machine.states)
        {
            if (state is PanicState panicState)
                m_panicState = panicState;
            
            if (state is EndGameState endGameState)
                m_endGameState = endGameState;
        }

        ClearLists();
        
        RegisteringListener(_players);

        m_roundTimer = StartCoroutine(RoundTimer(m_roundDuration*60));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (m_roundTimer != null)
            StopCoroutine(m_roundTimer);
        
        // Unsubscribe from ghost death events
        foreach (var ghost in m_ghosts)
        {
            if (ghost != null)
                ghost.OnDeathChange -= OnGhostDeathChange;
        }

        // TODO Unlisten to Score manager
    }

    private void ClearLists()
    {
        m_playerIds.Clear();
        m_ghosts.Clear();
        m_childs.Clear();
        
        m_aliveGhosts.Clear();
        m_deadGhosts.Clear();
    } 

    private void RegisteringListener(List<PlayerControllerCore> _players)
    {
        foreach (PlayerControllerCore player in _players)
        {
            if (!player.owner.HasValue)
                return;
            m_playerIds.Add(player.owner.Value);
            switch (player)
            {
                case GhostController controller:
                    m_ghosts.Add(controller);
                    controller.OnDeathChange += OnGhostDeathChange;
                    m_aliveGhosts.Add(player.owner.Value);
                    break;

                case ChildController controller:
                    m_childs.Add(controller);
                    break;
                
                default:
                    PurrLogger.LogError($"Unknown PType player ID : {player.owner.HasValue}");
                    break;
            }
        }
        
        // TODO Listen to Score manager
    }

    /*
     * @brief The timer of the round, and sun mover
     * @param float _roundDuration !!! In seconds
     */
    private IEnumerator RoundTimer(float _roundDuration)
    {
        for (var i = 0; i < m_sunIncrementNumber; i++)
        {
            yield return new WaitForSeconds(_roundDuration/m_sunIncrementNumber);
            // TODO move the sun to reflect time change
        }
        // Time ended
        MoveToEnd(true);
    }

    private void OnHouseBreak(string _houseBreakReason)
    {
        // TODO need Score manager
        if (string.IsNullOrEmpty(_houseBreakReason))
            return;
        
        if (_houseBreakReason == "break")
            MoveToEnd(false);
        if (_houseBreakReason == "sabotage")
            MoveToPanic();
    }

    private void MoveToPanic()
    {
        PurrLogger.Log($"Moving to Panic State");
        machine.SetState(m_panicState, new GhostGameStateData(m_ghosts, m_aliveGhosts, m_deadGhosts));
    }

    private void OnGhostDeathChange(bool _deathOrRevive, PlayerID _playerID)
    {
        if (_deathOrRevive)
        {
            // Death Case
            if (m_aliveGhosts.Contains(_playerID))
            {
                m_aliveGhosts.Remove(_playerID);
                m_deadGhosts.Add(_playerID);
            }

            if (m_deadGhosts.Count >= m_ghosts.Count)
            {
                MoveToEnd(true);
            }
        }
        else
        {
            // Revive Case
            if (!m_deadGhosts.Contains(_playerID)) return;
            m_deadGhosts.Remove(_playerID);
            m_aliveGhosts.Add(_playerID);
        }
    }
    
    private void MoveToEnd(bool _childWin)
    {
        PurrLogger.Log($"Moving to End of game step child in:{_childWin}");
        machine.SetState(m_endGameState, _childWin);
    }
}