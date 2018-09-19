using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class RangedTeleportTarget : NetworkBehaviour {

	// Use this for initialization
	void Start () {
        if (isServer) {
            VariableHolder.instance.enemyRangedPositions.Add(gameObject, false);
            print("added " + name + " to range tele target dictionary");
        }
	}
}
