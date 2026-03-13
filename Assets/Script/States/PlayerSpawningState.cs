using PurrNet;
using System.Collections.Generic;
using PurrNet.StateMachine;
using UnityEngine;
using PurrNet.Modules;
using PurrLobby;
using System;
using PurrNet.Transports;
using Script.UI.Views;
using UI;
using Antony;
using System.Linq;

/*
 * @brief  Contains class declaration for the state PlayerSpawningState
 * @details Script that will handle the correct spawning of each player element
 */
public class PlayerSpawningState : StateNode
{
    [Header("Child spawner")]
    [SerializeField] private ChildController m_childPrefab;
    [SerializeField] private List<Transform> m_childSpawnPoints = new List<Transform>();
    
    [Header("Ghost spawner")]
    [SerializeField] private GhostController m_ghostPrefab;
    [Tooltip("Even if rules are to not despawn on disconnect, this will ignore that and always spawn a player.")]
    [SerializeField] private List<Transform> m_ghostSpawnPoints = new List<Transform>();

    [SerializeField] private PlaydoughShaderManager m_playdoughPrefab;
    [SerializeField] private JellyGhostShaderManager m_jellyPrefab;
    private bool m_isServer = false;
    private bool m_hasStarted = false;

    public override void Enter(bool _asServer)
    {
        base.Enter(_asServer);

        m_isServer = _asServer;
        m_hasStarted = false;
    }

    public void StartMachine()
    {
        if(!m_isServer) return;
        if(m_hasStarted) return;
        m_hasStarted = true;

        DespawnPlayers();

        var spawnedPlayers = SpawnPlayers();

        // We still keep the player list in case for future implementation of round running state.
        machine.Next();
    }
    
    private List<PlayerControllerCore> SpawnPlayers()
    {
        var spawnedPlayers = new List<PlayerControllerCore>();
        var roleKeeper = FindAnyObjectByType<RoleKeeper>();
        
        int currentSpawnChildIndex = 0;
        int currentSpawnGhostIndex = 0;
        foreach (var player in networkManager.players)
        {
            if (NetworkManager.main.TryGetModule(out GlobalOwnershipModule ownership, true) && ownership.PlayerOwnsSomething(player))
                continue;
                
            //CONNECTION
            networkManager.GetModule<PlayersManager>(m_isServer).TryGetConnection(player, out Connection conn);

            bool isGhost = roleKeeper.IsGhost(conn.connectionId);

            Transform spawnPoint;
            PlayerControllerCore newPlayer;

            if (isGhost)
            {
                spawnPoint = m_ghostSpawnPoints[currentSpawnGhostIndex++ %  m_ghostSpawnPoints.Count];
                newPlayer = UnityProxy.Instantiate(m_ghostPrefab, spawnPoint.position, spawnPoint.rotation);
                // foreach (var renderer in newPlayer.GetComponentsInChildren<MeshRenderer>())
                // {
                //     m_jellyPrefab.renderersToModify.Append(renderer);
                // }
            }
            else
            {
                spawnPoint = m_childSpawnPoints[currentSpawnChildIndex++ % m_childSpawnPoints.Count];
                newPlayer = UnityProxy.Instantiate(m_childPrefab, spawnPoint.position, spawnPoint.rotation);
                // foreach (var renderer in newPlayer.GetComponentsInChildren<SkinnedMeshRenderer>())
                // {
                //     m_playdoughPrefab.skinnedMeshRenderersToModify.Append(renderer);
                // }
            } 
            newPlayer.GiveOwnership(player);
            spawnedPlayers.Add(newPlayer);
        }
        
        return spawnedPlayers;
    }

    [ObserversRpc]
    void DisableWaitInterface()
    {
        if (!InstanceHandler.TryGetInstance(out UIsManager uisManager))
            return;
        uisManager.HideView<WaitForPlayerView>();
    }
    
    private void DespawnPlayers()
    {
        var allPlayers = FindObjectsByType<PlayerControllerCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            Destroy(player.gameObject);
        }
    }

}