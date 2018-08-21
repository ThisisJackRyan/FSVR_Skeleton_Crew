using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Opsive.ThirdPersonController.Input
{
    /// <summary>
    /// Acts as a common base class for any type of Unity input. Works with keyboard/mouse, controller, and mobile input.
    /// </summary>
    public class UnityInput : PlayerInput
    {
        [Tooltip("Should the mobile input be used? Useful for debugging with Unity remote")]
        [SerializeField] protected bool m_ForceMobileInput;
        [Tooltip("Should the standalone input be used? This will force non-mobile input while on a mobile platform")]
        [SerializeField] protected bool m_ForceStandaloneInput;
        [Tooltip("Should the cursor be disabled with the escape key?")]
        [SerializeField] protected bool m_DisableWithEscape = true;
        [Tooltip("Should the cursor be disabled?")]
        [SerializeField] protected bool m_DisableCursor = true;
        [Tooltip("Should the cursor be disabled with the specified button is down?")]
        [SerializeField] protected bool m_DisableWhenButtonDown;

        // Internal variables
        private UnityInputBase m_Input;
        private bool m_UseMobileInput;
        private bool m_AllowGameplayInput = true;
        private HashSet<string> m_JoystickDownSet;
        private List<string> m_ToAddJoystickDownList;
        private List<string> m_JoystickDownList;

        /// <summary>
        /// Assign the static variables and initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_UseMobileInput = m_ForceMobileInput;
#if !UNITY_EDITOR && !DLL_RELEASE && (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_WP8_1 || UNITY_BLACKBERRY)
            if (!m_ForceStandaloneInput) {
                m_UseMobileInput = true;
            }
#endif
            if (m_UseMobileInput) {
                if (m_Input != null && m_Input is UnityMobileInput) {
                    return;
                }
                m_Input = new UnityMobileInput();
                var virtualButtonManager = GameObject.FindObjectOfType<UnityVirtualButtonManager>();
                if (virtualButtonManager == null) {
                    Debug.LogError("Unable to enable mobile input - no Unity Virtual Button Manager found.");
                } else {
                    virtualButtonManager.EnableVirtualButtons(true);
                }
            } else {
                if (m_Input != null && m_Input is UnityStandaloneInput) {
                    return;
                }
                m_Input = new UnityStandaloneInput();

                if (m_DisableCursor) {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
#if UNITY_EDITOR || DLL_RELEASE
                    StartCoroutine(LockCursor());
#endif
                }
            }
        }

#if UNITY_EDITOR || DLL_RELEASE
        /// <summary>
        /// There is a bug in the Unity editor that prevents the cursor from always being centered when the cursor is locked. It only happens in the editor and can be fixed
        /// by toggling the lock cursor on and off between frames.
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator LockCursor()
        {
            yield return new WaitForEndOfFrame();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield return new WaitForEndOfFrame();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
        
        /// <summary>
        /// Register a VirtualButton when the VirtualButton is enabled.
        /// </summary>
        /// <param name="name">The name of the VirtualButton.</param>
        /// <param name="virtualButton">A reference to the VirtualButton.</param>
        public void RegisterVirtualButton(string name, UnityVirtualButton virtualButton)
        {
            if (!m_UseMobileInput) {
                return;
            }
            (m_Input as UnityMobileInput).AddVirtualButton(name, virtualButton);
        }

        /// <summary>
        /// Unregister a VirtualButton. This happens when the VirtualButton is disabled.
        /// </summary>
        /// <param name="name">The name of the VirtualButton.</param>
        public void UnregisterVirtualButton(string name)
        {
            if (!m_UseMobileInput) {
                return;
            }
            (m_Input as UnityMobileInput).RemoveVirtualButton(name);
        }

        /// <summary>
        /// Register for any events that the handler should be aware of.
        /// </summary>
        private void Start()
        {
            EventHandler.RegisterEvent<bool>(gameObject, "OnAllowGameplayInput", AllowGameplayInput);
        }

        /// <summary>
        /// Unlock the cursor.
        /// </summary>
        private void OnDisable()
        {
            if (!m_UseMobileInput && m_DisableCursor) {
#if UNITY_4_6 || UNITY_4_7
                Screen.lockCursor = false;
#else
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
#endif
            }
        }

        /// <summary>
        /// Track the touches for use by the swipe for mobile input, otherwise keep the cursor disabled.
        /// </summary>
        private void LateUpdate()
        {
            if (!m_AllowGameplayInput) {
                return;
            }

            if (!m_UseMobileInput) {
                // The joystick is no longer down after the axis is 0.
                if (m_JoystickDownList != null) {
                    for (int i = m_JoystickDownList.Count - 1; i > -1; --i) {
                        if (m_Input.GetAxis(m_JoystickDownList[i]) <= 0.1f) {
                            m_JoystickDownSet.Remove(m_JoystickDownList[i]);
                            m_JoystickDownList.RemoveAt(i);
                        }
                    }
                }
                // GetButtonDown doesn't immediately add the button name to the set to prevent the GetButtonDown from returning false
                // if it is called twice within the same frame.
                if (m_ToAddJoystickDownList != null && m_ToAddJoystickDownList.Count > 0) {
                    if (m_JoystickDownList == null) {
                        m_JoystickDownList = new List<string>();
                    }
                    for (int i = 0; i < m_ToAddJoystickDownList.Count; ++i) {
                        m_JoystickDownSet.Add(m_ToAddJoystickDownList[i]);
                        m_JoystickDownList.Add(m_ToAddJoystickDownList[i]);
                    }
                    m_ToAddJoystickDownList.Clear();
                }

                if (m_DisableWithEscape && UnityEngine.Input.GetKeyDown(KeyCode.Escape)) {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                } else if (m_DisableCursor) {
#if !UNITY_EDITOR && !DLL_RELEASE
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
#endif
                } else if (!m_DisableCursor && m_DisableWhenButtonDown) {
                    var visible = !(GetButton(Constants.PrimaryDisableButtonName) || GetButton(Constants.SecondaryDisableButtonName));
                    Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
                    Cursor.visible = visible;
                }
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL || UNITY_WINRT
        /// <summary>
        /// Lock the cursor when the mouse is pressed down.
        /// </summary>
        private void OnMouseDown()
        {
            if (m_DisableCursor && m_AllowGameplayInput) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
#endif

        /// <summary>
        /// Returns true if the button is being pressed.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>True of the button is being pressed.</returns>
        public override bool GetButton(string name)
        {
            if (m_Input.GetButton(name, UnityInputBase.ButtonAction.GetButton)) {
                return true;
            }
            if (IsControllerConnected() && m_Input.GetAxis(name) == 1) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the button was pressed this frame.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>True if the button is pressed this frame.</returns>
        public override bool GetButtonDown(string name)
        {
            if (IsControllerConnected() && m_Input.GetAxis(name) == 1) {
                if (m_JoystickDownSet == null) {
                    m_JoystickDownSet = new HashSet<string>();
                }
                // The button should only be considered down on the first frame.
                if (m_JoystickDownSet.Contains(name)) {
                    return false;
                }
                if (m_ToAddJoystickDownList == null) {
                    m_ToAddJoystickDownList = new List<string>();
                }
                m_ToAddJoystickDownList.Add(name);
                return true;
            }
            if (m_Input.GetButton(name, UnityInputBase.ButtonAction.GetButtonDown)) {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Returns true if the button is up.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>True if the button is up.</returns>
        public override bool GetButtonUp(string name)
        {
            if (IsControllerConnected()) {
                var value = m_Input.GetAxis(name);
                if (m_JoystickDownSet == null) {
                    m_JoystickDownSet = new HashSet<string>();
                }
                if (m_JoystickDownSet.Contains(name) && value <= 0.1f) {
                    m_JoystickDownSet.Remove(name);
                    return true;
                }
                return false;
            }
            if (m_Input.GetButton(name, UnityInputBase.ButtonAction.GetButtonUp)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if a double press occurred (double click or double tap).
        /// </summary>
        /// <param name="name">The button name to check for a double press.</param>
        /// <returns>True if a double press occurred (double click or double tap).</returns>
        public override bool GetDoublePress(string name)
        {
            return m_Input.GetDoublePress(name);
        }

        /// <summary>
        /// Returns the value of the axis with the specified name.
        /// </summary>
        /// <param name="name">The name of the axis.</param>
        /// <returns>The value of the axis.</returns>
        public override float GetAxis(string name)
        {
            return m_Input.GetAxis(name);
        }

        /// <summary>
        /// Returns the value of the raw axis with the specified name.
        /// </summary>
        /// <param name="name">The name of the axis.</param>
        /// <returns>The value of the raw axis.</returns>
        public override float GetAxisRaw(string name)
        {
            return m_Input.GetAxisRaw(name);
        }

        /// <summary>
        /// Returns the position of the mouse.
        /// </summary>
        /// <returns>The mouse position.</returns>
        public override Vector2 GetMousePosition()
        {
            return m_Input.GetMousePosition();
        }

        /// <summary>
        /// Is gameplay input allowed? An example of when it will not be allowed is when there is a fullscreen UI over the main camera.
        /// </summary>
        /// <param name="allow">True if gameplay is allowed.</param>
        private void AllowGameplayInput(bool allow)
        {
            m_AllowGameplayInput = allow;
            if (m_DisableCursor) {
                Cursor.lockState = (allow ? CursorLockMode.Locked : CursorLockMode.None);
                Cursor.visible = !allow;
            }
        }

        /// <summary>
        /// The GameObject has been destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (m_Input is UnityMobileInput) {
                (m_Input as UnityMobileInput).ClearVirtualButtons();
            }
        }
    }
}