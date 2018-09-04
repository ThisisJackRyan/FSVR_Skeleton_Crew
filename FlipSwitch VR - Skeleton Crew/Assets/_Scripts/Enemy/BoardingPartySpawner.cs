using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;
public class BoardingPartySpawner : NetworkBehaviour {

	public GameObject[] crewBosses, crewMembers;

	// Use this for initialization
	void Start () {
		if ( !isServer ) {
			return;
		}

		int bossIndex = Random.Range( 0, crewBosses.Length );
		int crewIndex1 = Random.Range( 0, crewMembers.Length );

        List<GameObject> crewmen = new List<GameObject>();

		GameObject crew1 = Instantiate(crewMembers[crewIndex1], transform.GetChild(1).position, Quaternion.identity);
		crew1.transform.parent = transform;
        crewmen.Add(crew1);
		NetworkServer.Spawn( crew1 );

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
