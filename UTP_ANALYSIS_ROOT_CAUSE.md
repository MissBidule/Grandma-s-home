# UTP Transport - HierarchyV2 Scene Synchronization Analysis

## Problem Statement
When using UTP Transport, clients fail to receive spawned network objects (House, Rooms, PropAnchors) created by the server. Purr Transport works correctly. Network objects are instantiated on the server but never replicated to clients.

## Root Cause Identified

### The Scene Synchronization Flow

When a client connects, the following should happen:

1. **Client Connection (UTP)**
   - UTPClient receives `NetworkEvent.Type.Connect`
   - Calls `onConnected` callback → UTPTransport.OnRemoteConnected(int obj)
   - Invokes: `onConnected?.Invoke(new Connection(conn), true);`

2. **NetworkManager Routing**
   - NetworkManager.OnNewConnection(Connection conn, bool asServer)
   - For server-side connections: `_serverModules.OnNewConnection(conn, true)`
   - Calls ModulesCollection.OnNewConnection()
   - **Invokes `OnConnected(Connection conn, bool asServer)` on all IConnectionListener modules**

3. **Client ID Assignment**
   - Server validates client credentials
   - Sends ServerLoginResponse to client with assigned PlayerID
   - Client receives it → PlayersManager.OnClientLoginResponse()
   - **Fires event: `onLocalPlayerReceivedID?.Invoke(playerId)`**

4. **Scene Synchronization (Should Happen)**
   - HierarchyV2 subscribed to `_playersManager.onLocalPlayerReceivedID`
   - When fired: HierarchyV2.OnPlayerReceivedID(PlayerID player)
   - **Calls: `other.CatchupClient(player)` to send all spawned objects**

### The Critical Issue

**HierarchyV2 does NOT implement `IConnectionListener` interface**

```csharp
// Current implementation
public class HierarchyV2 : IPromoteToServerModule, ITransferToNewServer
// Missing: IConnectionListener
```

**This means:**
- When a client connects, HierarchyV2 is NOT notified
- HierarchyV2 depends ONLY on `onLocalPlayerReceivedID` event from PlayersManager
- Scene state synchronization is triggered by the ServerLoginResponse packet arrival
- **If this packet is delayed, lost, or reordered in the UTP message stream, scene sync fails**

### Why Purr Transport Works

Purr Transport likely uses a different underlying protocol or buffering mechanism that ensures ServerLoginResponse arrives reliably and in order before any gameplay-related packets. UTP's packet ordering might differ, causing race conditions.

## The Blocking Mechanism

**Timing Issue:**
1. Client connects via UTP
2. PlayerSpawningState waits for players to be registered
3. HouseBuilder spawns network objects on server
4. Objects are registered in HierarchyV2._spawnedIdentities
5. **BUT** - Client hasn't received ServerLoginResponse yet
6. Client's OnPlayerReceivedID() never fires
7. CatchupClient() never called
8. Client never receives spawn packets
9. Client enters gameplay with no spawned objects

## Logs Evidence

From the networking logs:
- Host properly creates objects: "spawning Room prefab", "spawning PropAnchor"
- Host shows objects in HierarchyV2._spawnedIdentities  
- Client receives player join events (proves basic network is working)
- Client NEVER receives SpawnPacket messages
- Objects appear in host's view immediately
- Objects never appear in client's view

## Solution Directions

### Option 1: Add IConnectionListener to HierarchyV2 (Recommended)
Make HierarchyV2 implement `IConnectionListener` and trigger `CatchupClient` directly on connection, not waiting for ServerLoginResponse.

```csharp
public class HierarchyV2 : IPromoteToServerModule, ITransferToNewServer, IConnectionListener
{
    public void OnConnected(Connection conn, bool asServer)
    {
        if (asServer)
        {
            // On server: when client connects, send them all current spawns
            if (conn.connId >= 0) // Valid client connection
            {
                CatchupClient(/* get the PlayerID for this connection */);
            }
        }
    }
    
    public void OnDisconnected(Connection conn, bool asServer) { }
}
```

### Option 2: Add Explicit Message to UTPTransport
Ensure ServerLoginResponse is guaranteed to arrive before other messages. Check if UTP's pipeline configuration needs adjustment.

### Option 3: Explicit Scene Sync Request
Add an RPC or network message that client sends after connecting to explicitly request scene state, instead of relying on server-side detection.

## Code Locations

**Key Files:**
- HierarchyV2: `Library/PackageCache/dev.purrnet.purrnet@*/Runtime/CoreModules/HierarchyV2/HierarchyV2.cs`
- PlayersManager: `Library/PackageCache/dev.purrnet.purrnet@*/Runtime/CoreModules/Players/PlayersManager.cs`
- UTPTransport: `Library/PackageCache/dev.purrnet.purrnet@*/Addons/UTP/Runtime/UTPTransport.cs`
- ModulesCollection: `Library/PackageCache/dev.purrnet.purrnet@*/Runtime/Managers/ModulesCollection.cs`
- INetworkModule: `Library/PackageCache/dev.purrnet.purrnet@*/Runtime/Managers/INetworkModule.cs`

**Critical Methods:**
- HierarchyV2.OnPlayerReceivedID() - Line 423
- HierarchyV2.CatchupClient() - Line 1483
- PlayersManager.OnClientLoginResponse() - Line 446
- UTPTransport.OnRemoteConnected() - Line 319
- ModulesCollection.OnNewConnection() - Line 140

## Next Steps

1. Implement Option 1 (Add IConnectionListener to HierarchyV2)
2. Handle the mapping between Connection ID and PlayerID in OnConnected
3. Test with UTP transport to verify scene objects reach clients
4. Verify no duplicate sends occur (since CatchupClient might still be called via ServerLoginResponse)
