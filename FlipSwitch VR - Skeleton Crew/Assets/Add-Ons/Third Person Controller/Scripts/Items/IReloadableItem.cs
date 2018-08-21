namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// Interface for an item that can be reloaded.
    /// </summary>
    public interface IReloadableItem
    {
        /// <summary>
        /// Returns the input name for the item to be reloaded.
        /// </summary>
        /// <returns>The input name for the item to be reloaded.</returns>
        string GetReloadInputName();

        /// <summary>
        /// Tries to reload the item.
        /// </summary>
        /// <returns>Was the reload started?</returns>
        bool TryStartReload();

        /// <summary>
        /// Is the item reloading?
        /// </summary>
        /// <returns>True if the item is reloading.</returns>
        bool IsReloading();

        /// <summary>
        /// Tries to stop the item reload.
        /// </summary>
        void TryStopReload();
    }
}