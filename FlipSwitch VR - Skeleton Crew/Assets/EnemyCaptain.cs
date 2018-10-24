﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using Opsive.ThirdPersonController.Abilities;
using Opsive.ThirdPersonController;
using BehaviorDesigner.Runtime;

public class EnemyCaptain : NetworkBehaviour {

	public static EnemyCaptain instance;


	public GameObject dragonkinSpawnParticles;
	public float timeBetweenParticlesAndEnemySpawn;
	public GameObject meleeDragonkin, rangedDragonkin;

	public GameObject[] captainTeleportPositions;
	public GameObject captainCurrentPositionTeleportParticles;
	public GameObject captainTargetPositionTeleportParticles;
	public GameObject[] rangedSpawnPositions;
	public GameObject[] meleeSpawnPositions;


	public int hitsToDeath;

	public bool isDraining;
	int numRanged = 1, numMelee = 1;

	// Use this for initialization
	void Start () {
		if ( !isServer ) {
			return;
		}

		VariableHolder.instance.enemyRangedPositions = new Dictionary<GameObject, bool>();
		VariableHolder.instance.enemyMeleePositions = new Dictionary<GameObject, bool>();
		

		VariableHolder.instance.enemyRangedPositions.Clear();
		VariableHolder.instance.enemyMeleePositions.Clear();

		foreach(var r in rangedSpawnPositions ) {
			VariableHolder.instance.enemyRangedPositions.Add( r, false );
		}

		foreach(var m in meleeSpawnPositions ) {
			VariableHolder.instance.enemyMeleePositions.Add( m, false );
		}

		if ( instance != null )
			Destroy( gameObject );
		else
			instance = this;
	}

	#region Teleport Testing Stuff
	[Button]
	public void StartTeleportAbility() {
		StartCoroutine( "CaptainTeleport" );
	}

	IEnumerator CaptainTeleport() {
		string abName = "CaptainTeleport";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility( ab );
		yield return new WaitForSecondsRealtime( 2.5f );
		GetComponent<ControllerHandler>().TryStopAbility( ab );
	}
	#endregion

	#region Teleport Stuff

	public void TeleportCaptain() {
		GameObject tpCurPos = Instantiate( captainCurrentPositionTeleportParticles, transform.position, Quaternion.identity );
		tpCurPos.transform.position = new Vector3(tpCurPos.transform.position.x, tpCurPos.transform.position.y + 1.5f, tpCurPos.transform.position.z);
		NetworkServer.Spawn( tpCurPos );
		GameObject tpTarget = Instantiate( captainTargetPositionTeleportParticles, captainTeleportPositions[Random.Range( 0, captainTeleportPositions.Length )].transform.position, Quaternion.identity );
		NetworkServer.Spawn( tpTarget );

		transform.position = tpTarget.transform.position;		
	}

	#endregion


	#region Dragonkin Testing Stuff
	[Button]
	public void StartSpawnAbility() {
		StartCoroutine( "DragonkinSummon" );
	}

	IEnumerator DragonkinSummon() {
		string abName = "SummonDragonkin";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility( ab );
		yield return new WaitForSecondsRealtime( 2.5f );
		GetComponent<ControllerHandler>().TryStopAbility( ab );
	}

	#endregion

	#region Dragonkin Stuff
	public void DragonkinDeath(bool ranged) {
		if ( ranged ) {
			numRanged--;
		} else {
			numMelee--;
		}
	}

	public void SpawnDragonkin() {

		if ( !isServer) {
			return;
		}

		if(numRanged % 3 != 0 && VariableHolder.instance.enemyRangedPositions.ContainsValue(false)) {
			foreach (GameObject key in VariableHolder.instance.enemyRangedPositions.Keys) {
				if(VariableHolder.instance.enemyRangedPositions[key] == false ) {
					GameObject p = Instantiate( dragonkinSpawnParticles, key.transform.position, key.transform.rotation );
					NetworkServer.Spawn( p );
					VariableHolder.instance.enemyRangedPositions[key] = true;
					StartCoroutine(GenerateEnemy(key, true, timeBetweenParticlesAndEnemySpawn));
					break;
				}
			}
		} else if (VariableHolder.instance.enemyMeleePositions.ContainsValue(false)) {
			foreach ( GameObject key in VariableHolder.instance.enemyMeleePositions.Keys ) {
				if ( VariableHolder.instance.enemyMeleePositions[key] == false ) {
					GameObject p = Instantiate( dragonkinSpawnParticles, key.transform.position, key.transform.rotation );
					NetworkServer.Spawn( p );
					VariableHolder.instance.enemyMeleePositions[key] = true;
					StartCoroutine( GenerateEnemy( key, false, timeBetweenParticlesAndEnemySpawn ) );
					break;
				}
			}
		} else {
			Debug.LogWarning( "Tried to spawn enemy with no positions available. Should never get here" );
		}

	}

	IEnumerator GenerateEnemy(GameObject key, bool ranged, float delayTime ) {
		yield return new WaitForSeconds( delayTime );
		if ( ranged ) {
			numRanged++;
			GameObject g = Instantiate( rangedDragonkin, key.transform.position, key.transform.rotation );
			g.GetComponent<EnemyDragonkin>().SetMyPosition( key );
			NetworkServer.Spawn( g );
		} else {
			numMelee++;
			GameObject g = Instantiate( meleeDragonkin, key.transform.position, key.transform.rotation );
			g.GetComponent<EnemyDragonkin>().SetMyPosition( key );
			NetworkServer.Spawn( g );
		}
	}

	#endregion
}
