using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FSVR_AssignController : MonoBehaviour {

    SteamVR_Controller.Device device;
    public Text canvasText;
	bool hasTried = false;

	void Start () {
        device = Controller.GetById((int)GetComponent<SteamVR_TrackedObject>().index);
        //canvasText = GetComponentInChildren<Text>();
        //Invoke("DestroyIfNotRendering", 0.5f);
    }
	
	
	void Update () {
		if (hasTried) {
			return;
		}

        if (device != null){
            //print(device + " id is " + device.index);

            if (device.GetPressDown(Controller.Trigger)) {
                //print("hit trigger on device i:" + device.index);
                canvasText.transform.parent.gameObject.SetActive(true);
                canvasText.text = Controller.InitControllers(device.index);

				hasTried = true;

                if (Controller.initialized) {
                    FindObjectOfType<ConnectWithPress>().EnableInput();
                }
            }
        }
	}

    void DestroyIfNotRendering() {
        if (transform.childCount < 2) {
            Destroy(gameObject);
        }
    }
}
