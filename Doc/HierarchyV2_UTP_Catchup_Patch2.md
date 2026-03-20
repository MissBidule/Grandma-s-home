# HierarchyV2 UTP catchup fix — Seconds Patch

This document summarizes all code changes applied in this workspace so you can replicate them in the target repository.

## Target package/version in this workspace

- `Library/PackageCache/dev.purrnet.purrnet@b49cb3d43db5`

> Your original notes used another hash (`ade910d90f01`).
> Reapply the same logical edits in the equivalent files for your target package hash.

---

## 1) Core functional fix (connection callbacks were not reaching HierarchyV2)

### Why this fallback

`HierarchyV2` now implements `IConnectionListener`, but it is **not** an `INetworkModule` directly registered in `ModulesCollection`.
`HierarchyFactory` is the module that owns per-scene `HierarchyV2` instances.
So transport connect/disconnect callbacks must be forwarded from `HierarchyFactory` to each `HierarchyV2`.

### File changed in step 1

- `Runtime/CoreModules/HierarchyV2/HierarchyFactory.cs`

### Changes

1. Add namespace:

```csharp
using PurrNet.Transports;
```

1. Update class signature:

```csharp
public class HierarchyFactory : INetworkModule, IFixedUpdate, IPreFixedUpdate, ICleanup, IPromoteToServerModule, ITransferToNewServer, IConnectionListener
```

1. Add forwarding methods:

```csharp
public void OnConnected(Connection conn, bool asServer)
{
    for (var i = 0; i < _rawHierarchies.Count; i++)
        _rawHierarchies[i].OnConnected(conn, asServer);
}

public void OnDisconnected(Connection conn, bool asServer)
{
    for (var i = 0; i < _rawHierarchies.Count; i++)
        _rawHierarchies[i].OnDisconnected(conn, asServer);
}
```

---

## 2) Robust catchup fallback (post-join should always try catchup)

### Why

With UTP timing, connection->player mapping can be late or callback order can differ. If pending-connection logic misses, catchup should still happen when `onPostPlayerJoined` fires.

### File changed in step 2

- `Runtime/CoreModules/HierarchyV2/HierarchyV2.cs`

### Change in `OnServerPostPlayerJoined(...)`

Before:

- If `TryGetConnection(playerId, out conn)` failed, returned early (no catchup).
- Catchup only happened when pending connection removal succeeded.

After:

- Still removes pending connection when available.
- **Always calls** `TryServerCatchup(playerId)` (dedupe protected).

Implemented shape:

```csharp
private void OnServerPostPlayerJoined(PlayerID playerId, bool isReconnect, bool asServer)
{
    if (!asServer)
        return;

    // If transport connection callbacks were missed/ordered differently,
    // post-join is still a safe point to perform catchup.
    if (_playersManager.TryGetConnection(playerId, out var conn))
        _pendingServerCatchupConnections.Remove(conn);

    TryServerCatchup(playerId);
}
```

---

## 3) Debug instrumentation added (for event-order diagnosis)

Added temporary debug trace logs to inspect the full path:

- transport connection callback arrival,
- forwarding to hierarchies,
- mapping resolution,
- catchup execution/skips,
- spawn count actually sent.

