using PurrNet;
using System.Collections.Generic;
using PurrNet.StateMachine;
using UnityEngine;

public class PlayerSpawningState : StateNode
{
    [Header("Child spawner")]
    [SerializeField] private ChildController m_childPrefab;
    [SerializeField] private List<Transform> m_childSpawnPoints = new List<Transform>();
    
    [Header("Ghost spawner")]
    [SerializeField] private GhostController m_ghostPrefab;
    [Tooltip("Even if rules are to not despawn on disconnect, this will ignore that and always spawn a player.")]
    [SerializeField] private List<Transform> m_ghostSpawnPoints = new List<Transform>();
    
    public override void Enter(bool _asServer)
    {
        base.Enter(_asServer);

        if (!_asServer)
            return;

        DespawnPlayers();

        var spawnedPlayers = SpawnPlayers();

        // We still keep the player list in case for future implementation of round running state.
        machine.Next();
    }
    
    private List<PlayerControllerCore> SpawnPlayers()
    {
        var spawnedPlayers = new List<PlayerControllerCore>();
        
        int currentSpawnIndex = 0;
        foreach (var player in networkManager.players)
        {
            bool isChild = currentSpawnIndex % 2 == 0;

            Transform spawnPoint;
            PlayerControllerCore newPlayer;

            if (isChild)
            {
                spawnPoint = m_childSpawnPoints[(currentSpawnIndex / 2) % m_childSpawnPoints.Count];
                newPlayer = UnityProxy.Instantiate(m_childPrefab, spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                spawnPoint = m_ghostSpawnPoints[(currentSpawnIndex / 2) %  m_ghostSpawnPoints.Count];
                newPlayer = UnityProxy.Instantiate(m_ghostPrefab, spawnPoint.position, spawnPoint.rotation);
            } 
            newPlayer.GiveOwnership(player);
            spawnedPlayers.Add(newPlayer);
            
            currentSpawnIndex = currentSpawnIndex + 1;
        }

        return spawnedPlayers;
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