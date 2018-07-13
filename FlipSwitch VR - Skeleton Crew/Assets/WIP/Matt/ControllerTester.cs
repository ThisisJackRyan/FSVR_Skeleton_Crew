using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerTester : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Controller.RightController.GetPressDown(Controller.Trigger) ){
            print("right trigger");
        }

        if (Controller.LeftController.GetPressDown(Controller.Trigger)) {
            print("left trigger");
        }
    }
}
