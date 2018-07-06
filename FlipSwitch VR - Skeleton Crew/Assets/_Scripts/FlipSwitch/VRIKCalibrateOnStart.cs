using System.Collections;
using System.Collections.Generic;
using RootMotion.Demos;
using UnityEngine;
using UnityEngine.Networking;

public class VRIKCalibrateOnStart : NetworkBehaviour {

	 public VRIKCalibrationController mine;
	//public OVRInput.Controller controller;


	public bool calibrated = false;
	// Use this for initialization
	public void CalibratePlayer() {
		print("calibrate on start local: " + isLocalPlayer + " for " + name);
		//if ( isLocalPlayer ) {
		if (!calibrated) {
			StartCoroutine( "CalibrateLocally" );
			print( "should be calibrating" );
		}

		//}
	}
	
	// Update is called once per frame
	IEnumerator Calibrate()
    {
		//yield return new WaitForSecondsRealtime(0.5f);
		calibrated = true;
        yield return new WaitForSeconds(1);
        CmdCalibrate();
    }

    IEnumerator CalibrateLocally()
    {
		calibrated = true;
        yield return new WaitForSecondsRealtime(1);
        mine.Calibrate();
        print("calibrated locally");
    }

    [Command]
    void CmdCalibrate()
    {
        RpcCalibrate();
    }

    [ClientRpc]
    void RpcCalibrate()
    {
        mine.Calibrate();
        print("Calibrated via rpc");
    }
}