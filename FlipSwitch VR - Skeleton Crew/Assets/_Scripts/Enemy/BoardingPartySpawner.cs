﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;
public class BoardingPartySpawner : NetworkBehaviour {

	public GameObject[] crewBosses, crewMembers, rangedMembers;
    public bool useRanged;
	// Use this for initialization
	void Start () {
		if ( !isServer ) {
			return;
		}

		int bossIndex = Random.Range( 0, crewBosses.Length );
		int crewIndex1 = Random.Range( 0, crewMembers.Length );
        List<GameObject> crewmen = new List<GameObject>();

        if (useRanged) { // If ship can spawn a ranged unit
            if (VariableHolder.instance.AddRangedUnit()) { // Try to add one to the unit count, retuns true if successful, false if max already reached
                int rangedIndex = Random.Range(0, rangedMembers.Length);
                GameObject ranged1 = Instantiate(rangedMembers[rangedIndex], transform.GetChild(1).position, Quaternion.identity);
                ranged1.transform.parent = transform;
                crewmen.Add(ranged1);
                NetworkServer.Spawn(ranged1);
            } else { // max ranged reached, can't spawn ranged
                useRanged = false;
            }
        }
        if(!useRanged) { // spawn crew if either ranged unit limit reached, or not a ranged boarding party
            GameObject crew1 = Instantiate(crewMembers[crewIndex1], transform.GetChild(1).position, Quaternion.identity);
            crew1.transform.parent = transform;
            crewmen.Add(crew1);
            NetworkServer.Spawn(crew1);
        }

		for(int i=0; i<FindObjectOfType<NumberOfPlayerHolder>().numberOfPlayers-2; i++ ) {
			int crewIndex = Random.Range( 0, crewMembers.Length );
			
			GameObject crew = Instantiate( crewMembers[crewIndex], transform.GetChild( i+2 ).position, Quaternion.identity );
			crew.transform.parent = transform;
            crewmen.Add(crew);
			NetworkServer.Spawn( crew );
		}

        GameObject boss = Instantiate(crewBosses[bossIndex], transform.GetChild(0).position, Quaternion.identity);
        boss.transform.parent = transform;
        boss.GetComponent<BehaviorTree>().SetVariableValue("ShipCrewmen", crewmen);
        NetworkServer.Spawn(boss);
    }
}
