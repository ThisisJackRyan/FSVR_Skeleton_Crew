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

	[Header( "Dragonkin Summoning" )]
	public GameObject[] rangedSpawnPositions;
	public GameObject[] meleeSpawnPositions;
	public GameObject dragonkinSpawnParticles;
	public float timeBetweenParticlesAndEnemySpawn;
	public GameObject meleeDragonkin, rangedDragonkin;

	private int numRanged = 1, numMelee = 1;

	[Header("Teleporting")]
	// publics
	public GameObject[] captainTeleportPositionsSafe;
	public GameObject[] captainTeleportPositionsDrain;
	public GameObject captainCurrentPositionTeleportParticles;
	public GameObject captainTargetPositionTeleportParticles;

	// privates
	private bool teleportingToSafety;

	[Header( "EnergyDraining" )]
	// publics
	public GameObject particlesToSpawnOnDragon;
	public GameObject energyTrail;
	public Transform skullTransform;
	public GameObject dragon;
	public float timeToCaptainWinningInSeconds;

	// privates
	private float curTime = 0f;
	private GameObject energyTrailInstance;
	private GameObject particlesOnDragonInstance;
	private bool isDraining, canDrain;

	[Header("Other")]
	public int hitsToDeath;




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

	#region Drain Testing

	[Button]
	public void StartDrainAbility() {
		StartCoroutine( "DrainEnergy" );
	}

	IEnumerator DrainEnergy() {
		string abName = "DrainEnergy";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility( ab );
		yield return new WaitForSecondsRealtime( 1000f );
		print( "have waitforseconds in coroutine" );
		GetComponent<ControllerHandler>().TryStopAbility( ab );
	}

	#endregion


	#region Drain Energy From Dragon

	public bool CanDrainFromDragon() {
		return canDrain;
	}

	public void DrainEnergyFromDragon() {
		StartCoroutine( "StartTheDrain" );
	}

	public IEnumerator StartTheDrain() {
		particlesOnDragonInstance = Instantiate( particlesToSpawnOnDragon, dragon.transform.position, Quaternion.identity );
		NetworkServer.Spawn( particlesOnDragonInstance );
		yield return new WaitForSecondsRealtime( 2.5f );
		energyTrailInstance = Instantiate( energyTrail, transform.position, Quaternion.identity );
		foreach ( var v in energyTrailInstance.GetComponentsInChildren<LineRenderer>() ) {
			v.SetPosition( 0, dragon.transform.position );
			v.SetPosition( 1, skullTransform.position );
		}
		NetworkServer.Spawn( energyTrailInstance );
		isDraining = true;
	}

	private void OnCollisionEnter( Collision collision ) {
		if ( !isServer ) {
			return;
		}

		if(collision.transform.tag == "CannonBallPlayer") {
			if ( isDraining ) {
				StopDrainingAbility();
				NetworkServer.Destroy( energyTrailInstance );
				NetworkServer.Destroy( particlesOnDragonInstance );

				isDraining = false;
			}
		}

	}

	private void Update() {
		if ( isDraining ) {
			curTime += Time.deltaTime;

			if(curTime >= timeToCaptainWinningInSeconds ) {
				Debug.LogWarning( "Players have lost the game. Put in shit when that happens here" );
			}
		}
	}

	#endregion

	private void StopDrainingAbility() {
		string abName = "DrainEnergy";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStopAbility( ab );
	}


	#region Teleport Testing

	[Button]
	public void TeleportingToSafety() {
		teleportingToSafety = true;
	}

	[Button]
	public void TeleportingToDrain() {
		teleportingToSafety = false;
	}

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

	#region Teleporting

	// toSafe: 1 teleports to safe target, otherwise it teleports to a drain target
	public void TeleportCaptain() {
		GameObject tpCurPos = Instantiate( captainCurrentPositionTeleportParticles, transform.position, Quaternion.identity );
		tpCurPos.transform.position = new Vector3(tpCurPos.transform.position.x, tpCurPos.transform.position.y + 1.5f, tpCurPos.transform.position.z);
		NetworkServer.Spawn( tpCurPos );
		GameObject tpTarget;
		if (teleportingToSafety) {
			tpTarget = Instantiate( captainTargetPositionTeleportParticles, captainTeleportPositionsSafe[Random.Range( 0, captainTeleportPositionsSafe.Length )].transform.position, Quaternion.identity );
		} else {
			tpTarget = Instantiate( captainTargetPositionTeleportParticles, captainTeleportPositionsDrain[Random.Range( 0, captainTeleportPositionsDrain.Length )].transform.position, Quaternion.identity );
		}
		NetworkServer.Spawn( tpTarget );
		canDrain = !teleportingToSafety;
		transform.position = tpTarget.transform.position;		
	}

	#endregion


	#region Dragonkin Summoning Testing
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

	#region Dragonkin Summoning
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
