using System.Collections;
using PurrNet.Logging;
using PurrNet.StateMachine;
using UnityEngine;

public class WaitForPlayerState : StateNode
{
    // Need to be updated to wait for the number of player in the lobby
    [SerializeField] private int m_minPlayers = -1;

    public void set_numPlayers(int numPlayers)
    {
        PurrLogger.Log("Number of players in lobby: " + numPlayers);
        m_minPlayers = numPlayers;
    }
    
    public override void Enter(bool _asServer)
    {
        base.Enter(_asServer);
        
        if (!_asServer)
            return;
        
        StartCoroutine(WaitForPlayers());
    }

    private IEnumerator WaitForPlayers()
    {
        if (m_minPlayers == -1)
            yield return null;
        
        while (networkManager?.playerCount < m_minPlayers)
            yield return null;
        
        machine.Next();
    }
}