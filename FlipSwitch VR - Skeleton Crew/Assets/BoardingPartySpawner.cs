using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BoardingPartySpawner : NetworkBehaviour {

	public GameObject[] crewBosses, crewMembers;


	// Use this for initialization
	void Start () {
		if (!isServer) {
			return;
		}

		int bossIndex = Random.Range(0, crewBosses.Length);
		int crewIndex1 = Random.Range( 0, crewMembers.Length );
		int crewIndex2 = Random.Range( 0, crewMembers.Length );

		print("Spawing " + crewBosses[bossIndex].name + " as boss, and " + crewMembers[crewIndex1] + " as crew member 1, " + crewMembers[crewIndex2] +" as crew member 2.");
	}

	// Update is called once per frame
	void Update () {
		
	}
}
