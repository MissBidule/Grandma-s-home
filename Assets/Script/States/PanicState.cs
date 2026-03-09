using System.Collections.Generic;
using PurrNet;
using PurrNet.Logging;
using PurrNet.StateMachine;
using UnityEngine;

public class GhostGameStateData
{
    public List<GhostController> Ghosts;
    public List<PlayerID> AliveGhosts;
    public List<PlayerID> DeadGhosts;
    
    public  GhostGameStateData(List<GhostController> _ghosts, List<PlayerID> _aliveGhosts, List<PlayerID> _deadGhosts)
    {
        Ghosts = _ghosts;
        AliveGhosts = _aliveGhosts;
        DeadGhosts = _deadGhosts;
    }
}

public class PanicState : StateNode<GhostGameStateData>
{
    
    [Header("Round Settings")]
    [SerializeField] [Tooltip("Duration of the round in minutes")] private float m_roundDuration = 1.0f;
    
    private List<GhostController> m_ghosts = new();
    private List<PlayerID> m_aliveGhosts = new();
    private List<PlayerID> m_deadGhosts = new();
    
    public override void Enter(GhostGameStateData _ghostGameStateData, bool _asServer)
    {
        base.Enter(_asServer);
        
        if (!_asServer)
            return;

        GhostInitialize(_ghostGameStateData);
    }

    private void GhostInitialize(GhostGameStateData _ghostGameStateData)
    {
        m_ghosts =  _ghostGameStateData.Ghosts;
        m_aliveGhosts = _ghostGameStateData.AliveGhosts;
        m_deadGhosts = _ghostGameStateData.DeadGhosts;
        
        foreach (GhostController ghostController in m_ghosts)
        {
            if (!ghostController.owner.HasValue)
                return;
            
            ghostController.OnDeathChange += OnGhostDeathChange;
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

            if (m_aliveGhosts.Count == 0)
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
        PurrLogger.Log($"Moving to End of game step. ChildWin: {_childWin}");
        machine.Next(_childWin);
    }
}
