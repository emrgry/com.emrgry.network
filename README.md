# com.emrgry.network

NGO-based multiplayer infrastructure: Relay connection, session management, networked scene loading, player spawning, and network sync.

## Requirements

- Unity 6000.0+
- [UniTask](https://github.com/Cysharp/UniTask) — install manually first:
  ```
  https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
  ```
- [com.emrgry.core](https://github.com/emrgry/com.emrgry.core) — install before this package:
  ```
  https://github.com/emrgry/com.emrgry.core.git#v1.0.3
  ```
- Unity Netcode for GameObjects (`com.unity.netcode.gameobjects`)
- Unity Services Multiplayer (`com.unity.services.multiplayer`)

## Installation Order

1. UniTask (git URL)
2. com.emrgry.core (git URL)
3. com.emrgry.network (git URL)

```
https://github.com/emrgry/com.emrgry.network.git#v1.0.2
```

## Contents

| Namespace | Class | Description |
|-----------|-------|-------------|
| `Emrgry.Network` | `IConnectionService` / `ConnectionService` | Host/join via Relay or local |
| `Emrgry.Network` | `ISessionService` / `SessionManager` | Player slots, ready state, phase |
| `Emrgry.Network` | `INetworkSceneService` / `NetworkSceneService` | Server-authoritative scene loading |
| `Emrgry.Network` | `IPlayerSpawnService` / `PlayerSpawnService` | Player spawn management |
| `Emrgry.Network` | `IRelayAdapter` / `RelayAdapter` | Unity Relay wrapper |
| `Emrgry.Network.Spawn` | `SpawnPoint` | MonoBehaviour spawn point |
| `Emrgry.Network.Spawn` | `GameSceneSpawner` | Teleports players on scene load |
| `Emrgry.Network.Sync` | `PlayerNetworkBehaviour` | Networked player (display name, RPCs) |

## Notes

- `NetworkBootstrap` and `NetworkConfig` are **not included** — create project-specific versions
- `SessionManager` is a NetworkBehaviour — add it to a networked prefab in your scene
