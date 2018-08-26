using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class PathFollower : NetworkBehaviour {

	public NodePath path;
	int currentNode, nextNode;
	public float speed = 1;
	public float maxSpeed = 2, minSpeed = 0.5f;
	public Vector2 yLimiter;
	public GameObject shipDeck;
	public GameObject crystalDeathParticles;

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

	public void ChangeSpeed(bool faster) {
		speed = (faster) ? maxSpeed : minSpeed;
	}

	internal void DestroyCrystal( GameObject g ) {
		if ( isServer ) {
			NetworkServer.Destroy( g );
			GameObject go = Instantiate( crystalDeathParticles, g.transform.position, Quaternion.identity );
			NetworkServer.Spawn( go );
		}
	}

	void ChangeToPhaseTwo() {
		currentStage = EncounterStage.firstBreak;
		Invoke( "StartSecondPhase", breakTimer );
	}

	void StartSecondPhase() {
		currentStage = EncounterStage.Second;
		Invoke("StartSecondBreak", encounterOneTotalTime);
		InvokeRepeating("SpawnMeteors", meteorSpawnTimer, meteorSpawnTimer);
	}

	void StartSecondBreak() {
		currentStage = EncounterStage.secondBreak;
		CancelInvoke( "SpawnMeteors" );
		//add timer for boss battle
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

	public float xOffset = 50;
	public float meteorSpawnTimer = .5f;
	public float meteorRadius = 40;
	public GameObject[] meteors;
	void SpawnMeteors() {
		bool hitDeck = false;
		Vector2 spawnVector;
		Vector3 rayVector;
		do { 
			hitDeck = false;
			spawnVector = Random.insideUnitCircle * 40;
			rayVector = new Vector3( spawnVector.x + shipDeck.transform.position.x, Random.Range(5f, 50f), spawnVector.y + shipDeck.transform.position.z );
			var hits = Physics.RaycastAll( rayVector, Vector3.down );
			foreach (var hit in hits) {
				if (hit.collider.gameObject == shipDeck) {
					hitDeck = true;
				}
			}
		} while ( hitDeck );

		//not hitting deck
		int rng = Random.Range(0, meteors.Length);
		rayVector.x += xOffset;
		var m = Instantiate(meteors[rng], rayVector, Quaternion.identity);
		m.transform.parent = transform;
		NetworkServer.Spawn( m );
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
		
		//print( name + " called spawn " + Time.time + " prefabToSpawn " + prefabToSpawn.name  );
		//find rock
		List<GameObject> rocks = new List<GameObject>();

		foreach ( GameObject go in Floaters ) {
			float dist = Vector3.Distance( shipTransform.position, go.transform.position );
			//print( "distance to " + go.name + " is " + dist );
			if ( dist > spawnRadiusMin && dist < spawnRadiusMax ) {
				rocks.Add( go );
			}
		}
		//print( "number of floaters " + Floaters.Length );
		//print( "rocks in range " + rocks.Count );

		if ( rocks.Count > 0 ) {
			int chosenOne = Random.Range( 0, rocks.Count );
			//calc other side
			Vector3 spawnVector = rocks[chosenOne].transform.position - shipTransform.position;
			spawnVector = rocks[chosenOne].transform.position + ( spawnVector.normalized * spawnDistFromRock );

			float rng = Random.Range(yLimiter.x, yLimiter.y);
			spawnVector.y = rng;
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