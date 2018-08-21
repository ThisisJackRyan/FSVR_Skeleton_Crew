using UnityEngine;
using UnityEngine.UI;

namespace Opsive.ThirdPersonController.UI
{
    /// <summary>
    /// The ItemWheelSliceMonitor will keep each slice of the ItemWheel's UI up in sync with the rest of the game. This includes selecting the currently active Item
    /// and disabling any slices that correspond to an Item that the character does not have in their inventory.
    /// </summary>
    public class ItemWheelSliceMonitor : MonoBehaviour
    {
        [Tooltip("The character that the UI should monitor. Can be null")]
        [SerializeField] protected GameObject m_Character;
        [Tooltip("The ItemType that this slice is monitoring")]
        [SerializeField] protected PrimaryItemType m_PrimaryItemType;
        [Tooltip("The color of the image when the ItemType is active")]
        [SerializeField] protected Color m_SelectedColor;
        [Tooltip("The color of the image when the ItemType is not active")]
        [SerializeField] protected Color m_NotSelectedColor = Color.white;
        [Tooltip("The color of the image when the ItemType is not in the character's inventory")]
        [SerializeField] protected Color m_DisabledColor;

        // SharedFields
        private SharedProperty<Item> m_CurrentPrimaryItem = null;
        private SharedProperty<Item> m_CurrentDualWieldItem = null;
        private SharedMethod<ItemType, bool> m_HasItem = null;

        // Component references
        [System.NonSerialized] private GameObject m_GameObject;
        private Image m_Image;
        private Image[] m_ChildImages;
        private ItemWheelMonitor m_ItemWheelMonitor;
        private GameObject m_ItemWheelGameObject;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_GameObject = gameObject;
            m_Image = GetComponent<Image>();
            m_ChildImages = GetComponentsInChildren<Image>();
            m_ItemWheelMonitor = transform.GetComponentInParent<ItemWheelMonitor>();
            m_ItemWheelGameObject = m_ItemWheelMonitor.gameObject;

            // Start disabled. AttachCharacter will enable the GameObject.
            m_GameObject.SetActive(false);

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

            if (character == null) {
                // The object may be destroyed when Unity is ending.
                if (this != null) {
                    EventHandler.UnregisterEvent<bool>(m_ItemWheelGameObject, "OnItemWheelToggleVisibility", ToggleVisiblity);
                    m_GameObject.SetActive(false);
                    m_Character = null;
                }
                return;
            }

            m_Character = character;

            SharedManager.InitializeSharedFields(m_Character, this);

            ToggleVisiblity(false);

            // Register for the events. Do not register within OnEnable because the character may not be known at that time.
            EventHandler.RegisterEvent<bool>(m_ItemWheelGameObject, "OnItemWheelToggleVisibility", ToggleVisiblity);
        }

        /// <summary>
        /// Show or hide the item slice.
        /// </summary>
        /// <param name="visible">Should the item slice be shown?</param>
        private void ToggleVisiblity(bool visible)
        {
            // Set the correct color if the slice is visible.
            if (visible) {
                if (m_PrimaryItemType == null || !m_HasItem.Invoke(m_PrimaryItemType)) {
                    m_Image.color = m_DisabledColor;
                } else if (m_PrimaryItemType == m_CurrentPrimaryItem.Get().ItemType || (m_CurrentDualWieldItem.Get() != null && m_PrimaryItemType == m_CurrentDualWieldItem.Get().ItemType)) {
                    m_Image.color = m_SelectedColor;
                } else {
                    m_Image.color = m_NotSelectedColor;
                }
                for (int i = 0; i < m_ChildImages.Length; ++i) {
                    m_ChildImages[i].color = m_Image.color;
                }
            }
            m_GameObject.SetActive(visible);
        }

        /// <summary>
        /// This item has been selected. Let the parent wheel monitor know of the change.
        /// </summary>
        public void ItemSelected()
        {
            m_ItemWheelMonitor.ItemSelected(m_PrimaryItemType);
        }

        /// <summary>
        /// The EventHandler was cleared. This will happen when a new scene is loaded. Unregister the registered events to prevent old events from being fired.
        /// </summary>
        private void EventHandlerClear()
        {
            EventHandler.UnregisterEvent<GameObject>("OnCameraAttachCharacter", AttachCharacter);
            EventHandler.UnregisterEvent<bool>(m_ItemWheelGameObject, "OnItemWheelToggleVisibility", ToggleVisiblity);
            EventHandler.UnregisterEvent("OnEventHandlerClear", EventHandlerClear);
        }

        /// <summary>
        /// The object has been destroyed - unregister for all events.
        /// </summary>
        private void OnDestroy()
        {
            EventHandlerClear();
        }
    }
}