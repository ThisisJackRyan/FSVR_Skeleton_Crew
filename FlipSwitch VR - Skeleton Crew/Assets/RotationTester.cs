using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTester : MonoBehaviour {

    public Transform toFollow;
    Quaternion holdingRot;
	// Update is called once per frame
	void Update () {
        if (toFollow) {

            Vector3 facing = toFollow.position - transform.position;
            facing.x = 0;
            transform.rotation = (holdingRot * Quaternion.LookRotation(facing, new Vector3(0, -1, 0)) );

        } else {
            print("updating holding rot");
            holdingRot = transform.rotation;
        }
	}
}
