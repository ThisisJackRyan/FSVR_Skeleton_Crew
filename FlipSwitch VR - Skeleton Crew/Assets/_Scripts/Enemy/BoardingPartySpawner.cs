using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;
public class BoardingPartySpawner : NetworkBehaviour {

	[Header("Use These")]
	// Publics
	public GameObject[] spawnableEnemies;
	public GameObject[] enemySpawnPositions;
	public float modifier = 1f;
	public int health = 4;
	public GameObject smokeTrailOnDeath;
	public GameObject cannonHitParticles;
	public GameObject finalCannonHitParticles;
	public bool portSideShip;
	public PathFollower pathFollowerRef;

	// Privates
	private int timesHit = 0;
	private BehaviorTree myTree;
	// public GameObject[] crewBosses, crewMembers, rangedMembers;
    // public bool useRanged;
	// Use this for initialization
	void Start () {
		if ( !isServer ) {
            ////print("not the server");
            return;
        }

		for (int i = 0; i < (int) Mathf.Floor((float) (NumberOfPlayerHolder.instance.numberOfPlayers ) * modifier); i++) {
			GameObject enemy = Instantiate(spawnableEnemies[Random.Range(0, spawnableEnemies.Length)], enemySpawnPositions[i].transform.position, Quaternion.identity);
			enemy.transform.parent = transform;
			NetworkServer.Spawn(enemy);
		}

		myTree = GetComponent<BehaviorTree>();
		print("myTree name: " + myTree.name);
		print("accessing the ship has arrived bool" + myTree.GetVariable("ShipHasArrived").Name);

   //     else{
			//////print("im the server");
			////transform.LookAt(FindObjectOfType<PathFollower>().lookPos);
			//Vector3 targetPos = transform.position + (new Vector3(transform.forward.z + Random.Range(25f, 35f), Random.Range(transform.forward.y - 2, transform.forward.y + 2),transform.forward.x));
			//GetComponent<BehaviorDesigner.Runtime.BehaviorTree>().SetVariableValue("TargetPosition", targetPos);
   //     }

        ////print("in start");

        //Debug.Break();
		/* --- OLD STUFF ---
		int bossIndex = Random.Range( 0, crewBosses.Length );
		int crewIndex1 = Random.Range( 0, crewMembers.Length );
        List<GameObject> crewmen = new List<GameObject>();

        if (useRanged) { // If ship can spawn a ranged unit
            ////print("use range");

            if (VariableHolder.instance.AddRangedUnit()) { // Try to add one to the unit count, retuns true if successful, false if max already reached
                int rangedIndex = Random.Range(0, rangedMembers.Length);
                GameObject ranged1 = Instantiate(rangedMembers[rangedIndex], transform.GetChild(1).position, Quaternion.Euler(Vector3.zero));
                ranged1.transform.parent = transform;
                ranged1.GetComponent<Enemy>().boardingPartyShip = gameObject;
				//print("should be adding " + ranged1.name + " to the boarding party");
                crewmen.Add(ranged1);
                NetworkServer.Spawn(ranged1);
				//print(ranged1.name + " should be spawned");
            } else { // max ranged reached, can't spawn ranged
                useRanged = false;
            }
        }

        if(!useRanged) { // spawn crew if either ranged unit limit reached, or not a ranged boarding party
            ////print("!useRange");

            GameObject crew1 = Instantiate(crewMembers[crewIndex1], transform.GetChild(1).position, Quaternion.identity);
            crew1.transform.parent = transform;
            crew1.GetComponent<Enemy>().boardingPartyShip = gameObject;
            ////print("set crew1 enemy boarding party ship to " + crew1.GetComponent<Enemy>().boardingPartyShip);
            crewmen.Add(crew1);
            NetworkServer.Spawn(crew1);
        }

		for(int i=0; i<FindObjectOfType<NumberOfPlayerHolder>().numberOfPlayers-2; i++ ) {
            ////print("for loop " + FindObjectOfType<NumberOfPlayerHolder>().numberOfPlayers);

            int crewIndex = Random.Range( 0, crewMembers.Length );
			
			GameObject crew = Instantiate( crewMembers[crewIndex], transform.GetChild( i+2 ).position, Quaternion.identity );
			crew.transform.parent = transform;
            crew.GetComponent<Enemy>().boardingPartyShip = gameObject;
            ////print("set crew" + i + " enemy boarding party ship to " + crew.GetComponent<Enemy>().boardingPartyShip);

            crewmen.Add(crew);
			NetworkServer.Spawn( crew );
		}

        ////print("after loop");


        GameObject boss = Instantiate(crewBosses[bossIndex], transform.GetChild(0).position, Quaternion.identity);
        boss.transform.parent = transform;
        boss.GetComponent<BehaviorTree>().SetVariableValue("ShipCrewmen", crewmen);
        boss.GetComponent<Enemy>().boardingPartyShip = gameObject;
        ////print("set boss enemy boarding party ship to " + boss.GetComponent<Enemy>().boardingPartyShip);

        NetworkServer.Spawn(boss);
        ////print("network server spawned the boss.");
		--- END OF OLD STUFF --- */
    }

	bool kinCheck = true;

	private void Update() {
		if (!isServer) {
			return;
		}

		if (kinCheck) {
			if ((bool) myTree.GetVariable("ShipHasArrived").GetValue()) {
				GetComponent<Rigidbody>().isKinematic = true;
				kinCheck = false;
			}
		}
	}

	private void OnCollisionEnter(Collision other) {
        if (!isServer) {
            return;
        }

		if ( other.gameObject.GetComponent<SCProjectile>() ) {
			if ( other.gameObject.GetComponent<SCProjectile>().isCannonball ) {
				//todo PLAYER SCORE INTEGRATION FOR PROJECTILE
				VariableHolder.instance.IncreasePlayerScore(other.gameObject.GetComponent<SCProjectile>().playerWhoFired, VariableHolder.PlayerScore.ScoreType.BoatsDestroyed, transform.position);
				timesHit++;
				GameObject g = Instantiate(timesHit >= health ? finalCannonHitParticles : cannonHitParticles, other.transform.position, Quaternion.identity);
				NetworkServer.Spawn(g);
				if (timesHit >= health) {
					EnableSmokeTrail();
					GetComponent<Rigidbody>().isKinematic = false;
					foreach(var v in GetComponentsInChildren<Rigidbody>()) {
						v.useGravity = true;
					}
					pathFollowerRef.ShipDestroyed(portSideShip);
				}
			} else {
				print("shot by player fire, but not cannonball");
			}
		}

		NetworkServer.Destroy(other.gameObject);
    }

	private void EnableSmokeTrail() {
		if (!isServer) {
			return;
		}

		RpcEnableSmokeTrail();
		smokeTrailOnDeath.SetActive(true);
	}

	[ClientRpc]
	private void RpcEnableSmokeTrail() {
		if (isServer) {
			return;
		}

		smokeTrailOnDeath.SetActive(true);
	}
}
