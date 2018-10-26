using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyDragonkin : NetworkBehaviour {

	public bool isRanged;

	private GameObject myPosition;					// Stores the key where they're located for number of enemy/position tracking.
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void SetMyPosition(GameObject g ) {
		myPosition = g;
	}

	public GameObject GetMyPosition() {
		return myPosition;
	}

	public void OnDeath() {
		if ( !isServer ) {
			return;
		}

		if ( isRanged ) {
			VariableHolder.instance.enemyRangedPositions[myPosition] = false;
		} else {
			VariableHolder.instance.enemyMeleePositions[myPosition] = false;
		}
	}
}
