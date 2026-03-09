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
    private List<PlayerID> m_playerIds = new();
    private List<GhostController> m_ghosts = new();
    private List<ChildController> m_childs = new();
    
    private List<PlayerID> m_aliveGhosts = new();
    private List<PlayerID> m_deadGhosts = new();

    private void ClearLists()
    {
        m_playerIds.Clear();
        m_ghosts.Clear();
        m_childs.Clear();
        
        m_aliveGhosts.Clear();
        m_deadGhosts.Clear();
    } 
    
    public override void Enter(List<PlayerControllerCore> _players, bool _asServer)
    {
        base.Enter(_players, _asServer);
        
        if(!_asServer)
            return;

        ClearLists();
        for (var index = 0; index < _players.Count; index++)
        {
            var player = _players[index];
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

            if (m_deadGhosts.Count == m_ghosts.Count)
            {
                PurrLogger.Log("Moving to End of game step");
            }
        }
        else
        {
            // Revive Case
            if (m_deadGhosts.Contains(_playerID))
            {
                m_deadGhosts.Remove(_playerID);
                m_aliveGhosts.Add(_playerID);
            }
        }
    }
}