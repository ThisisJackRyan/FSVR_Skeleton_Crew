using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class DeckDamage : NetworkBehaviour {

	public RepairDeckPattern[] repairPatterns;
	[HideInInspector]
	public RepairDeckPattern repairPattern;
	public float repairRadius = 0.5f;
	public GameObject repairSphere;
	public GameObject particles;


	private void OnEnable() {
		print("called");
        if (isServer) {
			print("isserver");

			Captain.instance.AddEventToQueue(Captain.AudioEventType.RepairDeck);
        }


		Collider[] cols = Physics.OverlapSphere( transform.position, 0.5f );
		foreach ( var item in cols ) {
			if ( item.GetComponent<DeckDamage>() && item.transform.root.gameObject != gameObject) {
				if ( item.GetComponent<DeckDamage>().repairSphere.activeInHierarchy ) {
					repairSphere.SetActive( false );
				}
			}
		}
	}

	[SyncVar( hook = "OnPatternIndexChange" )] int rng = -1;
	private void OnPatternIndexChange( int n ) {
		if ( isServer ) {
			return;
		}
		rng = n;

		if ( rng != -1 ) {
			repairPattern = repairPatterns[rng];
			//print("repair patter is now " + repairPattern.name + " on the client damaged object");
		}
	}

	internal RepairDeckPattern SelectPattern() {
		if ( !isServer ) {
			return null;
		}

		rng = Random.Range( 0, repairPatterns.Length );
		repairPattern = repairPatterns[rng];
		//print( "setting repairPattern to " + repairPattern.name );
		foreach ( var pat in repairPatterns ) {
			pat.gameObject.SetActive( false );
		}

		return repairPattern;
	}

	public void RepairDeck() {
		if (!isServer ) {
			return;
		}

		Collider[] hits = Physics.OverlapSphere( transform.root.position, repairRadius );

		foreach ( var col in hits ) {
			if ( col.transform.root == transform.root ) {
				continue;
			}

			if ( col.GetComponentInParent<DeckDamage>() ) {
				NetworkServer.Destroy( col.transform.root.gameObject );
			}
		}

		var p = Instantiate( particles , transform.position, Quaternion.identity);
		NetworkServer.Spawn( p );
		NetworkServer.Destroy( transform.root.gameObject );
	}

	internal void DisableRepairNode( int index ) {
		if ( !isServer ) {
			return;
		}

		RpcDisableNode( index );
	}

	[ClientRpc]
	private void RpcDisableNode( int index ) {
		if ( isServer ) {
			return;
		}


		//print( "pattern name: " + repairPattern.name );
		//print( "index received: " + index );
		repairPattern.transform.GetChild( index ).gameObject.SetActive( false );
	}

	internal void EnableRepairNode( int index ) {
		if ( !isServer ) {
			return;
		}
		repairPattern.transform.GetChild( 0 ).gameObject.SetActive( true ); // Enables the pattern on server?

		RpcEnableNode( index );
	}

	[ClientRpc]
	private void RpcEnableNode( int index ) {
		if ( isServer ) {
			return;
		}


		repairPattern.transform.GetChild( 0 ).gameObject.SetActive( true );
		repairPattern.transform.GetChild( index ).gameObject.SetActive( true );
	}

	internal void EnablePatternOnClients() {
		if ( !isServer ) {
			return;
		}

		RpcEnablePattern();
	}

	[ClientRpc]
	private void RpcEnablePattern() {
		if ( isServer ) {
			return;
		}
		repairPattern.gameObject.SetActive( true ); // turns on the pattern gameobject
		repairSphere.transform.GetChild( 0 ).gameObject.SetActive( false ); //  disables the particles
	}

	public void PatternInit() {

	}

	[ClientRpc]
	public void RpcFacePattern(Vector3 pos, Quaternion rot) {
		if (isServer) {
			return;
		}

		repairPattern.transform.position = pos;
		repairPattern.transform.rotation = rot;

	}

	public void FacePattern(Transform transform1) {
		if(!isServer) {
			return;
		}

		RpcFacePattern(transform1.position, transform1.rotation);
	}
}