All logs are behind:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
#endif
```

So they should not run in non-development player builds.

### File changed in step 3a

- `Runtime/CoreModules/HierarchyV2/HierarchyFactory.cs`

Added logs in:

- `OnConnected(...)`
- `OnDisconnected(...)`

Added helper:

```csharp
private static void LogCatchupTrace(string msg)
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    PurrLogger.Log($"[HierarchyV2 CatchupTrace] {msg}");
#endif
}
```

### File changed in step 3b

- `Runtime/CoreModules/HierarchyV2/HierarchyV2.cs`

Added logs in:

- `OnConnected(...)`
- `OnDisconnected(...)`
- `OnServerPostPlayerJoined(...)`
- `TryServerCatchup(...)`
- end of `CatchupClient(...)` with count summary

Also added same helper:

```csharp
private static void LogCatchupTrace(string msg)
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    PurrLogger.Log($"[HierarchyV2 CatchupTrace] {msg}");
#endif
}
```

### Log tag to filter in console

- `[HierarchyV2 CatchupTrace]`

---

## 4) Already present from your previous patch (kept)

In `HierarchyV2.cs`:

- `HierarchyV2` implements `IConnectionListener`.
- Added fields:

```csharp
private readonly HashSet<Connection> _pendingServerCatchupConnections = new();
private readonly HashSet<PlayerID> _serverCatchupDone = new();
```

- In `Enable()`:

```csharp
_playersManager.onPostPlayerJoined += OnServerPostPlayerJoined;
```

- In `Disable()`:

```csharp
_playersManager.onPostPlayerJoined -= OnServerPostPlayerJoined;
_pendingServerCatchupConnections.Clear();
_serverCatchupDone.Clear();
```

- Added methods:

  - `OnConnected(Connection connection, bool asServer)`
  - `OnDisconnected(Connection connection, bool asServer)`
  - `OnServerPostPlayerJoined(PlayerID playerId, bool isReconnect, bool asServer)`
  - `TryServerCatchup(PlayerID playerId)`

- `CatchupClient(PlayerID)` body behavior was preserved (only debug summary log appended at end).

---

## 5) Replication checklist for target repository

1. Locate package path:

   - `Library/PackageCache/dev.purrnet.purrnet@<your_hash>/Runtime/CoreModules/HierarchyV2/`

1. Reapply `HierarchyFactory.cs` changes:

   - add `using PurrNet.Transports;`
   - implement `IConnectionListener`
   - forward `OnConnected/OnDisconnected` to `_rawHierarchies`

1. Reapply `HierarchyV2.cs` fallback change:

   - `OnServerPostPlayerJoined(...)` must always call `TryServerCatchup(playerId)`

1. (Optional but recommended while diagnosing) reapply debug logs + helper methods.

1. Run in Editor/Development build and filter logs by:

   - `[HierarchyV2 CatchupTrace]`

1. Once verified, you can remove/disable debug logs while keeping the functional fixes.

---

## 6) Expected success signal in logs

For a connecting client, you should see sequence like:

1. `Factory OnConnected ...`
2. `OnConnected scene=...`
3. either immediate map resolution OR pending add
4. `OnServerPostPlayerJoined ...`
5. `TryServerCatchup executing ...`
6. `CatchupClient finished ... sentSpawnEvents=...`

If step 6 appears with `sentSpawnEvents > 0`, catchup path is active.

---

## 7) Ready-to-copy `HierarchyFactory.cs` (with logs)

```csharp
using System.Collections.Generic;
using PurrNet.Logging;
using PurrNet.Transports;
using UnityEngine.SceneManagement;

namespace PurrNet.Modules
{
    public class HierarchyFactory : INetworkModule, IFixedUpdate, IPreFixedUpdate, ICleanup, IPromoteToServerModule, ITransferToNewServer, IConnectionListener
    {
        readonly ScenesModule _scenes;

        readonly NetworkManager _manager;

        readonly ScenePlayersModule _scenePlayersModule;

        readonly Dictionary<SceneID, HierarchyV2> _hierarchies = new();

        readonly List<HierarchyV2> _rawHierarchies = new();

        readonly PlayersManager _playersManager;

        public HierarchyFactory(NetworkManager manager, ScenesModule scenes, ScenePlayersModule scenePlayersModule,
            PlayersManager playersManager)
        {
            _manager = manager;
            _scenes = scenes;
            _scenePlayersModule = scenePlayersModule;
            _playersManager = playersManager;
        }

        event ValidateSpawnAction _onClientSpawnValidate;

        public event ValidateSpawnAction onClientSpawnValidate
        {
            add
            {
                _onClientSpawnValidate += value;
                foreach (var hierarchy in _rawHierarchies)
                    hierarchy.onClientSpawnValidate += value;
            }
            remove
            {
                _onClientSpawnValidate -= value;
                foreach (var hierarchy in _rawHierarchies)
                    hierarchy.onClientSpawnValidate -= value;
            }
        }

        public event IdentityAction onEarlyIdentityAdded;

        public event IdentityAction onIdentityAdded;

        public event IdentityAction onIdentityRemoved;

        public event ObserverAction onObserverAdded;

        public event ObserverAction onLateObserverAdded;

        public event SpawnedAction onSentSpawnPacket;

        public void Enable(bool asServer)
        {
            var scenes = _scenes.sceneStates;

            foreach (var (id, sceneState) in scenes)
            {
                if (sceneState.scene.isLoaded)
                    OnPreSceneLoaded(id, asServer);
            }

            _scenes.onPreSceneLoaded += OnPreSceneLoaded;
            _scenes.onSceneUnloaded += OnSceneUnloaded;
        }

        public void Disable(bool asServer)
        {
            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].Disable();

            _scenes.onPreSceneLoaded -= OnPreSceneLoaded;
            _scenes.onSceneUnloaded -= OnSceneUnloaded;
        }

