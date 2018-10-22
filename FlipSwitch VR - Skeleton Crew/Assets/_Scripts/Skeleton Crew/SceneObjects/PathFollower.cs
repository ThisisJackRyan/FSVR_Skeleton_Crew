﻿using Sirenix.OdinInspector;
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

	public int encounterOneTotalTime = 180, breakTimer = 60;

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

	[SerializeField]
	EncounterStage currentStage;
	[Button]
	public void WorkAroundSpawn() {
		SpawnEncounter(currentStage);

	}

	protected void Start() {
		if (!isServer) {
			return;
		}
		currentNode = 0;
		nextNode = 1;
		currRot = transform.rotation;
		nextRot = CalcRotation(path.Nodes[nextNode]);
	}

	public void StartMoving() {
		if (!isServer) {
			return;
		}
		canMove = true;
		currentStage = EncounterStage.Tutorial;
		Invoke("EnemyWipeThenFirstBreak", encounterOneTotalTime);
	}

	public void ChangeSpeed(bool faster) {
		speed = (faster) ? maxSpeed : minSpeed;
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

		return true;
	}

	internal void DestroyCrystal(int i) {
		if (isServer) {
			//NetworkServer.Destroy( g );
			//print("i am the server, destroy crystal was called, should have spawned fragments");

			GameObject go = Instantiate(crystalDeathParticles, crystalRoot.GetChild(i).position, Quaternion.identity);
			NetworkServer.Spawn(go);

            Destroy(crystalRoot.GetChild(i).gameObject);
            RpcDestroyCrystalOnClient(i);

		} //else {
		//	//print("im not the server, but destroy crystal was called, should have spawned fragments");
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
	void StartFirstBreak() {
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
		foreach(var t in tutorialPanels ) {
			t.SetActive( false );
		}
	}

	void StartSecondPhase() {
		TurnOffTutorialPanels();

		currentStage = EncounterStage.Second;

		if (isServer && meteorClip) {
			Captain.instance.PlayDialogue(meteorClip.name);
		}

		Invoke("StartThirdPhase", encounterOneTotalTime);
		InvokeRepeating("SpawnMeteors", meteorSpawnTimer, meteorSpawnTimer);
	}

	void StartThirdPhase() {
		currentStage = EncounterStage.Third;
		if (isServer && mutinyClip) {
			Captain.instance.PlayDialogue(mutinyClip.name);
		}
		CancelInvoke("SpawnMeteors");
	}

	protected void Update() {
		//if (Input.GetKeyDown(KeyCode.Space)) {
		//	StartMoving();
		//}
		if (!isServer) {
			return;
		}

		if (!canMove) {
			return;
		}

		if (nextNode < path.Nodes.Length) {
			MovePosition();
		} else {
			//print("next node too high " + nextNode + " " + path.Nodes.Length);
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
		////print("curr " + currRot + " next " + nextRot);
		transform.rotation = Quaternion.Lerp(currRot, nextRot, perc);

		if (perc >= 1) {
			IncrementNode();
		}
	}

	Quaternion CalcRotation(Transform target) {
		Vector3 vectorToTarget = target.transform.position - transform.position;
		Vector3 facingDirection = transform.forward; // just for clarity!

		float angleInDegrees = Vector3.Angle(facingDirection, vectorToTarget);
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

		if (currentStage != EncounterStage.FirstBreak || currentStage != EncounterStage.SecondBreak) {
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
				////print( "hit node during first" );
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
				////print( "hit node during second" );

				Spawn(secondEncounters);
				break;
			case EncounterStage.Third:
				if (FindObjectsOfType<Enemy>().Length >= maxEnemies) {
                    ChangeSpeed(false);
                    return;
				}

				Spawn(thirdEncounters);
				break;
			case EncounterStage.Tutorial:
				////print("calling spawn with index " + ( currentNode - 1 ) );

				SpawnWithPortal(tutorialSpawns, currentNode - 1);
				if (tutorialSpawns.Length == currentNode) {
					////print("hit last node in tutorial, moving to first encounter");
					currentStage = EncounterStage.First;
				}

				break;
			default:
				////print("hit node during break or tutorial: " + currentStage.ToString());
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

	public void Spawn(GameObject[] toSpawnList, int specifiedIndex = -1) {
		if (!isServer) {
			return;
		}

		int spawnIndex = (specifiedIndex != -1) ? specifiedIndex : Random.Range(0, toSpawnList.Length);
		prefabToSpawn = toSpawnList[spawnIndex];

		////print( name + " called spawn " + Time.time + " prefabToSpawn " + prefabToSpawn.name  );
		//find rock
		List<GameObject> rocks = new List<GameObject>();

		foreach (GameObject go in Floaters) {
			float dist = Vector3.Distance(shipTransform.position, go.transform.position);
			////print( "distance to " + go.name + " is " + dist );
			if (dist > spawnRadiusMin && dist < spawnRadiusMax) {
				rocks.Add(go);
			}
		}
		////print( "number of floaters " + Floaters.Length );
		////print( "rocks in range " + rocks.Count );

		if (rocks.Count > 0) {
			int chosenOne = Random.Range(0, rocks.Count);
			//calc other side
			Vector3 spawnVector = rocks[chosenOne].transform.position - shipTransform.position;
			spawnVector = rocks[chosenOne].transform.position + (spawnVector.normalized * spawnDistFromRock);

			float rng = Random.Range(yLimiter.x, yLimiter.y);
			spawnVector.y = rng;
			//spawn
			GameObject g = Instantiate(prefabToSpawn, spawnVector, Quaternion.identity);

			if (g.GetComponent<ImpactReticuleSpawner>()) {
				foreach (var v in g.GetComponents<ImpactReticuleSpawner>()) {
					v.deckMesh = shipDeck;
				}
			}

			////print( g.name + " spawned, calling rpc" );
			//RpcSpawnEnemy( g, spawnVector );
			NetworkServer.Spawn(g);
		}
	}

	public List<Transform> portalPosPoints;
	public GameObject portal;
	public float distanceBehindPortal = 4;
	public int maxEnemies = 16;

	public void SpawnWithPortal(GameObject[] toSpawnList, int specifiedIndex = -1) {
		if (!isServer) {
			return;
		}

		int spawnIndex = (specifiedIndex != -1) ? specifiedIndex : Random.Range(0, toSpawnList.Length);
		prefabToSpawn = toSpawnList[spawnIndex];

		int chosenOne = Random.Range(0, portalPosPoints.Count);
		//calc other side
		Vector3 spawnVector = portalPosPoints[chosenOne].position;

		GameObject g = Instantiate(prefabToSpawn, spawnVector, Quaternion.identity);

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
	}

	#endregion

	enum EncounterStage {
		Tutorial, First, Second, Third, FirstBreak, SecondBreak
	}
}