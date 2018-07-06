//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class CannonPunt : Weapon {

//	public Transform fire;

//	// Use this for initialization
//	void Start () {
//		fire.gameObject.SetActive( false );
//	}
	
//	// Update is called once per frame
//	void Update () {
//		if ( !isLocal ) {
//			return;
//		} else if ( controller == null ) {
//			return;
//		}
		
//		if ( controller.GetPressDown( Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger ) ) {
//			if ( fire.gameObject.activeInHierarchy ) {

//				fire.gameObject.SetActive( false );
//			} else {
//				fire.gameObject.SetActive( true );

//			}
//		}
//	}
//}
