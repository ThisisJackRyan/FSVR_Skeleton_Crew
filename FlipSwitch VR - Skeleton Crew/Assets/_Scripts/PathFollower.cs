using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PathFollower : NetworkBehaviour {

	public NodePath path;
	int currentNode, nextNode;
	public float speed = 1;
	public GameObject shipDeck;

	[SerializeField]
	float timeToNextNode = 1f;
	float currentLerpTime;

	Vector3 startPos;
	Vector3 endPos;

	public int encounterOneTotalTime = 180, breakTimer = 60;

	bool canMove = false;

	protected void Start() {
		if ( !isServer ) {
			return;
		}
		currentNode = 0;
		nextNode = 1;
		currRot = transform.rotation;
		nextRot = CalcRotation( path.Nodes[nextNode] );
	}

	public void StartMoving() {
		if ( !isServer ) {
			return;
		}
		canMove = true;
		currentStage = EncounterStage.First;
		Invoke( "ChangeToPhaseTwo", encounterOneTotalTime );
	}

	void ChangeToPhaseTwo() {
		currentStage = EncounterStage.firstBreak;
		Invoke( "StartSecondPhase", breakTimer );
	}

	void StartSecondPhase() {
		currentStage = EncounterStage.Second;
	}

	protected void Update() {
		//if (Input.GetKeyDown(KeyCode.Space)) {
		//	StartMoving();
		//}
		if ( !isServer ) {
			return;
		}

		if ( !canMove ) {
			return;
		}

		if ( nextNode < path.Nodes.Length ) {
			MovePosition();
		} else {
			print( "next node too high " + nextNode + " " + path.Nodes.Length );
		}
	}

#pragma warning disable 0219

	void MovePosition() {
		//increment timer once per frame
		currentLerpTime += Time.deltaTime * speed;
		if ( currentLerpTime > timeToNextNode ) {
			currentLerpTime = timeToNextNode;
		}

		//lerp!
		float perc = currentLerpTime / timeToNextNode;
		transform.position = Vector3.Lerp( path.Nodes[currentNode].position, path.Nodes[nextNode].position, perc );

		//lerp rotation
		//print("curr " + currRot + " next " + nextRot);
		transform.rotation = Quaternion.Lerp( currRot, nextRot, perc );

		if ( perc >= 1 ) {
			IncrementNode();
		}
	}

	Quaternion currRot, nextRot;

	Quaternion CalcRotation( Transform target ) {
		Vector3 vectorToTarget = target.transform.position - transform.position;
		Vector3 facingDirection = transform.forward; // just for clarity!

		float angleInDegrees = Vector3.Angle( facingDirection, vectorToTarget );
		Quaternion rotation = Quaternion.FromToRotation( facingDirection, vectorToTarget );

		return rotation * transform.rotation;
	}

	void IncrementNode() {
		currentNode = nextNode;
		if ( this.nextNode < path.Nodes.Length - 1 ) {
			nextNode++;
		}

		currentLerpTime = 0f;

		SpawnEncounter( currentStage );

		//update rot values
		currRot = nextRot;
		nextRot = CalcRotation( path.Nodes[nextNode] );
	}

	void SpawnEncounter( EncounterStage stage ) {
		switch ( stage ) {
			case EncounterStage.First:
				print( "hit node during first" );

				Spawn( firstEncounters );
				break;
			case EncounterStage.Second:
				print( "hit node during second" );

				Spawn( secondEncounters );
				break;
			case EncounterStage.Third:
				Spawn( thirdEncounters );
				break;
			default:
				print("hit node during break");
				break;
		}
	}
	
	#region spawn stuff

	EncounterStage currentStage;
	GameObject prefabToSpawn;

	[Header("Spawning stuff")]
	public float spawnDistFromRock = 2;
	public float spawnRadiusMin, spawnRadiusMax;
	public Transform shipTransform;
	[Tooltip("second encounters will be the object that spawns the meteor prefab, not the prefab itself. third encounters is for ratmen." +
		"it again will have a specific object that tells rats to spawn. will prolly be changed tho. ")]
	public GameObject[] firstEncounters, secondEncounters, thirdEncounters;

	enum EncounterStage {
		First, Second, Third, firstBreak, secondBreak
	}

	public static GameObject[] Floaters {
		get {
			return floaters ?? ( floaters = GameObject.FindGameObjectsWithTag( "Floater" ) );
		}
	}

	static GameObject[] floaters;

	public void Spawn( GameObject[] toSpawnList ) {
		if ( !isServer ) {
			return;
		}


		int spawnIndex = Random.Range( 0, toSpawnList.Length );
		prefabToSpawn = toSpawnList[spawnIndex];
		
		print( name + " called spawn " + Time.time + " prefabToSpawn " + prefabToSpawn.name  );
		//find rock
		List<GameObject> rocks = new List<GameObject>();

		foreach ( GameObject go in Floaters ) {
			float dist = Vector3.Distance( shipTransform.position, go.transform.position );
			if ( dist > spawnRadiusMin && dist < spawnRadiusMax ) {
				rocks.Add( go );
			}
		}

		if ( rocks.Count > 0 ) {
			int chosenOne = Random.Range( 0, rocks.Count );
			//calc other side
			Vector3 spawnVector = rocks[chosenOne].transform.position - shipTransform.position;
			spawnVector = rocks[chosenOne].transform.position + ( spawnVector.normalized * spawnDistFromRock );
			//spawn
			GameObject g = Instantiate( prefabToSpawn, spawnVector, Quaternion.identity );

			if (g.GetComponent<ImpactReticuleSpawner>()) {
				g.GetComponent<ImpactReticuleSpawner>().deckMesh = shipDeck;
			}

			print( g.name + " spawned, calling rpc" );
			//RpcSpawnEnemy( g, spawnVector );
			NetworkServer.Spawn(g);
		}
	}

#pragma warning disable 0219

	[ClientRpc]
	private void RpcSpawnEnemy( GameObject g, Vector3 spawnPos ) {
		if ( isServer ) {
			print("server says G is " + g.name);
			return;
		}
		Instantiate( g, spawnPos, Quaternion.identity );
	}

	#endregion

}