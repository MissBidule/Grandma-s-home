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
using PurrNet.Logging;

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
        private bool m_hasStarted = false;

        public override void Enter(bool _asServer)
        {
            base.Enter(_asServer);
            
            if (!isServer)
                return;
            m_hasStarted = false;
        }

        public void StartMachine()
        {
            PurrLogger.Log("Starting machine ...", this);
            if (!isServer)
                return;
            PurrLogger.Log("Check Server", this);
            if (m_hasStarted)
                return;
            PurrLogger.Log("Check HasStarted", this);
            m_hasStarted = true;
            
            DespawnPlayers();
            PurrLogger.Log("Clear player", this);
            
            List<PlayerControllerCore> spawnedPlayers = SpawnPlayers();
            PurrLogger.Log("Spawned player", this);
            
            machine.Next(spawnedPlayers);
        }

        private List<PlayerControllerCore> SpawnPlayers()
        {
            List<PlayerControllerCore> spawnedPlayers = new ();
            //RoleKeeper roleKeeper = FindAnyObjectByType<RoleKeeper>();

            //if (roleKeeper == null)
            //{
            //    return spawnedPlayers;
            //}

            int currentSpawnChildIndex = 0;
            int currentSpawnGhostIndex = 0;

            foreach (PlayerID player in networkManager.players)
            {
                //if (NetworkManager.main.TryGetModule(out GlobalOwnershipModule ownership, true) && ownership.PlayerOwnsSomething(player))
                //{
                //    continue;
                //}

                //CONNECTION
                //networkManager.GetModule<PlayersManager>(m_isServer).TryGetConnection(player, out Connection conn);

                bool isGhost = false;// TODO HAAAAAAAAA roleKeeper.IsGhost(conn.connectionId);

                Transform spawnPoint;
                PlayerControllerCore newPlayer;

                if (isGhost)
                {
                    spawnPoint = m_ghostSpawnPoints[currentSpawnGhostIndex++ % m_ghostSpawnPoints.Count];
                    newPlayer = UnityProxy.Instantiate(m_ghostPrefab, spawnPoint.position, spawnPoint.rotation);
                }
                else
                {
                    spawnPoint = m_childSpawnPoints[currentSpawnChildIndex++ % m_childSpawnPoints.Count];
                    newPlayer = UnityProxy.Instantiate(m_childPrefab, spawnPoint.position, spawnPoint.rotation);
                }
                
                newPlayer.GiveOwnership(player);
                spawnedPlayers.Add(newPlayer);
            }

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