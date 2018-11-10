using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairDeckPattern : MonoBehaviour {

	internal int index = 0;
	public DeckDamage deckDmg;
	public GameObject repairerInstance;
	public void Init(Transform activator) {
		//print( name + " init called" );

		index = 0;
		for ( int i = index + 1; i < transform.childCount; i++ ) {
			transform.GetChild( i ).gameObject.SetActive( false );
		}

		

		//FaceActivator(activator);

		deckDmg.FacePattern(transform);
		
		Increment(null, null);
		//print("awerrrrrrrrrrrrrrrrrrrrrrrgwer");
	}

	//void FaceActivator(Transform activator) {
	//	transform.position = new Vector3( transform.position.x,
	//									 activator.GetComponentInChildren<HipMarker>().transform.position.y,
	//									 transform.position.z );

	//	Vector3 v = activator.GetComponentInChildren<HipMarker>().transform.position - transform.position;

	//	v.x = v.z = 0.0f;
	//	transform.LookAt( Camera.main.transform.position - v );
	//	transform.Rotate( 0, 180, 0 );
	//}
	// Use this for initialization
	public virtual void Increment( GameObject repairer, bool? isLefthand) {
		//print( "incrememnt called with index of " + index );

		index++;

		if (repairer) {
			repairerInstance = repairer;
			//VariableHolder.instance.IncreasePlayerScore(repairer, VariableHolder.PlayerScore.ScoreType.Repairs, transform.position);
			//print("increment with repairer, about to call start trail, is left hand is " + isLefthand);
			repairer.GetComponent<Player>().StartTrail(isLefthand);
		}

		if ( index < transform.childCount ) {
			deckDmg.DisableRepairNode( index - 1 );
			//print("index in range");
			transform.GetChild( index ).gameObject.SetActive( true );
			deckDmg.EnableRepairNode( index );
		} else if ( index == transform.childCount ) {
			
			GetComponentInParent<RepairDeckTrigger>().repairPattern = null;


			if ( repairer ) {
				VariableHolder.instance.IncreasePlayerScore( repairer, VariableHolder.PlayerScore.ScoreType.Repairs, transform.position );
				repairer.GetComponent<Player>().DisableTrailRenderer();
			}

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
