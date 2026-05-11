// ----------------------------------------------------------------------------
// Sample: IPlayerNetworkActions — gameplay action bridge interface.
//
// This was part of the package's public API in v1.x but was moved here in
// v2.0 because the three actions (Interact / Pickup / Drop) are gameplay
// concerns that should live in the project's own gameplay assembly. The
// stub is preserved here as a starting point. Copy it into your project's
// gameplay assembly, expand it with your own actions, and have your
// PlayerNetworkBehaviour subclass implement it.
//
// Usage in your project:
//   1. Copy this file into Assets/_Scripts/Gameplay/ (or similar).
//   2. Change the namespace to your project's.
//   3. Subclass Emrgry.Network.Sync.PlayerNetworkBehaviour and implement
//      the actions as ServerRpcs.
// ----------------------------------------------------------------------------

namespace Emrgry.Network.Samples
{
    /// <summary>
    /// Bridge interface between an input handler (Gameplay assembly) and the
    /// networked player object (Network assembly). Owned-side input calls these
    /// methods, the implementer forwards them to the server as RPCs.
    /// </summary>
    public interface IPlayerNetworkActions
    {
        void RequestInteract();
        void RequestPickup(string itemId);
        void RequestDrop();
    }
}
