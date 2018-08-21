using UnityEngine;

namespace Opsive.ThirdPersonController.UI
{
    /// <summary>
    /// The SniperScopeMonitor will monitor the visiblity of the scope UI.
    /// </summary>
    public class SniperScopeMonitor : MonoBehaviour
    {
        [Tooltip("The character that the UI should monitor. Can be null")]
        [SerializeField] protected GameObject m_Character;

        // Component references
        [System.NonSerialized] private GameObject m_GameObject;

        /// <summary>
        /// Cache the component references.
        /// </summary>
        private void Awake()
        {
            m_GameObject = gameObject;

            // Start disabled. AttachCharacter will enable the GameObject.
            ShowScope(false);

            EventHandler.RegisterEvent("OnEventHandlerClear", EventHandlerClear);
            if (m_Character == null) {
                EventHandler.RegisterEvent<GameObject>("OnCameraAttachCharacter", AttachCharacter);
            }
        }

        /// <summary>
        /// Attach the character if the character is already assigned.
        /// </summary>
        private void Start()
        {
            if (m_Character != null) {
                AttachCharacter(m_Character);
            }
        }

        /// <summary>
        /// The character has been attached to the camera. Update the UI reference and initialze the character-related values.
        /// </summary>
        /// <param name="character">The character that the UI is monitoring.</param>
        private void AttachCharacter(GameObject character)
        {
            if (m_Character == character) {
                return;
            }

            // The object may be destroyed when Unity is ending.
            if (this != null) {
                ShowScope(false);
            }

            if (m_Character != null) {
                EventHandler.UnregisterEvent<bool>(m_Character, "OnItemShowScope", ShowScope);
            }

            m_Character = character;

            if (character == null) {
                return;
            }

            EventHandler.RegisterEvent<bool>(character, "OnItemShowScope", ShowScope);
        }

        /// <summary>
        /// Shows or hides the scope.
        /// </summary>
        /// <param name="show">Should the scope be shown?</param>
        private void ShowScope(bool show)
        {
            m_GameObject.SetActive(show);
        }

        /// <summary>
        /// The EventHandler was cleared. This will happen when a new scene is loaded. Unregister the registered events to prevent old events from being fired.
        /// </summary>
        private void EventHandlerClear()
        {
            EventHandler.UnregisterEvent<GameObject>("OnCameraAttachCharacter", AttachCharacter);
            EventHandler.UnregisterEvent("OnEventHandlerClear", EventHandlerClear);
        }
    }
}