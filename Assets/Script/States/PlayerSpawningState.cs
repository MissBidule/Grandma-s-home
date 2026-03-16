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
using Script.HouseBuilding;
using System.Linq;
namespace Script.States
{
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
            if (!m_isServer) 
            {
                Debug.Log("[PlayerSpawningState] Not server, returning.");
                return;
            }
            if (m_hasStarted) 
            {
                Debug.LogWarning("[PlayerSpawningState] Already started, ignoring duplicate call.");
                return;
            }
            m_hasStarted = true;

            Debug.Log("[PlayerSpawningState] Starting machine - despawning old players.");
            DespawnPlayers();

            Debug.Log("[PlayerSpawningState] Spawning new players.");
            List<PlayerControllerCore> spawnedPlayers = SpawnPlayers();
            
            Debug.Log("[PlayerSpawningState] Building house network.");
            if (InstanceHandler.TryGetInstance(out HouseBuilder houseBuilder))
            {
                houseBuilder.BuildHouseNetwork();
                Debug.Log("[PlayerSpawningState] House network building initiated.");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawningState] HouseBuilder instance not found!");
            }
            
            Debug.Log($"[PlayerSpawningState] {spawnedPlayers.Count} players spawned, moving to next state.");
            machine.Next(spawnedPlayers);
        }

        private List<PlayerControllerCore> SpawnPlayers()
        {
            List<PlayerControllerCore> spawnedPlayers = new List<PlayerControllerCore>();
            RoleKeeper roleKeeper = FindAnyObjectByType<RoleKeeper>();

            if (roleKeeper == null)
            {
                Debug.LogError("[PlayerSpawningState] RoleKeeper not found in scene!");
                return spawnedPlayers;
            }

            int currentSpawnChildIndex = 0;
            int currentSpawnGhostIndex = 0;
            
            Debug.Log($"[PlayerSpawningState] Spawning players. Total players: {networkManager.players.Count}");

            foreach (var player in networkManager.players)
            {
                if (NetworkManager.main.TryGetModule(out GlobalOwnershipModule ownership, true) && ownership.PlayerOwnsSomething(player))
                {
                    Debug.Log($"[PlayerSpawningState] Player {player} already owns something, skipping spawn.");
                    continue;
                }

                //CONNECTION
                networkManager.GetModule<PlayersManager>(m_isServer).TryGetConnection(player, out Connection conn);

                bool isGhost = roleKeeper.IsGhost(conn.connectionId);

                Transform spawnPoint;
                PlayerControllerCore newPlayer;

                if (isGhost)
                {
                    spawnPoint = m_ghostSpawnPoints[currentSpawnGhostIndex++ % m_ghostSpawnPoints.Count];
                    newPlayer = UnityProxy.Instantiate(m_ghostPrefab, spawnPoint.position, spawnPoint.rotation);
                    Debug.Log($"[PlayerSpawningState] Spawned ghost player at {spawnPoint.position}");
                }
                else
                {
                    spawnPoint = m_childSpawnPoints[currentSpawnChildIndex++ % m_childSpawnPoints.Count];
                    newPlayer = UnityProxy.Instantiate(m_childPrefab, spawnPoint.position, spawnPoint.rotation);
                    Debug.Log($"[PlayerSpawningState] Spawned child player at {spawnPoint.position}");
                }
                
                newPlayer.GiveOwnership(player);
                spawnedPlayers.Add(newPlayer);
            }

            Debug.Log($"[PlayerSpawningState] Total spawned: {spawnedPlayers.Count}");
            return spawnedPlayers;
        }

        [ObserversRpc]
        private void DisableWaitInterface()
        {
            if (!InstanceHandler.TryGetInstance(out UIsManager uisManager))
                return;
            uisManager.HideView<WaitForPlayerView>();
        }

        private void DespawnPlayers()
        {
            PlayerControllerCore[] allPlayers = FindObjectsByType<PlayerControllerCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (PlayerControllerCore player in allPlayers)
            {
                Destroy(player.gameObject);
            }
        }

    }
}