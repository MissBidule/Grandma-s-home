using System.Collections.Generic;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

public class RoundRuningState : StateNode<List<PlayerControllerCore>>
{
    private List<PlayerID> _players = new();
    
    public override void Enter(List<PlayerControllerCore> players, bool asServer)
    {
        base.Enter(players, asServer);
        
        if(!asServer)
            return;
        
        _players.Clear();
        foreach (var player in players)
        {
            if (player.owner.HasValue)
                _players.Add(player.owner.Value);
        }
    }
}