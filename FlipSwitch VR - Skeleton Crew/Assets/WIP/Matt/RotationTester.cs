using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTester : MonoBehaviour {

    public Transform toFollow;
    //Quaternion holdingRot;
	// Update is called once per frame
	public enum RotationAxis {
		AxisX, AxisY,AxisZ
	}

	public RotationAxis rotationAxis;

	void Update () {
		if (toFollow) {
			Vector3 facing = toFollow.position - transform.position;

			switch (rotationAxis) {
				case RotationAxis.AxisX:
					facing.x = 0;

					break;
				case RotationAxis.AxisY:
					facing.y = 0;

					break;
				case RotationAxis.AxisZ:
					facing.z = 0;

					break;
			}


			transform.rotation = Quaternion.LookRotation(facing, new Vector3(0, -1, 0) );
		}
	}
}
