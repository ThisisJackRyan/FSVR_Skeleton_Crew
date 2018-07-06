using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HoldSwitch : SimpleSwitch {

	//OVRInput.Controller con;

	internal int charge = 0;
	public int chargingRate = 1;

	public override void OnActivate() {
		//print("activated " + name);
		if (activator.GetComponent<MastInteraction>()) {
			//con = activator.GetComponent<SwitchActivator>().controller;
			charge = 0;
		}
	}

	public override void OnDeactivate() {
		//con = OVRInput.Controller.None;
		charge = 0;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	internal virtual void FixedUpdate () {
		if (isActive) {
			//print("is active");
			//if (OVRInput.Get(OVRInput.Button.One, con)||OVRInput.Get( OVRInput.Button.Two, con )) {
			//	charge = chargingRate;
			//} else {
			//	charge = 0;
			//	CmdToggle( activator );

			//}

			//if  ( OVRInput.GetUp( OVRInput.Button.One, con ) || OVRInput.GetUp( OVRInput.Button.Two, con )  ) {
			//	charge = 0;
			//	CmdToggle(activator);
			//}			
		}

	}
}
