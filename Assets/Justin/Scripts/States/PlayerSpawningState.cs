using System.Collections.Generic;
using PurrNet.StateMachine;
using UnityEngine;

public class PlayerSpawningState : StateNode
{
    [SerializeField] private PlayerControllerCore m_playerPrefab;
    [SerializeField] private List<Transform> m_spawnPoints = new();
    
    public override void Enter(bool _asServer)
    {
        base.Enter(_asServer);

        if (!_asServer)
            return;

        DespawnPlayers();

        var spawnedPlayers = SpawnPlayers();

        machine.Next(spawnedPlayers);
    }
    
    private List<PlayerControllerCore> SpawnPlayers()
    {
        var spawnedPlayers = new List<PlayerControllerCore>();
        
        int currentSpawnIndex = 0;
        foreach (var player in networkManager.players)
        {
            var spawnPoint = m_spawnPoints[currentSpawnIndex];
            var newPlayer = Instantiate(m_playerPrefab, spawnPoint.position, spawnPoint.rotation);
            newPlayer.GiveOwnership(player);
            spawnedPlayers.Add(newPlayer);
            
            currentSpawnIndex = (currentSpawnIndex + 1) % m_spawnPoints.Count;
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