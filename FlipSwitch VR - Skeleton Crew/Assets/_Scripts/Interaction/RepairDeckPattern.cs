using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairDeckPattern : MonoBehaviour {

	internal int index = 0;
	public DeckDamage deckDmg;

	public void Init(Transform activator) {
		//print( name + " init called" );

		index = 0;
		for ( int i = index + 1; i < transform.childCount; i++ ) {
			transform.GetChild( i ).gameObject.SetActive( false );
		}

		

		FaceActivator(activator);

		deckDmg.FacePattern(transform);
		
		Increment();
		//print("awerrrrrrrrrrrrrrrrrrrrrrrgwer");
	}

	void FaceActivator(Transform activator) {
		transform.position = new Vector3( transform.position.x,
										 activator.GetComponentInChildren<HipMarker>().transform.position.y,
										 transform.position.z );

		Vector3 v = activator.GetComponentInChildren<HipMarker>().transform.position - transform.position;

		v.x = v.z = 0.0f;
		transform.LookAt( Camera.main.transform.position - v );
		transform.Rotate( 0, 180, 0 );
	}
	// Use this for initialization
	public virtual void Increment() {
		//print( "incrememnt called with index of " + index );

		deckDmg.DisableRepairNode( index );
		index++;
		if ( index < transform.childCount ) {
			//print("index in range");
			transform.GetChild( index ).gameObject.SetActive( true );
			deckDmg.EnableRepairNode( index );
		} else if ( index == transform.childCount ) {
			
			GetComponentInParent<RepairDeckTrigger>().repairPattern = null;

			deckDmg.RepairDeck();
			//gameObject.SetActive( false );
			
		}
	}

	private void OnDisable() {
		//print(name + " disabled, turning off nodes");

		for ( int i = 1; i < transform.childCount; i++ ) {
			transform.GetChild( i ).gameObject.SetActive( false );
		}
	}




}
