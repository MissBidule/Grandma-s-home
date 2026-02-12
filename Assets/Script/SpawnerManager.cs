using System.Collections.Generic;
using PurrNet.Logging;
using PurrNet.Modules;
using UnityEngine;

namespace PurrNet
{
    [AddComponentMenu("PurrNet/Player Spawner")]
    public class SpawnerManager : PurrMonoBehaviour
    {
        [Header("Child spawner")]
        [SerializeField, HideInInspector] private NetworkIdentity childPrefab;
        [SerializeField] private GameObject m_childPrefab;
        [Tooltip("Even if rules are to not despawn on disconnect, this will ignore that and always spawn a player.")]
        [SerializeField] private List<Transform> m_childSpawnPoints = new List<Transform>();

        [Header("Ghost spawner")]
        [SerializeField, HideInInspector] private NetworkIdentity ghostPrefab;
        [SerializeField] private GameObject m_ghostPrefab;
        [Tooltip("Even if rules are to not despawn on disconnect, this will ignore that and always spawn a player.")]
        [SerializeField] private List<Transform> m_ghostSpawnPoints = new List<Transform>();

        [Header("Other")]
        [SerializeField] private bool m_ignoreNetworkRules;

        private int m_currentChildSpawnPoint;
        private int m_currentGhostSpawnPoint;

        private IProvideSpawnPoints m_spawnPointProvider;
        private IProvidePrefabInstantiated m_prefabInstantiatedProvider;
        
        //Temporary half player gets ghost
        [SerializeField] private bool ghostSpawn = true;

        /// <summary>
        /// Sets a provider that will be used to provide spawn points for players.
        /// Spawn points lists will be ignored.
        /// </summary>
        public void SetRespawnPointProvider(IProvideSpawnPoints provider)
        {
            m_spawnPointProvider = provider;
        }

        /// <summary>
        /// Resets the spawn point provider.
        /// Uses the spawn points list instead.
        /// </summary>
        public void ResetSpawnPointProvider()
        {
            m_spawnPointProvider = null;
        }

        /// <summary>
        /// Sets a provider that will be used to notify when a player prefab has been instantiated.
        /// </summary>
        public void SetPrefabInstantiatedProvider(IProvidePrefabInstantiated provider)
        {
            m_prefabInstantiatedProvider = provider;
        }

        /// <summary>
        /// Resets the prefab instantiated provider.
        /// </summary>
        public void ResetPrefabInstantiatedProvider()
        {
            m_prefabInstantiatedProvider = null;
        }

        private void Awake()
        {
            CleanupSpawnPoints();
        }

        private void CleanupSpawnPoints()
        {
            bool hadNullEntry = false;
            for (int i = 0; i < m_childSpawnPoints.Count; i++)
            {
                if (!m_childSpawnPoints[i])
                {
                    hadNullEntry = true;
                    m_childSpawnPoints.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < m_ghostSpawnPoints.Count; i++)
            {
                if (!m_ghostSpawnPoints[i])
                {
                    hadNullEntry = true;
                    m_ghostSpawnPoints.RemoveAt(i);
                    i--;
                }
            }

            if (hadNullEntry)
                PurrLogger.LogWarning($"Some spawn points were invalid and have been cleaned up.", this);
        }

        private void OnValidate()
        {
            if (childPrefab)
            {
                m_childPrefab = childPrefab.gameObject;
                childPrefab = null;
            }
            if (ghostPrefab)
            {
                m_ghostPrefab = ghostPrefab.gameObject;
                ghostPrefab = null;
            }
        }

        public override void Subscribe(NetworkManager manager, bool asServer)
        {
            if (asServer && manager.TryGetModule(out ScenePlayersModule scenePlayersModule, true))
            {
                scenePlayersModule.onPlayerLoadedScene += OnPlayerLoadedScene;

                if (!manager.TryGetModule(out ScenesModule scenes, true))
                    return;

                if (!scenes.TryGetSceneID(gameObject.scene, out var sceneID))
                    return;

                if (scenePlayersModule.TryGetPlayersInScene(sceneID, out var players))
                {
                    foreach (var player in players)
                        OnPlayerLoadedScene(player, sceneID, true);
                }
            }
        }

        public override void Unsubscribe(NetworkManager manager, bool asServer)
        {
            if (asServer && manager.TryGetModule(out ScenePlayersModule scenePlayersModule, true))
                scenePlayersModule.onPlayerLoadedScene -= OnPlayerLoadedScene;
        }

        private void OnDestroy()
        {
            if (NetworkManager.main &&
                NetworkManager.main.TryGetModule(out ScenePlayersModule scenePlayersModule, true))
                scenePlayersModule.onPlayerLoadedScene -= OnPlayerLoadedScene;
        }

        private void OnPlayerLoadedScene(PlayerID player, SceneID scene, bool asServer)
        {
            var main = NetworkManager.main;

            if (!main || !main.TryGetModule(out ScenesModule scenes, true))
                return;

            var unityScene = gameObject.scene;

            if (!scenes.TryGetSceneID(unityScene, out var sceneID))
                return;

            if (sceneID != scene)
                return;

            if (!asServer)
                return;

            bool isDestroyOnDisconnectEnabled = main.networkRules.ShouldDespawnOnOwnerDisconnect();
            if (!m_ignoreNetworkRules && !isDestroyOnDisconnectEnabled && main.TryGetModule(out GlobalOwnershipModule ownership, true) &&
                ownership.PlayerOwnsSomething(player))
                return;

            GameObject newPlayer;

            CleanupSpawnPoints();

            GameObject prefab = ghostSpawn ? m_ghostPrefab : m_childPrefab;
            if (m_spawnPointProvider != null)
            {
                var point = m_spawnPointProvider.NextSpawnPoint(player, scene);
                newPlayer = UnityProxy.Instantiate(prefab, point.position, point.rotation, unityScene);
            }
            else if (m_childSpawnPoints.Count > 0 && m_ghostSpawnPoints.Count > 0)
            {
                var spawnPoint = ghostSpawn ? m_ghostSpawnPoints[m_currentGhostSpawnPoint++] : m_childSpawnPoints[m_currentChildSpawnPoint++];
                m_currentGhostSpawnPoint = (m_currentGhostSpawnPoint + 1) % m_ghostSpawnPoints.Count;
                m_currentChildSpawnPoint = (m_currentChildSpawnPoint + 1) % m_childSpawnPoints.Count;
                newPlayer = UnityProxy.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, unityScene);
            }
            else
            {
                prefab.transform.GetPositionAndRotation(out var position, out var rotation);
                newPlayer = UnityProxy.Instantiate(prefab, position, rotation, unityScene);
            }
            ghostSpawn = !ghostSpawn;

            m_prefabInstantiatedProvider?.OnPrefabInstantiated(newPlayer, player, scene);

            if (newPlayer.TryGetComponent(out NetworkIdentity identity))
                identity.GiveOwnership(player);
        }
    }
}
