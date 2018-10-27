using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Camera))]
public class GhostFreeRoamCamera : NetworkBehaviour {
    public float initialSpeed = 10f;
    public float increaseSpeed = 1.25f;

    public bool allowMovement = true;
    public bool allowRotation = true;
    public GameObject player;
    public Transform childTransform;

    public KeyCode isVisible = KeyCode.Alpha1;

    [Header("Movement")]
    public KeyCode forwardButton = KeyCode.W;
    public KeyCode backwardButton = KeyCode.S;
    public KeyCode rightButton = KeyCode.D;
    public KeyCode leftButton = KeyCode.A;
    public KeyCode upButton = KeyCode.E;
    public KeyCode downButton = KeyCode.Q;

    [Header("Cursor")]
    public float cursorSensitivity = 0.025f;
    public bool cursorToggleAllowed = true;
    public KeyCode cursorToggleButton = KeyCode.Escape;

    private float currentSpeed = 0f;
    private bool moving = false;
    private bool togglePressed = false;

    private MeshRenderer thisRenderer;
	public GameObject gimbleCamera;

    private void Start() {
        thisRenderer = GetComponent<MeshRenderer>();
        thisRenderer.enabled = false;

		if (FindObjectOfType<CameraPathFollower>()) {

			gimbleCamera = FindObjectOfType<CameraPathFollower>().gameObject;
		}

    }

    private void Update() {
        if (!isLocalPlayer)
            return;

        if (Input.GetKeyDown(KeyCode.Space)) {
            var g = Instantiate(player, Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(g);
        }

		if (gimbleCamera && Input.GetKeyDown(KeyCode.Escape)) {
			foreach ( var c in GetComponentsInChildren<Camera>() ) {
				c.enabled = false;
			}
			gimbleCamera.SetActive( true );
			gimbleCamera.GetComponent<CameraPathFollower>().canMove = true;
		}

        if (allowMovement) {
            bool lastMoving = moving;
            Vector3 deltaPosition = Vector3.zero;

            if (moving)
                currentSpeed += increaseSpeed * Time.deltaTime;

            moving = false;

            if (Input.GetKeyDown(isVisible)) {
                thisRenderer.enabled = !thisRenderer.enabled;
            }

            CheckMove(forwardButton, ref deltaPosition, transform.forward);
            CheckMove(backwardButton, ref deltaPosition, -transform.forward);
            CheckMove(rightButton, ref deltaPosition, transform.right);
            CheckMove(leftButton, ref deltaPosition, -transform.right);
            CheckMove(upButton, ref deltaPosition, transform.up);
            CheckMove(downButton, ref deltaPosition, -transform.up);

            if (moving) {
                if (moving != lastMoving)
                    currentSpeed = initialSpeed;

                transform.position += deltaPosition * currentSpeed * Time.deltaTime;
            } else currentSpeed = 0f;
        }

        if (allowRotation) {
            if (Input.GetMouseButton(1)) {
                Vector3 eulerAngles = transform.eulerAngles;
                eulerAngles.x += -Input.GetAxis("Mouse Y") * 359f * cursorSensitivity;
                eulerAngles.y += Input.GetAxis("Mouse X") * 359f * cursorSensitivity;
                transform.eulerAngles = eulerAngles;
            }
        }

        if (cursorToggleAllowed) {
            if (Input.GetKey(cursorToggleButton)) {
                if (!togglePressed) {
                    togglePressed = true;
                    switch (Cursor.lockState) {
                        case CursorLockMode.Locked:
                            Cursor.lockState = CursorLockMode.None;
                            break;
                        case CursorLockMode.None:
                            Cursor.lockState = CursorLockMode.Locked;
                            break;
                    }

                    Cursor.visible = !Cursor.visible;
                }
            } else togglePressed = false;
        } else {
            togglePressed = false;
            Cursor.visible = false;
        }
    }

    private void CheckMove(KeyCode keyCode, ref Vector3 deltaPosition, Vector3 directionVector) {
        if (Input.GetKey(keyCode)) {
            moving = true;
            deltaPosition += directionVector;
        }
    }
}
