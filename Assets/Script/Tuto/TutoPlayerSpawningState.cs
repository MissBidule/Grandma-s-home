using PurrNet;
using System.Collections.Generic;
using PurrNet.StateMachine;
using UnityEngine;
using PurrNet.Modules;
using System;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

namespace Script.States
{
    /*
     * @brief  Contains TUTO version of the class declaration for the state PlayerSpawningState
     * @details Script that will handle the correct spawning of each player element
     */
    public class TutoPlayerSpawningState : StateNode
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
            if (!m_isServer) return;
            if (m_hasStarted) return;
            m_hasStarted = true;

            DespawnPlayers();

            List<PlayerControllerCore> spawnedPlayers = SpawnPlayers();

            // We still keep the player list in case for future implementation of round running state.
            Debug.Log($"{spawnedPlayers.Count} Player spawned moving to next state.");

            TutoManager tutoManager = FindAnyObjectByType<TutoManager>();
            if (tutoManager != null)
            {
                GhostController ghost = spawnedPlayers.Find(p => p is GhostController) as GhostController;
                ChildController child = spawnedPlayers.Find(p => p is ChildController) as ChildController;
                if (ghost != null && child != null)
                    tutoManager.Init(ghost, child);
            }

            machine.Next(spawnedPlayers);
        }

        private List<PlayerControllerCore> SpawnPlayers()
        {
            List<PlayerControllerCore> spawnedPlayers = new List<PlayerControllerCore>();

            foreach (var player in networkManager.players)
            {
                if (NetworkManager.main.TryGetModule(out GlobalOwnershipModule ownership, true) && ownership.PlayerOwnsSomething(player))
                    continue;
                //CONNECTION
                GhostController ghost = UnityProxy.Instantiate(m_ghostPrefab, m_ghostSpawnPoints[0].position, m_ghostSpawnPoints[0].rotation);
                ChildController child = UnityProxy.Instantiate(m_childPrefab, m_childSpawnPoints[0].position, m_childSpawnPoints[0].rotation);

                ghost.GiveOwnership(player);
                child.GiveOwnership(player);

                SetPlayerInputActive(ghost.gameObject, true);
                child.gameObject.SetActive(false);

                spawnedPlayers.Add(ghost);
                spawnedPlayers.Add(child);
            }

            return spawnedPlayers;
        }

        private void SetPlayerInputActive(GameObject _player, bool _active)
        {
            PlayerInput input = _player.GetComponent<PlayerInput>();
            if (input != null) input.enabled = _active;

            CinemachineCamera cam = _player.GetComponentInChildren<CinemachineCamera>();
            if (cam != null) cam.enabled = _active;

            AudioListener audio = _player.GetComponentInChildren<AudioListener>();
            if (audio != null) audio.enabled = _active;
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