        private void OnPreSceneLoaded(SceneID scene, bool asServer)
        {
            if (_hierarchies.ContainsKey(scene))
                return;

            if (!_scenes.TryGetSceneState(scene, out var sceneState))
            {
                PurrLogger.LogError($"Scene {scene} doesn't exist; trying to create hierarchy module for it?");
                return;
            }

            var hierarchy = new HierarchyV2(_manager, scene, sceneState.scene, _scenePlayersModule, _playersManager,
                asServer);

            hierarchy.onEarlyIdentityAdded += OnEarlyIdentityAdded;
            hierarchy.onObserverAdded += OnObserverAdded;
            hierarchy.onLateObserverAdded += OnLateObserverAdded;
            hierarchy.onIdentityAdded += OnIdentityAdded;
            hierarchy.onIdentityRemoved += OnIdentityRemoved;
            hierarchy.onSentSpawnPacket += OnSentSpawnPacket;

            if (_onClientSpawnValidate != null)
            {
                foreach (var del in _onClientSpawnValidate.GetInvocationList())
                    hierarchy.onClientSpawnValidate += (ValidateSpawnAction)del;
            }

            hierarchy.Enable();

            _rawHierarchies.Add(hierarchy);
            _hierarchies.Add(scene, hierarchy);
        }

        private void OnSentSpawnPacket(PlayerID player, SceneID scene, NetworkID identity) =>
            onSentSpawnPacket?.Invoke(player, scene, identity);

        private void OnLateObserverAdded(PlayerID player, NetworkIdentity identity) =>
            onLateObserverAdded?.Invoke(player, identity);

        private void OnEarlyIdentityAdded(NetworkIdentity identity) =>
            onEarlyIdentityAdded?.Invoke(identity);

        private void OnObserverAdded(PlayerID player, NetworkIdentity identity) =>
            onObserverAdded?.Invoke(player, identity);

        private void OnIdentityAdded(NetworkIdentity identity) =>
            onIdentityAdded?.Invoke(identity);

        private void OnIdentityRemoved(NetworkIdentity identity) =>
            onIdentityRemoved?.Invoke(identity);

        private void OnSceneUnloaded(SceneID scene, bool asserver)
        {
            if (!_hierarchies.TryGetValue(scene, out var hierarchy))
            {
                PurrLogger.LogError($"Hierarchy module for scene {scene} doesn't exist; trying to unload it?");
                return;
            }

            hierarchy.Disable();

            hierarchy.onEarlyIdentityAdded -= OnEarlyIdentityAdded;
            hierarchy.onObserverAdded -= OnObserverAdded;
            hierarchy.onLateObserverAdded -= OnLateObserverAdded;
            hierarchy.onIdentityAdded -= OnIdentityAdded;
            hierarchy.onIdentityRemoved -= OnIdentityRemoved;
            hierarchy.onSentSpawnPacket -= OnSentSpawnPacket;

            if (_onClientSpawnValidate != null)
            {
                foreach (var del in _onClientSpawnValidate.GetInvocationList())
                    hierarchy.onClientSpawnValidate -= (ValidateSpawnAction)del;
            }

            _rawHierarchies.Remove(hierarchy);
            _hierarchies.Remove(scene);
        }

        public void FixedUpdate()
        {
            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].PostNetworkMessages();
        }

        public void PreFixedUpdate()
        {
            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].PreNetworkMessages();
        }

        public bool TryGetHierarchy(SceneID sceneId, out HierarchyV2 o)
        {
            return _hierarchies.TryGetValue(sceneId, out o);
        }

        public bool TryGetHierarchy(Scene scene, out HierarchyV2 o)
        {
            if (_scenes.TryGetSceneID(scene, out var sceneId))
                return _hierarchies.TryGetValue(sceneId, out o);
            o = null;
            return false;
        }

        public bool TryGetIdentity(SceneID scene, NetworkID id, out NetworkIdentity result)
        {
            if (_hierarchies.TryGetValue(scene, out var hierarchy))
                return hierarchy.TryGetIdentity(id, out result);
            result = null;
            return false;
        }

        public bool Cleanup()
        {
            for (var i = 0; i < _rawHierarchies.Count; i++)
            {
                if (!_rawHierarchies[i].Cleanup())
                    return false;
            }

            return true;
        }

        public void PromoteToServerModule()
        {
            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].PromoteToServerModule();
        }

        public void PostPromoteToServerModule()
        {
            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].PostPromoteToServerModule();
        }

        public void TransferToNewServer()
        {
            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].TransferToNewServer();
        }

        public void EvaluateVisibilityForPlayer(PlayerID player)
        {
            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].EvaluateVisibilityForPlayer(player);
        }

        // Try fix UTP Transport
        public void OnConnected(Connection conn, bool asServer)
        {
            LogCatchupTrace($"Factory OnConnected conn={conn.connectionId} valid={conn.isValid} asServer={asServer} hierarchies={_rawHierarchies.Count}");

            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].OnConnected(conn, asServer);
        }

        public void OnDisconnected(Connection conn, bool asServer)
        {
            LogCatchupTrace($"Factory OnDisconnected conn={conn.connectionId} valid={conn.isValid} asServer={asServer} hierarchies={_rawHierarchies.Count}");

            for (var i = 0; i < _rawHierarchies.Count; i++)
                _rawHierarchies[i].OnDisconnected(conn, asServer);
        }

        private static void LogCatchupTrace(string msg)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PurrLogger.Log($"[HierarchyV2 CatchupTrace] {msg}");
