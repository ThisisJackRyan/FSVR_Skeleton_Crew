﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairTrigger : MonoBehaviour {

	public DamagedObject dmgObj;
	[HideInInspector]
	public RepairPattern repairPattern;
	Transform activator;

	float timer = 0;
	bool active = false;

	private void OnTriggerStay( Collider other ) {
		if (other.transform.root == activator && active) {
			timer += Time.deltaTime;

			if (timer >= 1) {
				repairPattern.gameObject.SetActive( true );//
				repairPattern.Init();//

				GetComponent<Renderer>().enabled = false;
				transform.position = new Vector3( transform.position.x, other.transform.root.GetComponentInChildren<HipMarker>().transform.position.y, transform.position.z );
				dmgObj.EnablePatternOnClients();

				active = false;
				//repairPattern = null;
				activator = null;
			}
		}
	}

	private void OnTriggerEnter( Collider other ) {
		if ( other.gameObject.GetComponentInParent<MastInteraction>() ) {//player check
			if (repairPattern != null && repairPattern.gameObject.activeInHierarchy) {//pattern is active
				return;
			}

			print( "returning because active in hieracrhy is false:" + ( repairPattern != null ) );

			timer = 0;
			active = true;
			GetComponent<Renderer>().enabled = true;

			repairPattern = dmgObj.SelectPattern();			

			activator = other.transform.root;

			//repairPattern.gameObject.SetActive( false );//
		}
	}

	private void OnTriggerExit( Collider other ) {
		if (other.transform.root != activator ) {
			return;
		}

		active = false;
		//repairPattern = null;
		//activator = null;
	}

	private void OnDisable() {
		print("repoaIR NODE DISABLED");
	}



}
