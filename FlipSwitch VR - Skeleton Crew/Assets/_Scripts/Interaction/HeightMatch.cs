//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class HeightMatch : MonoBehaviour {

//	public Transform toMatch;
//	public MastInteraction mastInteraction;
//	float startingHeight;
//	bool canActivate = true;

//	void Start() {
//		startingHeight = transform.position.y;
//	}

//	// Update is called once per frame
//	void Update () {
//		Vector3 newPos = toMatch.position;
//		newPos.x = transform.position.x;
//		newPos.z = transform.position.z;
//		transform.position = newPos;

//		if (Mathf.Abs( startingHeight-transform.position.y ) >= 0.5f) {
//			print("traveled full length");

//			if (canActivate) {
//				//mastInteraction.CmdReachedTarget();
//				canActivate = false;
//			}

//		}
//	}

//	//private void OnTriggerEnter( Collider other ) {
//	//	print( "trigger entered on " + name + " triggered by " + other.transform.name + " with tag " + other.transform.tag);
//	//	if (other.tag == "ReleasePoint" ) {
//	//		print( "hand target triggerd the target point" );
//	//		mastInteraction.CmdReachedTarget();
//	//	}
//	//}


//}
