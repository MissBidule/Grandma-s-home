# HierarchyV2 UTP catchup patch

This file contains a patch-ready implementation proposal.
No source code was modified.

## Goal

Ensure server-side `CatchupClient()` runs for newly connected clients, even when `Connection -> PlayerID` mapping is not ready yet at transport connect time.

## Why this shape

- `OnConnected(Connection, bool)` may run before `PlayersManager` has registered the player mapping.
- `CatchupClient()` requires `PlayerID`.
- We queue pending connections and resolve them on `onPostPlayerJoined`.
- We deduplicate by `PlayerID` to avoid duplicate catchup sends.

## Proposed changes (patch-ready)

## Exact placement map (where to put each block)

Target file:

- [Library/PackageCache/dev.purrnet.purrnet@ade910d90f01/Runtime/CoreModules/HierarchyV2/HierarchyV2.cs](../Library/PackageCache/dev.purrnet.purrnet@ade910d90f01/Runtime/CoreModules/HierarchyV2/HierarchyV2.cs)

Use these anchors to place code safely:

1. **Class signature**
    - Find the class header near the top (currently `public class HierarchyV2 : IPromoteToServerModule, ITransferToNewServer`).
    - Add `IConnectionListener` in that same declaration.

2. **New fields**
    - Put the two new fields in the private field section, right after `_isPlayerReady` (or nearby with other state fields).
    - Do not place them inside methods.

3. **Enable()/Disable() subscription lines**
    - In `Enable()`, put the new subscribe line next to existing `_playersManager` event wiring.
    - In `Disable()`, put the unsubscribe line next to existing `_playersManager` unwiring.
    - Put `Clear()` calls at the end of `Disable()` after unsubscribes.

4. **`OnConnected` / `OnDisconnected` methods**
    - Add these as class-level methods (same level as `OnPlayerReceivedID`, `OnParentChangedPacket`, etc.).
    - Recommended spot: directly **below** `OnPlayerReceivedID(...)` for readability.

5. **`OnServerPostPlayerJoined` and `TryServerCatchup` helpers**
    - Add as class-level private methods.
    - Recommended spot: just **above** `CatchupClient(...)` so all catchup logic is grouped.

6. **Do not touch existing `CatchupClient(PlayerID)` body**
    - Keep the current implementation intact.
    - Only add the new call-path to reach it reliably.

### Quick insertion checklist

- [ ] Class declaration includes `IConnectionListener`
- [ ] Two new private hash sets added
- [ ] `Enable()` subscribes `onPostPlayerJoined`
- [ ] `Disable()` unsubscribes and clears sets
- [ ] `OnConnected(...)` added (server-only, queue fallback)
- [ ] `OnDisconnected(...)` added (server-only cleanup, no throw)
- [ ] `OnServerPostPlayerJoined(...)` added
- [ ] `TryServerCatchup(...)` added
- [ ] Existing `CatchupClient(...)` unchanged

### 1) Update class signature

In [Library/PackageCache/dev.purrnet.purrnet@ade910d90f01/Runtime/CoreModules/HierarchyV2/HierarchyV2.cs](../Library/PackageCache/dev.purrnet.purrnet@ade910d90f01/Runtime/CoreModules/HierarchyV2/HierarchyV2.cs):

```csharp
public class HierarchyV2 : IPromoteToServerModule, ITransferToNewServer, IConnectionListener
```

### 2) Add fields

Add near other private fields:

```csharp
private readonly HashSet<Connection> _pendingServerCatchupConnections = new();
private readonly HashSet<PlayerID> _serverCatchupDone = new();
```

### 3) Subscribe/unsubscribe in lifecycle

Inside `Enable()`:

```csharp
_playersManager.onPostPlayerJoined += OnServerPostPlayerJoined;
```

Put it in `Enable()` with the other `_playersManager` subscriptions, right after:

```csharp
_playersManager.onNetworkIDReceived += OnNetworkIDReceived;
```

Inside `Disable()`:

```csharp
_playersManager.onPostPlayerJoined -= OnServerPostPlayerJoined;
_pendingServerCatchupConnections.Clear();
_serverCatchupDone.Clear();
```

Put the unsubscribe with the other `_playersManager` unsubscriptions, near:

```csharp
_playersManager.onLocalPlayerReceivedID -= OnPlayerReceivedID;
_playersManager.onNetworkIDReceived -= OnNetworkIDReceived;
```

### 4) Add `IConnectionListener` implementation

```csharp
public void OnConnected(Connection connection, bool asServer)
{
    if (!asServer || !connection.isValid)
        return;

    if (_playersManager.TryGetPlayer(connection, out var playerId))
    {
        TryServerCatchup(playerId);
        return;
    }

    _pendingServerCatchupConnections.Add(connection);
}

public void OnDisconnected(Connection connection, bool asServer)
{
    if (!asServer)
        return;

    _pendingServerCatchupConnections.Remove(connection);
}
```

### 5) Resolve pending connects when player join mapping is ready

```csharp
private void OnServerPostPlayerJoined(PlayerID playerId, bool isReconnect, bool asServer)
{
    if (!asServer)
        return;

    if (!_playersManager.TryGetConnection(playerId, out var conn))
        return;

    if (_pendingServerCatchupConnections.Remove(conn))
        TryServerCatchup(playerId);
}
```

### 6) Deduplicated catchup helper

```csharp
private void TryServerCatchup(PlayerID playerId)
{
    if (!_serverCatchupDone.Add(playerId))
        return;

    CatchupClient(playerId);
}
```

## Notes

- Do not throw in `OnDisconnected`; callbacks should be safe/no-op when not needed.
- This patch is transport-agnostic and should be safe for UTP timing differences.
- If you want reconnects to force a fresh catchup, remove the dedupe check or reset per reconnect policy.
