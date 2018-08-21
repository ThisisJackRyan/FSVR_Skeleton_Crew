using UnityEngine;

namespace Opsive.ThirdPersonController.Abilities
{
    /// <summary>
    /// The Push ability has been deprecated and replaced by the Move Object ability.
    /// </summary>
    public class Push : Ability
    {
        protected override void Awake()
        {
            Debug.LogWarning("The Push ability has been deprecated. Please use the Move Object ability instead.");
        }
    }
}