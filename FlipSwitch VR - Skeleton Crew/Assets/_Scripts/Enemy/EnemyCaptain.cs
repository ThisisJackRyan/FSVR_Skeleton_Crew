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
	// publics
	public GameObject[] rangedSpawnPositions;
	public GameObject[] meleeSpawnPositions;
	public GameObject dragonkinSpawnParticles;
	public float timeBetweenParticlesAndEnemySpawn;
	public GameObject meleeDragonkin, rangedDragonkin;

	//privates
	private int numRanged = 1, numMelee = 1;

	[Header("Meteor Summoning")]
	// publics
	public GameObject[] meteorsToEnable;

	// privates
	private int curActiveIndex = -1;

	[Header("Teleporting")]
	// publics
	public GameObject[] captainTeleportPositionsSafe;
	public GameObject[] captainTeleportPositionsDrain;
	public GameObject captainCurrentPositionTeleportParticles;
	public GameObject captainTargetPositionTeleportParticles;

	// privates
	private bool teleportingToSafety;
	private int currentIndexForSafety;
	private int currentIndexForDraining;

	[Header( "EnergyDraining" )]
	// publics
	public GameObject particlesToSpawnOnDragon;
	public GameObject energyTrail;
	public Transform skullTransform;
	public GameObject skullParticles;
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
		if (!isServer) {
			return;
		}

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

		yield return new WaitForSecondsRealtime( 2.5f );

		energyTrailInstance = Instantiate( energyTrail, transform.position, Quaternion.identity );

		foreach ( var v in energyTrailInstance.GetComponentsInChildren<LineRenderer>() ) {
			v.SetPosition( 0, dragon.transform.position );
			v.SetPosition( 1, skullTransform.position );
		}
		skullParticles.SetActive( true );
		isDraining = true;
	}

	private void OnCollisionEnter( Collision collision ) {
		if ( !isServer ) {
			return;
		}

		if(collision.transform.tag == "CannonBallPlayer") {
			if ( isDraining ) {
				StopDrainingAbility();
				Destroy( energyTrailInstance );
				Destroy( particlesOnDragonInstance );
				skullParticles.SetActive( false );
				RpcDestroyDrain();
				isDraining = false;
			}
		}

	}

	[ClientRpc]
	private void RpcDestroyDrain() {
		if ( isServer ) {
			return;
		}

		skullParticles.SetActive( false );
		Destroy( energyTrailInstance );
		Destroy( particlesOnDragonInstance );
	}

	private void Update() {
		if ( !isServer ) {
			return;
		}

		if ( isDraining ) {
			curTime += Time.deltaTime;
			print( "is draining. Current Elapsed Time: " + curTime );
			if(curTime >= timeToCaptainWinningInSeconds ) {
				Debug.LogWarning( "Players have lost the game. Put in shit when that happens here" );
			}
		}
	}



	private void StopDrainingAbility() {
		string abName = "DrainEnergy";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStopAbility( ab );
	}

	#endregion

	#region Teleport Testing

	[Button]
	public void TeleportingToSafety() {
		if (!isServer) {
			return;
		}

		teleportingToSafety = true;
	}

	[Button]
	public void TeleportingToDrain() {
		if (!isServer) {
			return;
		}

		teleportingToSafety = false;
	}

	[Button]
	public void StartTeleportAbility() {
		if (!isServer) {
			return;
		}

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
		if (!isServer) {
			return;
		}

		GameObject tpCurPos = Instantiate( captainCurrentPositionTeleportParticles, transform.position, Quaternion.identity );
		tpCurPos.transform.position = new Vector3(tpCurPos.transform.position.x, tpCurPos.transform.position.y + 1.5f, tpCurPos.transform.position.z);
		NetworkServer.Spawn( tpCurPos );
		GameObject tpTarget;
		int temp;

		if (teleportingToSafety) {
			do {
				temp = Random.Range(0, captainTeleportPositionsSafe.Length);
			} while (temp == currentIndexForSafety);

			currentIndexForSafety = temp;
			tpTarget = Instantiate(captainTargetPositionTeleportParticles, captainTeleportPositionsSafe[currentIndexForSafety].transform.position, Quaternion.identity);
		} else {
			do {
				temp = Random.Range(0, captainTeleportPositionsSafe.Length);
			} while (temp == currentIndexForDraining);

			currentIndexForDraining = temp;
			tpTarget = Instantiate( captainTargetPositionTeleportParticles, captainTeleportPositionsDrain[currentIndexForDraining].transform.position, Quaternion.identity );
		}

		NetworkServer.Spawn( tpTarget );
		canDrain = !teleportingToSafety;
		transform.position = tpTarget.transform.position;		
	}

	#endregion

	#region Meteor Summoning Testing

	[Button]
	public void StartSummonMeteorAbility() {
		if (!isServer) {
			return;
		}

		StartCoroutine("MeteorSummon");
	}

	IEnumerator MeteorSummon() {
		string abName = "SummonMeteor";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents(TaskUtility.GetTypeWithinAssembly(abName));

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility(ab);
		yield return new WaitForSecondsRealtime(2.5f);
		GetComponent<ControllerHandler>().TryStopAbility(ab);
	}

	#endregion

	#region Meteor Summoning

	public void SpawnMeteor() {

		if (!isServer) {
			return;
		}

		curActiveIndex = Random.Range(0, meteorsToEnable.Length);
		RpcEnableMeteor(curActiveIndex);
		Invoke("EnableMeteor", 0.1f);
		Invoke("DisablePortal", 4.5f);
	}

	private void EnableMeteor() {
		meteorsToEnable[curActiveIndex].SetActive(true);
	}

	[ClientRpc]
	private void RpcEnableMeteor(int index) {
		if (isServer) {
			return;
		}

		curActiveIndex = index;
		meteorsToEnable[curActiveIndex].SetActive(true);

		Invoke("DisablePortal", 4.5f);
	}

	private void DisablePortal() {
		if(curActiveIndex == -1) {
			Debug.Log("Tried to deactivate portal with invalid index. Looping through and disabling all portals");
			foreach(var p in meteorsToEnable) {
				p.SetActive(false);
			}
		}

		meteorsToEnable[curActiveIndex].SetActive(false);
		curActiveIndex = -1;
	}

	#endregion

	#region Dragonkin Summoning Testing
	[Button]
	public void StartSpawnAbility() {
		if (!isServer) {
			return;
		}

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
