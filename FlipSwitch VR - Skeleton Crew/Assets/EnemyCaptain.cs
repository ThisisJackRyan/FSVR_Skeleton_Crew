using System.Collections;
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
	public GameObject captainTeleportParticles;
	public GameObject[] rangedSpawnPositions;
	public GameObject[] meleeSpawnPositions;


	public int hitsToDeath;

	public bool isDraining;
	int numRanged, numMelee;

	// Use this for initialization
	void Start () {
		print( "start called on captain pre server" );
		if ( !isServer ) {
			return;
		}

		print( "start called on captain post server" );

		VariableHolder.instance.enemyRangedPositions.Clear();
		VariableHolder.instance.enemyMeleePositions.Clear();

		foreach(var r in rangedSpawnPositions ) {
			print( "adding " + r.name + " to the ranged dictionary" );
			VariableHolder.instance.enemyRangedPositions.Add( r, false );
		}

		foreach(var m in meleeSpawnPositions ) {
			print( "adding " + m.name + " to the melee dictionary" );
			VariableHolder.instance.enemyMeleePositions.Add( m, false );
		}

		if ( instance != null )
			Destroy( gameObject );
		else
			instance = this;
	}

	[Button]
	public void StartSpawnAbility() {
		StartCoroutine( "Blah" );
	}

	IEnumerator Blah() {
		string abName = "SummonDragonkin";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility( ab );
		yield return new WaitForSecondsRealtime( 2f );
		GetComponent<ControllerHandler>().TryStopAbility( ab );
	}

	public void DragonkinDeath(bool ranged) {
		if ( ranged ) {
			numRanged--;
		} else {
			numMelee--;
		}
	}

	public void SpawnDragonkin() {
		if ( !isServer ) {
			return;
		}
		print( "number of ranged pre spawn: " + numRanged );
		print( "number of melee pre spawn: " + numMelee );
		print( "dragonkin spawned" );

		if(numRanged % 3 != 0 && VariableHolder.instance.enemyRangedPositions.ContainsValue(false)) {
			foreach (GameObject key in VariableHolder.instance.enemyRangedPositions.Keys) {
				if(VariableHolder.instance.enemyRangedPositions[key] == false ) {
					GameObject p = Instantiate( dragonkinSpawnParticles, key.transform.position, key.transform.rotation );
					NetworkServer.Spawn( p );
					VariableHolder.instance.enemyRangedPositions[key] = true;
					StartCoroutine(GenerateEnemy(key, true, timeBetweenParticlesAndEnemySpawn));
				}
			}
		} else if (VariableHolder.instance.enemyMeleePositions.ContainsValue(false)) {
			foreach ( GameObject key in VariableHolder.instance.enemyMeleePositions.Keys ) {
				if ( VariableHolder.instance.enemyMeleePositions[key] == false ) {
					GameObject p = Instantiate( dragonkinSpawnParticles, key.transform.position, key.transform.rotation );
					NetworkServer.Spawn( p );
					VariableHolder.instance.enemyMeleePositions[key] = true;
					StartCoroutine( GenerateEnemy( key, false, timeBetweenParticlesAndEnemySpawn ) );
				}
			}
		} else {
			Debug.LogWarning( "Tried to spawn enemy with no positions available. Should never get here" );
		}

		print( "number of ranged post spawn: " + numRanged );
		print( "number of melee post spawn: " + numMelee );

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
}