#endif
        }
    }
}
```

---

## 8) Ready-to-copy `HierarchyV2.cs` catchup block (with logs)

Paste/merge this block into your `HierarchyV2` class (it matches what was implemented in this workspace):

```csharp
// UTP transport fix
private readonly HashSet<Connection> _pendingServerCatchupConnections = new();
private readonly HashSet<PlayerID> _serverCatchupDone = new();

public void OnConnected(Connection connection, bool asServer)
{
    if (!asServer || !connection.isValid)
        return;

    LogCatchupTrace($"OnConnected scene={_sceneId} conn={connection.connectionId} valid={connection.isValid} asServer={asServer}");

    if (_playersManager.TryGetPlayer(connection, out var playerId))
    {
        LogCatchupTrace($"OnConnected resolved player={playerId} conn={connection.connectionId} -> TryServerCatchup");
        TryServerCatchup(playerId);
        return;
    }

    _pendingServerCatchupConnections.Add(connection);
    LogCatchupTrace($"OnConnected pending add conn={connection.connectionId} pendingCount={_pendingServerCatchupConnections.Count}");
}

public void OnDisconnected(Connection connection, bool asServer)
{
    if (!asServer)
        return;

    var removed = _pendingServerCatchupConnections.Remove(connection);
    LogCatchupTrace($"OnDisconnected scene={_sceneId} conn={connection.connectionId} removedPending={removed} pendingCount={_pendingServerCatchupConnections.Count}");
}

private void OnServerPostPlayerJoined(PlayerID playerId, bool isReconnect, bool asServer)
{
    if (!asServer)
        return;

    LogCatchupTrace($"OnServerPostPlayerJoined scene={_sceneId} player={playerId} reconnect={isReconnect} asServer={asServer}");

    // If transport connection callbacks were missed/ordered differently,
    // post-join is still a safe point to perform catchup.
    if (_playersManager.TryGetConnection(playerId, out var conn))
    {
        _pendingServerCatchupConnections.Remove(conn);
        LogCatchupTrace($"OnServerPostPlayerJoined resolved conn={conn.connectionId} pendingCount={_pendingServerCatchupConnections.Count}");
    }
    else
    {
        LogCatchupTrace($"OnServerPostPlayerJoined no-connection-for-player player={playerId}");
    }

    TryServerCatchup(playerId);
}

private void TryServerCatchup(PlayerID playerId)
{
    if (!_serverCatchupDone.Add(playerId))
    {
        LogCatchupTrace($"TryServerCatchup skipped duplicate player={playerId}");
        return;
    }

    LogCatchupTrace($"TryServerCatchup executing player={playerId} scene={_sceneId}");
    CatchupClient(playerId);
}

private void CatchupClient(PlayerID playerId)
{
    var sentCount = 0;

    for (var i = 0; i < _spawnedIdentities.Count; i++)
    {
        var identity = _spawnedIdentities[i];

        if (!identity.isSpawned)
            continue;

        if (!identity.id.HasValue)
            continue;

        if (_toSpawnNextFrame.Contains(identity))
            continue;

        identity.SetIsSpawned(true, false);
        identity.TriggerEarlySpawnEvent(false);

        onSentSpawnPacket?.Invoke(playerId, _sceneId, identity.id.Value);

        if (identity.TryAddObserver(playerId))
        {
            onObserverAdded?.Invoke(playerId, identity);
            identity.TriggerOnPreObserverAdded(playerId, false);
            _triggerLateObserverAdded.Add(new PlayerNid { player = playerId, nid = identity, isSpawner = false });
        }

        identity.TriggerSpawnEvent(false);
        onIdentityAdded?.Invoke(identity);
        sentCount++;
    }

    LogCatchupTrace($"CatchupClient finished player={playerId} scene={_sceneId} sentSpawnEvents={sentCount} spawnedIdentities={_spawnedIdentities.Count}");
}

private static void LogCatchupTrace(string msg)
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    PurrLogger.Log($"[HierarchyV2 CatchupTrace] {msg}");
#endif
}
```
