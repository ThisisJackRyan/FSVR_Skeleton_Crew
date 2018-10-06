using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FSVR_AssignController : MonoBehaviour {

    SteamVR_Controller.Device device;
    public Text canvasText;

	void Start () {
        device = Controller.GetById((int)GetComponent<SteamVR_TrackedObject>().index);
        //canvasText = GetComponentInChildren<Text>();
    }
	
	
	void Update () {
        if (device != null){
            //print(device + " id is " + device.index);

            if (device.GetPressDown(Controller.Trigger)) {
                //print("hit trigger on device i:" + device.index);
                canvasText.transform.parent.gameObject.SetActive(true);
                canvasText.text = Controller.InitControllers(device.index);

                if (Controller.initialized) {
                    FindObjectOfType<ConnectWithPress>().canInput = true;
                }
            }


        }
	}
}
