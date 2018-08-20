namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// Interface for an item that can use the flashlight.
    /// </summary>
    public interface IFlashlightUseable
    {
        /// <summary>
        /// Returns the input name for the flashlight to be toggled.
        /// </summary>
        /// <returns>The input name for the flashlight to be toggled.</returns>
        string GetToggleFlashlightInputName();

        /// <summary>
        /// Toggles the activate state of the flashlight.
        /// </summary>
        void ToggleFlashlight();

        /// <summary>
        /// Activates or deactivates the flashlight when the item is aimed.
        /// </summary>
        /// <param name="activate">Should the flashlight be active?</param>
        void ActivateFlashlightOnAim(bool activate);
    }
}