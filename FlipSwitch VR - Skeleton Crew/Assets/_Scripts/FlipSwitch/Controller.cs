using UnityEngine;
using HTC.UnityPlugin.Vive;

//[RequireComponent(typeof(LineRenderer))]
public static class Controller {

	static uint leftIndex, rightIndex;
    
	public static SteamVR_Controller.Device RightController {

		get { return SteamVR_Controller.Input((int)rightIndex); }

	}

	public static SteamVR_Controller.Device LeftController {

		get { return SteamVR_Controller.Input((int)leftIndex); }

	}

	static bool initialized = false;

	public static void InitControllers(uint leftId, uint rightId) {
        //Debug.Log("controller init called with ids of " + leftId + " and " + rightId);
        //Debug.Log(SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.FarthestLeft) + " is furthest left" );
		if (!initialized) {
			initialized = true;
            rightIndex = rightId;
			leftIndex = leftId;
		}
	}

	public static Valve.VR.EVRButtonId TouchPad = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
	public static Valve.VR.EVRButtonId Trigger = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
	public static Valve.VR.EVRButtonId Grip = Valve.VR.EVRButtonId.k_EButton_Grip;
}

