using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HTC.UnityPlugin.Vive;

//[RequireComponent(typeof(LineRenderer))]
public static class Controller {

	static int leftIndex = -1, rightIndex = -1;
	
	public static SteamVR_Controller.Device RightController {

		get { return SteamVR_Controller.Input((int)rightIndex); }

	}

	public static SteamVR_Controller.Device LeftController {

		get { return SteamVR_Controller.Input((int)leftIndex); }

	}

    public static SteamVR_Controller.Device GetById(int id) {
        return SteamVR_Controller.Input(id);
    }

    public static void PlayHaptics (bool leftController, HapticEvent haptics) {

		SteamVR_Controller.Device con = (leftController) ? LeftController : RightController;

		if (con != null) {
			if (HapticController.instance) {
				if (haptics.eventType == HapticEvent.HapticEventType.Pulse) {
					HapticController.instance.StartHapticVibrationPulse(con, haptics.vibrationCount, haptics.vibrationLength, haptics.gapLength, haptics.strength);
				} else if(haptics.eventType == HapticEvent.HapticEventType.Standard) {
					HapticController.instance.StartHapticVibration(con,haptics.vibrationLength, haptics.strength);
				}
			} else {
				Debug.LogError("There is no hapticController in the scene when haptics were called.");
			}	
		}
	}	

	public static bool initialized {
         get; private set;
    }

	public static string InitControllers(uint leftId) {
        ////Debug.Log("controller init called with ids of " + leftId + " and " + rightId);
        ////Debug.Log(SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.FarthestLeft) + " is furthest left" );
        //if (!initialized) {
        //	initialized = true;
        //	rightIndex = rightId;
        //	leftIndex = leftId;
        //}

        string toReturn = "";

        if (!initialized) {

            if (leftIndex == -1) {
                //Debug.Log("assigning left controller to " + leftId);
                leftIndex = (int)leftId;
                leftInit = true;
                toReturn= "Left";
            } else if (rightIndex == -1) {
                //Debug.Log("assigning right controller to " + leftId);
                rightIndex = (int)leftId;
              rightInit = true;
                toReturn= "Right";
            } else {
                Debug.LogWarning("received assignment when both controllers have been assigned. call from device " + leftId);
                toReturn = "Broken";
            }

            if (leftInit && rightInit) {
                initialized = true;
            }

        } else {
            toReturn = "Initialized";
        }
        
        return toReturn;
    }

    static bool leftInit, rightInit;

	public static Valve.VR.EVRButtonId TrackPad = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
	public static Valve.VR.EVRButtonId Trigger = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
	public static Valve.VR.EVRButtonId Grip = Valve.VR.EVRButtonId.k_EButton_Grip;
	//public static Valve.VR.EVRButtonId Grip = Valve.VR.EVRButtonId.k_EButton_Grip;


}

