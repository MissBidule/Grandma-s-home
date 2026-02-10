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
        [SerializeField] private GameObject _childPrefab;
        [Tooltip("Even if rules are to not despawn on disconnect, this will ignore that and always spawn a player.")]
        [SerializeField] private List<Transform> ChildSpawnPoints = new List<Transform>();

        [Header("Ghost spawner")]
        [SerializeField, HideInInspector] private NetworkIdentity ghostPrefab;
        [SerializeField] private GameObject _ghostPrefab;
        [Tooltip("Even if rules are to not despawn on disconnect, this will ignore that and always spawn a player.")]
        [SerializeField] private List<Transform> GhostSpawnPoints = new List<Transform>();

        [Header("Other")]
        [SerializeField] private bool _ignoreNetworkRules;

        private int _currentChildSpawnPoint;
        private int _currentGhostSpawnPoint;

        private IProvideSpawnPoints _spawnPointProvider;
        private IProvidePrefabInstantiated _prefabInstantiatedProvider;
        
        //Temporary half player gets ghost
        [SerializeField] private bool ghostSpawn = true;

        /// <summary>
        /// Sets a provider that will be used to provide spawn points for players.
        /// Spawn points lists will be ignored.
        /// </summary>
        public void SetRespawnPointProvider(IProvideSpawnPoints provider)
        {
            _spawnPointProvider = provider;
        }

        /// <summary>
        /// Resets the spawn point provider.
        /// Uses the spawn points list instead.
        /// </summary>
        public void ResetSpawnPointProvider()
        {
            _spawnPointProvider = null;
        }

        /// <summary>
        /// Sets a provider that will be used to notify when a player prefab has been instantiated.
        /// </summary>
        public void SetPrefabInstantiatedProvider(IProvidePrefabInstantiated provider)
        {
            _prefabInstantiatedProvider = provider;
        }

        /// <summary>
        /// Resets the prefab instantiated provider.
        /// </summary>
        public void ResetPrefabInstantiatedProvider()
        {
            _prefabInstantiatedProvider = null;
        }

        private void Awake()
        {
            CleanupSpawnPoints();
        }

        private void CleanupSpawnPoints()
        {
            bool hadNullEntry = false;
            for (int i = 0; i < ChildSpawnPoints.Count; i++)
            {
                if (!ChildSpawnPoints[i])
                {
                    hadNullEntry = true;
                    ChildSpawnPoints.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < GhostSpawnPoints.Count; i++)
            {
                if (!GhostSpawnPoints[i])
                {
                    hadNullEntry = true;
                    GhostSpawnPoints.RemoveAt(i);
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
                _childPrefab = childPrefab.gameObject;
                childPrefab = null;
            }
            if (ghostPrefab)
            {
                _ghostPrefab = ghostPrefab.gameObject;
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
            if (!_ignoreNetworkRules && !isDestroyOnDisconnectEnabled && main.TryGetModule(out GlobalOwnershipModule ownership, true) &&
                ownership.PlayerOwnsSomething(player))
                return;

            GameObject newPlayer;

            CleanupSpawnPoints();

            GameObject prefab = ghostSpawn ? _ghostPrefab : _childPrefab;
            if (_spawnPointProvider != null)
            {
                var point = _spawnPointProvider.NextSpawnPoint(player, scene);
                newPlayer = UnityProxy.Instantiate(prefab, point.position, point.rotation, unityScene);
            }
            else if (ChildSpawnPoints.Count > 0 && GhostSpawnPoints.Count > 0)
            {
                var spawnPoint = ghostSpawn ? GhostSpawnPoints[_currentGhostSpawnPoint++] : ChildSpawnPoints[_currentChildSpawnPoint++];
                _currentGhostSpawnPoint = (_currentGhostSpawnPoint + 1) % GhostSpawnPoints.Count;
                _currentChildSpawnPoint = (_currentChildSpawnPoint + 1) % ChildSpawnPoints.Count;
                newPlayer = UnityProxy.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, unityScene);
            }
            else
            {
                prefab.transform.GetPositionAndRotation(out var position, out var rotation);
                newPlayer = UnityProxy.Instantiate(prefab, position, rotation, unityScene);
            }
            ghostSpawn = !ghostSpawn;

            _prefabInstantiatedProvider?.OnPrefabInstantiated(newPlayer, player, scene);

            if (newPlayer.TryGetComponent(out NetworkIdentity identity))
                identity.GiveOwnership(player);
        }
    }
}
