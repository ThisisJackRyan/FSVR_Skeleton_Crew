using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(LineRenderer))]
public class Controller : MonoBehaviour {

	public ControllerSettings settings;
	public GameObject cursor;
	public bool updatePointer = true, gizmos = true;

	//[Tooltip("OVR Controller type")]
	//public OVRInput.Controller controller;
	//[Tooltip("Controller Type")]
	//public ControllerType type;
	//[Tooltip("Update via oculus api")]
	//public bool updateController = true;

	public int controllerID;
	public bool isRightHand;

	static int leftIndex, rightIndex;
	protected SteamVR_Controller.Device controller {
		get
		{
			if ( isRightHand ) {
				return RightController;
			} else {
				return LeftController;
			}
		}
	}

	public static SteamVR_Controller.Device RightController {

		get { return SteamVR_Controller.Input(rightIndex); }

	}

	public static SteamVR_Controller.Device LeftController {

		get { return SteamVR_Controller.Input(leftIndex); }

	}

	#region Privates

	internal LineRenderer lr;
	internal Vector3[] pointer = new Vector3[2];
	internal GameObject selected;

	#endregion

	//todo make this better
	private void Awake() {
		InitControllers();
	}

	static bool initialized = false;

	public static void InitControllers() {
		if (!initialized) {
			initialized = true;
			rightIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);
			leftIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
		}
	}

	// Use this for initialization
	internal virtual void Start() {
		lr = GetComponent<LineRenderer>();
		lr.materials = new Material[] {settings.beam};
		lr.material.color = settings.inactiveColor;
		lr.widthMultiplier = settings.lineWeight;

		if (!cursor && updatePointer) {
			Debug.LogWarning(name + " has no cursur, turning off update pointer");
			updatePointer = false;
		}
	}

	// Update is called once per frame
	internal virtual void Update() {

	}

	internal virtual void FixedUpdate() {

	}

	//todo prolly be moved in to the child classes
	internal virtual void Interact() { }

	internal void UpdateController() {

	}

	void OnDrawGizmos() {
		if (gizmos) {
			Gizmos.DrawRay(transform.position, transform.forward * settings.range);
		}
	}

	public enum ControllerType {
		MultiTool,
		Hand,
		Flashlight
	}


	public static Valve.VR.EVRButtonId TouchPad = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
	public static Valve.VR.EVRButtonId Trigger = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
	public static Valve.VR.EVRButtonId Grip = Valve.VR.EVRButtonId.k_EButton_Grip;
}

