using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptGameSetup : MonoBehaviour {

	// Use this for initialization
	void Start () {
		ScriptClientSocket.SetupSockets();
	}
}
