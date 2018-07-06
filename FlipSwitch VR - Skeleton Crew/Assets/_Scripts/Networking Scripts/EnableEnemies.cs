using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnableEnemies : NetworkBehaviour {

	[SyncVar (hook="EnemyParentChange")] public GameObject enemyParent;

	void EnemyParentChange (GameObject g ) {
		enemyParent = g;
	}

	// Use this for initialization
	void Start () {
		if (isLocalPlayer) {
			Debug.Break();
			enemyParent = GameObject.Find( "EnemyParent" );
			if(enemyParent != null)
				enemyParent.SetActive( false );
			return;
		}else if(enemyParent == null && !isLocalPlayer ) {
			enemyParent = GameObject.Find( "EnemyParent" );
			enemyParent.SetActive( false );
		} else {
			enemyParent.SetActive( false );
		}

	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.Space)) {
			print( "space key pressed" );
			if ( !enemyParent.activeSelf )
				enemyParent.SetActive( true );
		}
	}
}
