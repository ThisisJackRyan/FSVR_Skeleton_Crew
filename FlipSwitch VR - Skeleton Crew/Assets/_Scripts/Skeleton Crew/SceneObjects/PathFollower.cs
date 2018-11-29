using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class PathFollower : NetworkBehaviour {
#pragma warning disable 0219

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
	public float xOffset = 50;
	public float meteorSpawnTimer = .5f;
	public float meteorRadius = 40;
	public GameObject[] meteors;
	Quaternion currRot, nextRot;
	GameObject prefabToSpawn;

	public int encounterOneTotalTime = 180,
		encounterTwoTotalTime = 180,
		encounterthreeTotalTime = 180,
		breakTimer = 60;

	bool canMove = false;
	bool firstMove = true;

	[Header("Spawning stuff")]
	public float spawnDistFromRock = 2;
	public float spawnRadiusMin, spawnRadiusMax;
	public Transform shipTransform;
	[Tooltip("second encounters will be the object that spawns the meteor prefab, not the prefab itself. third encounters is for ratmen." +
		"it again will have a specific object that tells rats to spawn. will prolly be changed tho. ")]
	public GameObject[] firstEncountersRanged, firstEncountersMelee, secondEncounters, thirdEncounters, tutorialSpawns;

	public GameObject[] ratkinSpawnPositions;
	public Transform crystalRoot;

	[Header("New Spawning Stuff")]
	// Publics
	public GameObject[] portPortalSpawnPositions;
	public List<GameObject> portShipMovePositions;
	public GameObject[] starboardPortalSpawnPositions;
	public List<GameObject> starboardShipMovePositions;
	public GameObject shipToSpawn;
	public float minDistanceToMove = 10, maxDistanceToMove = 25;

	// Privates
	private bool portIsOccupied;
	private bool starboardIsOccupied;
	private bool spawnOnPortSide = true;

	[Header("Ratkin Rebellion Swap")]
	// publics
	public GameObject swapParticles;
	public GameObject ratkinRebelPrefab;

	// privates
	private bool firstTimeRatkinRebel = true;

	[Space]
	[SerializeField]
	public EncounterStage currentStage;
	[Button]
	public void WorkAroundSpawn() {
		SpawnEncounter(currentStage);

	}

	[Button]
	public void SpawnFirstEncounterMelee() {
		SpawnWithPortal(firstEncountersMelee);
	}

	protected void Start() {
		if (!isServer) {
			return;
		}
		currentNode = 0;
		nextNode = 1;
		currRot = transform.rotation;
		nextRot = CalcRotation(path.Nodes[nextNode]);

		//update phase timers
		encounterOneTotalTime = VariableHolder.instance.phaseOneTimer;
		encounterTwoTotalTime = VariableHolder.instance.phaseTwoTimer;
		encounterthreeTotalTime = VariableHolder.instance.phaseThreeTimer;
		breakTimer = VariableHolder.instance.breakTimer;
	}

	[Button]
	public void StartMoving() {
		if (!isServer) {
			return;
		}
		canMove = true;
		currentStage = EncounterStage.Tutorial;
		Invoke("EnemyWipeThenFirstBreak", encounterOneTotalTime);
	}

	public MastSwitch mast;
	public void ChangeSpeed(bool faster) {
		speed = (faster) ? maxSpeed : minSpeed;
		mast.AdjustSails();

		UpdateWindProp();
	}

	public bool ChangeSpeed(float increment) {
		if (firstMove && Mathf.Sign(increment) == 1) {
			StartMoving();
			firstMove = false;
		}

		if (speed + increment == maxSpeed || speed + increment == minSpeed) {
			return false;
		}

		speed += increment;
		if (speed > maxSpeed) {
			speed = maxSpeed;
		} else if (speed < minSpeed) {
			speed = minSpeed;
		}

		UpdateWindProp();

		return true;
	}

	public void ChangeSpeed(int sign) {
		ChangeSpeed(mast.speedIncrement * sign);
	}

	void UpdateWindProp() {
#if PROP_ENABLED
		if (!isServer) {
			return;
		}

		CancelInvoke("InvokeWind");
		Invoke("InvokeWind", 1.0f);
		//InvokeWind();

		// .3, .5, .7, .9, 1.1, 1.3, 1.5

#endif

	}

	void InvokeWind() {
		//print("speed is " + speed);
		if (speed == 0) {
			//print("wind off");
			PropController.Instance.ActivateProp(Prop.WindOff);
		} else if (speed >= .3f && speed <= .7f) {
			//print("wind low");

			PropController.Instance.ActivateProp(Prop.WindLow);
		} else if (speed >= .9f && speed <= 1.3f) {
			//print("wind med");

			PropController.Instance.ActivateProp(Prop.WindMed);
		} else if (speed >= 1.5f) {
			//print("wind high");

			PropController.Instance.ActivateProp(Prop.WindHigh);
		}
	}

	internal void DestroyCrystal(int i) {
		if (isServer) {
			//NetworkServer.Destroy( g );
			////print("i am the server, destroy crystal was called, should have spawned fragments");

			GameObject go = Instantiate(crystalDeathParticles, crystalRoot.GetChild(i).position, Quaternion.identity);
			NetworkServer.Spawn(go);

            Destroy(crystalRoot.GetChild(i).gameObject);
            RpcDestroyCrystalOnClient(i);

		} //else {
		//	////print("im not the server, but destroy crystal was called, should have spawned fragments");
		//}
	}

    [ClientRpc]
    internal void RpcDestroyCrystalOnClient(int i) {
        if (isServer) {
            return;
        }

        Destroy(crystalRoot.GetChild(i).gameObject);

    }

	public AudioClip breakClip, mutinyClip, meteorClip;
	public void StartFirstBreak() {
		CancelInvoke("StartFirstBreak");
		currentStage = EncounterStage.FirstBreak;
		if (isServer && breakClip) {
			Captain.instance.PlayDialogue(breakClip.name);
		}

		Invoke("StartSecondPhase", breakTimer);
	}

    void EnemyWipeThenFirstBreak() {
        if (isServer) {
            Captain.instance.FullWipeAttack();
            Invoke("StartFirstBreak", 5f);
        }
    }

	public GameObject[] tutorialPanels;
	void TurnOffTutorialPanels() {
		if (!isServer) {
			return;
		}

		RpcTurnOffTutorialPanels();
		foreach(var t in tutorialPanels ) {
			if (t) {

			t.SetActive( false );
			}
		}
		//print("should be turned off on server");
	}

	[ClientRpc]
	private void RpcTurnOffTutorialPanels() {
		if (isServer) {
			return;
		}

		foreach (var t in tutorialPanels) {
			if (t) {
				t.SetActive(false);
			}
		}
		//print("should be turned off on client");
	}

	public void StartSecondPhase() {
		CancelInvoke("StartSecondPhase");

		if (isServer) {
			////print("called turn off panels on server");
			TurnOffTutorialPanels();
		}

		currentStage = EncounterStage.Second;

		if (isServer && meteorClip) {
			Captain.instance.PlayDialogue(meteorClip.name);
		}

		Invoke("StartThirdPhase", encounterTwoTotalTime);
		InvokeRepeating("SpawnMeteors", meteorSpawnTimer, meteorSpawnTimer);
	}

	public void StartThirdPhase() {
		CancelInvoke("StartThirdPhase");
		CancelInvoke("SpawnMeteors");

		currentStage = EncounterStage.Third;
		if (isServer && mutinyClip) {
			Captain.instance.PlayDialogue(mutinyClip.name);
		}
		CancelInvoke("SpawnMeteors");
		//InitialRebellion();
		SpawnEncounter(EncounterStage.Third);
		Invoke( "SpawnBossCave", encounterthreeTotalTime );

	}
	
	public GameObject[] landmarks;
	public GameObject bossCave;
	public float distanceToDelete = 100;
	public float caveMultiplier = 10;
	[Button]
	public void SpawnBossCave() {
		CancelInvoke("SpawnBossCave");
		currentStage = EncounterStage.ThirdBreak;
		foreach (var lm in landmarks) {
			float dist = Vector3.Distance(shipTransform.position, lm.transform.position);
			if (dist >= distanceToDelete) {
				lm.SetActive(false);
			}
		}
		bossCave.transform.position = new Vector3(shipTransform.position.x, shipTransform.position.y, shipTransform.position.z - (distanceToDelete * caveMultiplier));
		foreach(var v in Captain.instance.mastRopes) {
			v.enabled = false;
		}

		ChangeSpeed(true);
	}

	protected void Update() {

		if (!isServer) {
			return;
		}

		if (Input.GetKeyDown(KeyCode.Space)) {
			SpawnWithPortal(firstEncountersMelee);
		}

		if (!canMove) {
			return;
		}

		if (nextNode < path.Nodes.Length) {
			MovePosition();
		} else {
			////print("next node too high " + nextNode + " " + path.Nodes.Length);
		}
	}

	void SpawnMeteors() {
		bool hitDeck = false;
		Vector2 spawnVector;
		Vector3 rayVector;
		do {
			hitDeck = false;
			spawnVector = Random.insideUnitCircle * 40;
			rayVector = new Vector3(spawnVector.x + shipDeck.transform.position.x, Random.Range(5f, 50f), spawnVector.y + shipDeck.transform.position.z);
			var hits = Physics.RaycastAll(rayVector, Vector3.down);
			foreach (var hit in hits) {
				if (hit.collider.gameObject == shipDeck) {
					hitDeck = true;
				}
			}
		} while (hitDeck);

		//not hitting deck
		int rng = Random.Range(0, meteors.Length);
		rayVector.x += xOffset;
		var m = Instantiate(meteors[rng], rayVector, Quaternion.identity);
		m.transform.parent = transform;
		NetworkServer.Spawn(m);
	}

#pragma warning disable 0219

	void MovePosition() {
		//increment timer once per frame
		currentLerpTime += Time.deltaTime * speed;
		if (currentLerpTime > timeToNextNode) {
			currentLerpTime = timeToNextNode;
		}

		//lerp!
		float perc = currentLerpTime / timeToNextNode;
		transform.position = Vector3.Lerp(path.Nodes[currentNode].position, path.Nodes[nextNode].position, perc);

		//lerp rotation
		//////print("curr " + currRot + " next " + nextRot);
		transform.rotation = Quaternion.Lerp(currRot, nextRot, perc);

		if (perc >= 1) {
			IncrementNode();
		}
	}

	Quaternion CalcRotation(Transform target) {
		Vector3 vectorToTarget = target.transform.position - transform.position;
		Vector3 facingDirection = transform.forward; // just for clarity!

		//float angleInDegrees = Vector3.Angle(facingDirection, vectorToTarget);
		Quaternion rotation = Quaternion.FromToRotation(facingDirection, vectorToTarget);

		return rotation * transform.rotation;
	}

    [Button("increment path")]
	void IncrementNode() {
		currentNode = nextNode;
		if (this.nextNode < path.Nodes.Length - 1) {
			nextNode++;
		}

		currentLerpTime = 0f;

		if (currentStage != EncounterStage.FirstBreak && currentStage != EncounterStage.SecondBreak) {
			if (path.Nodes[currentNode].GetComponent<EncounterNode>()) {
				path.Nodes[currentNode].GetComponent<EncounterNode>().SpawnEncounter();
			} else {
				SpawnEncounter(currentStage);
			}
		}

		//update rot values
		currRot = nextRot;
		nextRot = CalcRotation(path.Nodes[nextNode]);
	}

	void SpawnEncounter(EncounterStage stage) {
		switch (stage) {
			case EncounterStage.First:
				//////print( "hit node during first" );
				//test enemy count
				if (FindObjectsOfType<Enemy>().Length >= maxEnemies) {
                    ChangeSpeed(false);
					return;
				}
                if (NumberOfPlayerHolder.instance.numberOfPlayers <= 2) {
                    if (VariableHolder.instance.numRangedUnits > 2) {
                        int rand = Random.Range(0, 100);
                        if (rand <= 49) {
                            SpawnWithPortal(firstEncountersRanged);
                        } else {
                            SpawnWithPortal(firstEncountersMelee);
                        }
                    } else {
                        SpawnWithPortal(firstEncountersRanged);
                    }
                } else {
                    SpawnWithPortal(firstEncountersRanged);
                }
				break;
			case EncounterStage.Second:
				//////print( "hit node during second" );

				Spawn(secondEncounters);
				break;
			case EncounterStage.Third:
				if (FindObjectsOfType<Enemy>().Length >= maxEnemies) {
                    ChangeSpeed(false);
                    return;
				}
				//if (firstTimeRatkinRebel) {
				//} else {
					Spawn(thirdEncounters);
				//}
				break;
			case EncounterStage.Tutorial:
				//////print("calling spawn with index " + ( currentNode - 1 ) );
				///
				if ( FindObjectsOfType<Enemy>().Length >= maxEnemies ) {
					ChangeSpeed( false );
					return;
				}

				SpawnWithPortal(tutorialSpawns, currentNode - 1);
				if (tutorialSpawns.Length == currentNode) {
					//////print("hit last node in tutorial, moving to first encounter");
					currentStage = EncounterStage.First;
				}

				break;
			default:
				//////print("hit node during break or tutorial: " + currentStage.ToString());
				break;
		}
	}

	#region spawn stuff	

	public static GameObject[] Floaters {
		get {
			return floaters ?? (floaters = GameObject.FindGameObjectsWithTag("Floater"));
		}
	}

	static GameObject[] floaters;

	private void InitialRebellion() {
		if (!isServer) {
			return;
		}

		var ratkinSlaveList = FindObjectsOfType<Ratman>();
		foreach (var slave in ratkinSlaveList) {
			if (slave.IsDead()) {
				continue;
			}

			GameObject temp = slave.gameObject;
			NetworkServer.Destroy(slave.gameObject);

			GameObject p = Instantiate(swapParticles, temp.transform.position, Quaternion.identity);
			NetworkServer.Spawn(p);

			GameObject r = Instantiate(ratkinRebelPrefab, temp.transform.position, Quaternion.identity);
			NetworkServer.Spawn(r);

		}

		firstTimeRatkinRebel = false;
	}

	public void Spawn(GameObject[] toSpawnList, int specifiedIndex = -1) {
		if (!isServer) {
			return;
		}

		int spawnIndex = (specifiedIndex != -1) ? specifiedIndex : Random.Range(0, toSpawnList.Length);
		prefabToSpawn = toSpawnList[spawnIndex];

		print(name + " called spawn " + Time.time + " prefabToSpawn " + prefabToSpawn.name);
		//find rock
		//List<GameObject> rocks = new List<GameObject>();

		//foreach (GameObject go in Floaters) {
		//	float dist = Vector3.Distance(shipTransform.position, go.transform.position);
		//	//////print( "distance to " + go.name + " is " + dist );
		//	if (dist > spawnRadiusMin && dist < spawnRadiusMax) {
		//		rocks.Add(go);
		//	}
		//}
		////////print( "number of floaters " + Floaters.Length );
		////////print( "rocks in range " + rocks.Count );

		//if (rocks.Count > 0) {
		//	int chosenOne = Random.Range(0, rocks.Count);
		//	//calc other side
		//	Vector3 spawnVector = rocks[chosenOne].transform.position - shipTransform.position;

		//	float rng = Random.Range(yLimiter.x, yLimiter.y);
		//	spawnVector.y = rng;
		//	//spawn
			spawnVector = transform.position;
			GameObject g = Instantiate(prefabToSpawn, spawnVector, Quaternion.identity);

			if (g.GetComponent<ImpactReticuleSpawner>()) {
				foreach (var v in g.GetComponentsInChildren<ImpactReticuleSpawner>()) {
					print("setting deck on spawner");
					v.deckMesh = shipDeck;
				}
			}

			//////print( g.name + " spawned, calling rpc" );
			//RpcSpawnEnemy( g, spawnVector );
			NetworkServer.Spawn(g);
		//}
	}

	public List<Transform> portalPosPoints;
	public GameObject portal;
	public float distanceBehindPortal = 4;
	public int maxEnemies = 16;

	public void SpawnShip(bool portSide) {
		if (portSide) {
			GameObject p = portPortalSpawnPositions[Random.Range(0, portPortalSpawnPositions.Length)];
			spawnVector = p.transform.position;
			lookPos = spawnVector + (-Vector3.forward * Random.Range(minDistanceToMove, maxDistanceToMove));

			p = Instantiate(portal, spawnVector, Quaternion.LookRotation(-Vector3.forward, Vector3.up));

			GameObject s = Instantiate(shipToSpawn, spawnVector, Quaternion.LookRotation(-Vector3.forward, Vector3.up));
			s.GetComponent<BehaviorDesigner.Runtime.BehaviorTree>().SetVariableValue("TargetPosition", lookPos);
			s.GetComponent<BehaviorDesigner.Runtime.BehaviorTree>().SetVariableValue("MovePositions", portShipMovePositions);
			s.GetComponent<BoardingPartySpawner>().portSideShip = true;
			s.GetComponent<BoardingPartySpawner>().pathFollowerRef = this;
			if (s.GetComponent<ImpactReticuleSpawner>()) {
				foreach (var v in s.GetComponents<ImpactReticuleSpawner>()) {
					v.deckMesh = shipDeck;
				}
			}

			NetworkServer.Spawn(p);
			NetworkServer.Spawn(s);
		} else {
			GameObject p = starboardPortalSpawnPositions[Random.Range(0, starboardPortalSpawnPositions.Length)];
			spawnVector = p.transform.position;
			lookPos = spawnVector + (-Vector3.forward * Random.Range(minDistanceToMove, maxDistanceToMove));

			p = Instantiate(portal, spawnVector, Quaternion.LookRotation(-Vector3.forward, Vector3.up));

			GameObject s = Instantiate(shipToSpawn, spawnVector, Quaternion.LookRotation(-Vector3.forward, Vector3.up));
			s.GetComponent<BehaviorDesigner.Runtime.BehaviorTree>().SetVariableValue("TargetPosition", lookPos);
			s.GetComponent<BehaviorDesigner.Runtime.BehaviorTree>().SetVariableValue("MovePositions", starboardShipMovePositions);
			s.GetComponent<BoardingPartySpawner>().portSideShip = false;
			s.GetComponent<BoardingPartySpawner>().pathFollowerRef = this;

			if (s.GetComponent<ImpactReticuleSpawner>()) {
				foreach (var v in s.GetComponents<ImpactReticuleSpawner>()) {
					v.deckMesh = shipDeck;
				}
			}

			NetworkServer.Spawn(p);
			NetworkServer.Spawn(s);
		}
	}

	public void ShipDestroyed(bool isPort) {
		if (isPort) {
			portIsOccupied = false;
		} else {
			starboardIsOccupied = false;
		}
	}

	public void SpawnWithPortal(GameObject[] toSpawnList, int specifiedIndex = -1) {
		if (!isServer) {
			return;
		}

		if (spawnOnPortSide) {
			if (portIsOccupied) { // Port is already spawned so don't do it, see if starboard side is available
				if (starboardIsOccupied) { // Cannot spawn on starboard side. So break out.
					return;
				} else { // Can spawn one on starboard side so do it, don't toggle since this side will most likely be destroyed first.
					SpawnShip(false);
					starboardIsOccupied = true;
				}
			} else { // Ports turn to spawn and it can spawn one so do it. Toggle.
				SpawnShip(true);
				portIsOccupied = true;
				spawnOnPortSide = !spawnOnPortSide;
			}
		} else if(!spawnOnPortSide) {
			if (starboardIsOccupied) { // Starboard is already spawned so don't do it, see if port side is available
				if (portIsOccupied) { // Cannot spawn on port side. So break out.
					return;
				} else { // Can spawn one on port side so do it, don't toggle since this side will most likely be destroyed first.
					SpawnShip(true);
					portIsOccupied = true;
				}
			} else { // Starboards turn to spawn and it can so do it. Toggle.
				SpawnShip(false);
				starboardIsOccupied = true;
				spawnOnPortSide = !spawnOnPortSide;
			}
		}

		/* --- OLD STUFF ---
		int spawnIndex = (specifiedIndex != -1) ? specifiedIndex : Random.Range(0, toSpawnList.Length);
		prefabToSpawn = toSpawnList[spawnIndex];

		int chosenOne = Random.Range(0, portalPosPoints.Count);
		//calc other side
		spawnVector = portalPosPoints[chosenOne].position;

		lookPos = new Vector3(shipDeck.transform.position.x, spawnVector.y, spawnVector.z);
		facingVector =  lookPos - spawnVector;
		GameObject g = Instantiate(prefabToSpawn, spawnVector, Quaternion.identity);
		lookPos.x =  Mathf.Sign(Random.insideUnitCircle.x) * 10;
		//int y = (int)Mathf.Sign(Random.insideUnitCircle.x) * 5;
		lookPos.y = Random.Range(3,7);

		g.GetComponent<BehaviorDesigner.Runtime.BehaviorTree>().SetVariableValue("TargetPosition", lookPos);


		spawnVector += (spawnVector - shipTransform.position).normalized * -distanceBehindPortal;
		//g.GetComponent<BehaviorDesigner.Runtime.BehaviorTree>().SetVariable("TargetPosition", new Vector3())

		GameObject p = Instantiate(portal, spawnVector, Quaternion.LookRotation((spawnVector - shipTransform.position), Vector3.up));

		if (g.GetComponent<ImpactReticuleSpawner>()) {
			foreach (var v in g.GetComponents<ImpactReticuleSpawner>()) {
				v.deckMesh = shipDeck;
			}
		}

		NetworkServer.Spawn(g);
		NetworkServer.Spawn(p);
		--- END OF OLD STUFF ---*/


	}
	public Vector3 lookPos, facingVector, spawnVector;
	private void OnDrawGizmosSelected() {
		Gizmos.DrawSphere(lookPos, 1f);
		Gizmos.DrawRay(spawnVector, facingVector);
	}

	#endregion

	public enum EncounterStage {
		Tutorial, First, Second, Third, FirstBreak, SecondBreak, ThirdBreak
	}
}