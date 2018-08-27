using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropGameSetup : MonoBehaviour {

	// Use this for initialization
	void Start () {
		PropClientSocket.SetupSockets();
	}
}
