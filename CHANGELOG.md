# Changelog

## [2.0.0] — 2026-05-11

### Added
- `Emrgry.Network.Bootstrap.NetworkConfigData` — ScriptableObject project config (tick rate, max players, min players to start, transport timeouts, game/lobby scene names, useRelay).
- `Emrgry.Network.Bootstrap.NetworkBootstrap` — drop-in MonoBehaviour. Two-phase init (sync core services → async UGS sign-in → relay/connection). DontDestroyOnLoad + singleton-guarded. Exposes `CoreServicesReady` and `UGSReady` events so optional add-on packages (e.g. lobby) can chain.
- `Emrgry.Network.Connection.ConnectionFlow` — static helper for relay-only flows: `HostRelayAsync`, `JoinRelayAsync`, `LeaveAsync`, `DisconnectAllClientsAsHost`.
- `Emrgry.Network.Spawn.IPlayerSpawnAssignReceiver` — implemented by player NetworkBehaviour to receive owner-side spawn assignments from `GameSceneSpawner`.
- `ISessionService.MinPlayersToStart` and `ISessionService.GameSceneName` — interface members reflect the new configurable knobs.
- `SessionManager` now reads `MaxPlayers` / `MinPlayersToStart` / `GameSceneName` from the active `NetworkConfigData` (via `NetworkBootstrap`), falling back to serialized fields. `IsEveryoneReady` honours `MinPlayersToStart` instead of the hardcoded `>= 2`.

### Changed (Breaking)
- `GameSceneSpawner` — server-side `NetworkTransform.Teleport` removed. The spawner now asks each player's `IPlayerSpawnAssignReceiver` to teleport itself on the owner side, which is the only correct path for owner-authoritative NetworkTransforms. Projects using server-authoritative transforms still work via the transform-mutation fallback.
- `PlayerNetworkBehaviour` — `sealed` removed; class is now extensible with `protected virtual OnOwnerSpawned()` / `OnOwnerDespawned()` hooks. Implements `IPlayerSpawnAssignReceiver` and contains the spawn-point ClientRpc that was previously a workaround in consuming projects.
- `IPlayerNetworkActions` — removed from public API. The interface has been moved to the `Samples~/PlayerActions` folder; import it via Package Manager → Samples or copy it into your gameplay assembly.

### Migration from v1.x

1. **Add `NetworkBootstrap`** to your Bootstrap scene next to (or on the same GameObject as) your `NetworkManager` + `UnityTransport`. Create a `NetworkConfigData` asset and assign it.
2. **Delete your project's `NetworkBootstrap`** if you had one — the package now provides this.
3. **Player prefab** — switch to (or subclass) `Emrgry.Network.Sync.PlayerNetworkBehaviour`. Implementing `IPlayerSpawnAssignReceiver` is free now since the base class does it.
4. **Lobby flows** — `ConnectionFlowHelper`-style helpers that combine relay + lobby live in `com.emrgry.lobby` as `LobbyConnectionFlow`. The new `ConnectionFlow` in this package is relay-only.
5. **`IPlayerNetworkActions`** — copy from `Samples~/PlayerActions` into your gameplay assembly, change the namespace, and have your player subclass implement it.

## [1.0.4] — Previous release
- Initial public APIs: Connection / Session / Scene / Spawn / Sync.
