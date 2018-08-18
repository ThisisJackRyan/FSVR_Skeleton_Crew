using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BoardingPartySpawner : NetworkBehaviour {

	public GameObject[] crewBosses, crewMembers;


	// Use this for initialization
	void Start () {

		int bossIndex = Random.Range( 0, crewBosses.Length );
		int crewIndex1 = Random.Range( 0, crewMembers.Length );
		int crewIndex2 = Random.Range( 0, crewMembers.Length );

		print( "Spawing " + crewBosses[bossIndex].name + " as boss, and " + crewMembers[crewIndex1] + " as crew member 1, " + crewMembers[crewIndex2] + " as crew member 2." );
		if (!isServer) {
			return;
		}
		print( "Spawing " + crewBosses[bossIndex].name + " as boss, and " + crewMembers[crewIndex1] + " as crew member 1, " + crewMembers[crewIndex2] + " as crew member 2." );

		GameObject boss = Instantiate(crewBosses[bossIndex], transform.GetChild(0).position, Quaternion.identity);
		NetworkServer.Spawn(boss);

		GameObject crew1 = Instantiate(crewMembers[crewIndex1], transform.GetChild(1).position, Quaternion.identity);
		NetworkServer.Spawn( crew1 );


		GameObject crew2 = Instantiate(crewMembers[crewIndex2], transform.GetChild(2).position, Quaternion.identity);
		NetworkServer.Spawn( crew2 );

	}

	private void RpcSpawnEnemy(GameObject g, int childIndex) {
		//if (isServer) {
		//    return;
		//}
		print( "gadkjfas;lkdfj" );
		GameObject c = Instantiate(g, transform.GetChild(childIndex).position, Quaternion.identity);
		c.transform.parent = transform;
	}
	
}
