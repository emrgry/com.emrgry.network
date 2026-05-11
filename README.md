# com.emrgry.network

NGO-based multiplayer infrastructure: drop-in `NetworkBootstrap`, Relay connection, session management, networked scene loading, owner-authoritative spawn, and a stock player NetworkBehaviour. Designed so a new project can multiplayer-up by adding the package and dropping one prefab in the Bootstrap scene.

## Requirements

- Unity 6000.0+
- [UniTask](https://github.com/Cysharp/UniTask) (install via git URL first)
- [com.emrgry.core](https://github.com/emrgry/com.emrgry.core) `1.1.0+`
- Unity Netcode for GameObjects (`com.unity.netcode.gameobjects`) `2.0+`
- Unity Services Multiplayer (`com.unity.services.multiplayer`) `2.0+`

## Installation Order

```
# UniTask
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask

# core
https://github.com/emrgry/com.emrgry.core.git#v1.1.0

# network
https://github.com/emrgry/com.emrgry.network.git#v2.0.0
```

## Quick start

1. **Bootstrap scene** — create one. Add a GameObject named `_Network`. Add:
   - `NetworkManager` component (Netcode)
   - `UnityTransport` component (Netcode)
   - **`NetworkBootstrap`** component (this package)
2. **NetworkConfigData asset** — `Assets → Create → Emrgry → Network → NetworkConfigData`. Assign it to NetworkBootstrap.
3. **Player prefab** — give it a `NetworkObject`, a `NetworkTransform` (set Authority = Owner), and `PlayerNetworkBehaviour` (or your subclass). Register the prefab in `NetworkManager.NetworkConfig.PlayerPrefab`.
4. **SessionManager prefab** — create one with a `NetworkObject` + `SessionManager` component. Set the game scene name on it. Add it to `NetworkManager.NetworkConfig.Prefabs`. Spawn it after the host starts (typically from `LobbyBootstrap` or a custom `OnHostStarted` hook).
5. **GameScene** — place `SpawnPoint` MonoBehaviours and a single `GameSceneSpawner` NetworkObject. The spawner teleports players to spawn points on scene load.

## Contents

| Namespace | Type | Description |
|-----------|------|-------------|
| `Emrgry.Network.Bootstrap` | `NetworkConfigData` | Project SO: tick rate, max/min players, transport timeouts, scene names. |
| `Emrgry.Network.Bootstrap` | `NetworkBootstrap` | Drop-in MonoBehaviour. Two-phase init. Exposes `CoreServicesReady` / `UGSReady` events. |
| `Emrgry.Network.Connection` | `IConnectionService` / `ConnectionService` | Host/join via Relay or local. |
| `Emrgry.Network.Connection` | `ConnectionFlow` | Static helper. `HostRelayAsync`, `JoinRelayAsync`, `LeaveAsync`. |
| `Emrgry.Network.Connection` | `IRelayAdapter` / `RelayAdapter` | Unity Relay wrapper. |
| `Emrgry.Network` | `ISessionService` / `SessionManager` | Player slots, ready state, phase, configurable game scene + min-players. |
| `Emrgry.Network` | `INetworkSceneService` / `NetworkSceneService` | Server-authoritative scene loading. Publishes `SceneLoadProgressEvent` and `SceneLoadCompletedEvent`. |
| `Emrgry.Network` | `IPlayerSpawnService` / `PlayerSpawnService` | Player spawn management. |
| `Emrgry.Network.Spawn` | `SpawnPoint` | MonoBehaviour spawn point. Auto-registers with `IPlayerSpawnService`. |
| `Emrgry.Network.Spawn` | `GameSceneSpawner` | Owner-authoritative aware scene spawner. |
| `Emrgry.Network.Spawn` | `IPlayerSpawnAssignReceiver` | Implement on your player NetworkBehaviour to receive spawn-point assignments. |
| `Emrgry.Network.Sync` | `PlayerNetworkBehaviour` | Stock player. Display name, camera target hook, ClientRpc spawn-point teleport, `OnOwnerSpawned/Despawned` overrides. |

## Samples

- **Player Actions Interface** — starter `IPlayerNetworkActions` decoupling input from RPCs. Import via Package Manager → Samples.

## Notes

- The package does not register `ILobbyService`. To add lobby browse / password-protected lobbies install `com.emrgry.lobby`, which subscribes to `NetworkBootstrap.UGSReady` and registers itself.
- `SessionManager` is a NetworkBehaviour on a spawnable prefab; it self-registers as `ISessionService` on `OnNetworkSpawn`. Do not register it manually.
