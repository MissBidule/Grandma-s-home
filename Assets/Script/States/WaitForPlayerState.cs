using PurrNet;
using System.Collections;
using PurrNet.Logging;
using PurrNet.StateMachine;
using Script.UI.Views;
using UI;
using UnityEngine;
using PurrLobby;

/*
 * @brief  Contains class declaration for the state WaitForPlayerState
 * @details Script that will wait for each player to be ready before going into the server game
 */
public class WaitForPlayerState : StateNode
{
    // Need to be updated to wait for the number of player in the lobby
    [SerializeField] private int m_minPlayers = 1;
    [SerializeField] private int m_numPlayers = -1;

    public void set_numPlayers(int _numPlayers)
    {
        PurrLogger.Log("Number of players in lobby: " + _numPlayers);
        m_numPlayers = _numPlayers;
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
        
        while (m_numPlayers < m_minPlayers)
            yield return null;

        DisableWaitUIObserverRPC();
        
        machine.Next();
    }

    [ObserversRpc (runLocally: true, bufferLast: true)]
    private void DisableWaitUIObserverRPC()
    {
        if (!InstanceHandler.TryGetInstance(out UIsManager uisManager))
            return;
        uisManager.HideView<WaitForPlayerView>();
        uisManager.ToggleUIVision();
    }
}