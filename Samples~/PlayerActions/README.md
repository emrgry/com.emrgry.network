# Sample — Player Actions Interface

Importable through the Package Manager UI ("Samples" section on the package page).

## What's in here

`IPlayerNetworkActions.cs` — a starter interface that decouples gameplay-layer
input handlers from network-layer RPCs. Copy this into your gameplay assembly,
add the actions your game needs (interact, pickup, drop, attack, etc.), and have
your `PlayerNetworkBehaviour` subclass implement it.

## Why it's a sample and not a built-in interface

In v1.x of this package this interface lived under `Emrgry.Network`. We pulled
it out in v2.0 because (a) the action set is project-specific, and (b) games
end up writing their own interface anyway. Shipping it as a sample makes the
intent explicit and the public API smaller.

## Quick start

```csharp
// In your gameplay assembly:
public sealed class PlayerActions : PlayerNetworkBehaviour, IPlayerNetworkActions
{
    public void RequestInteract() { if (IsOwner) InteractServerRpc(); }
    public void RequestPickup(string id) { if (IsOwner) PickupServerRpc(id); }
    public void RequestDrop() { if (IsOwner) DropServerRpc(); }

    [ServerRpc] private void InteractServerRpc() { /* ... */ }
    [ServerRpc] private void PickupServerRpc(string id) { /* ... */ }
    [ServerRpc] private void DropServerRpc() { /* ... */ }
}
```
