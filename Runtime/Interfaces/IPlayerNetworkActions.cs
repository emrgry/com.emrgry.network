namespace Emrgry.Network
{
    /// <summary>
    /// Bridge interface between Gameplay assembly (input) and Network assembly (RPCs).
    /// Implemented by PlayerNetworkBehaviour in Network assembly.
    /// Referenced by input handlers in Gameplay assembly via GetComponent.
    /// </summary>
    public interface IPlayerNetworkActions
    {
        void RequestInteract();
        void RequestPickup(string itemId);
        void RequestDrop();
    }
}
