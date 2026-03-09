using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

public class EndGameState : StateNode<bool>
{
    public override void Enter(bool _childWin, bool _asServer)
    {
        base.Enter(_asServer);
        
        if (!_asServer)
            return;

        SetupEndGameUI(_childWin);
    }

    [ObserversRpc]
    private void SetupEndGameUI(bool _childWin)
    {
        
    }
}
