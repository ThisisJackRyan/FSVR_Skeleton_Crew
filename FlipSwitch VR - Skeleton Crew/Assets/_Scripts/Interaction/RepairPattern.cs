﻿using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class RepairPattern : MonoBehaviour {

   	internal int index = 0;
	public DamagedObject dmgObj;
	public GameObject repairerInstance;
	LineRenderer lr;

	[Button]
	public void Init() {
		//print( name + " init called" );

		index = 0;
		for (int i = index + 1; i < transform.childCount; i++ ) {
			transform.GetChild( i ).gameObject.SetActive( false );
		}

		Increment(null, null);
		//print("awerrrrrrrrrrrrrrrrrrrrrrrgwer");
	}

	Vector3[] nodePoints;
	void OnEnable() {
		lr = GetComponent<LineRenderer>();
		nodePoints = new Vector3[transform.childCount];

		for (int i = 0; i < transform.childCount; i++) {
			if (i == 0) {
				continue;

			}

			nodePoints[i - 1] = transform.GetChild(i).transform.localPosition;
		}

		lr.positionCount = transform.childCount - 1;
		lr.SetPositions(nodePoints);
	}

	[Button]
	void IncrementButton() {
		Increment(null, null);

	}

	void IncrementLineRenderer() {
		lr.positionCount = index;
		lr.SetPositions( nodePoints );
	}

	// Use this for initialization
	public virtual void Increment (GameObject repairer, bool? isLefthand) {
		//print("incrememnt called with index of " + index + " and repairer " + repairer + " has left hand " + isLefthand);

		dmgObj.DisableRepairNode( index );
		index++;
		IncrementLineRenderer();

		if ( repairer) {
			repairerInstance = repairer;
			//VariableHolder.instance.IncreasePlayerScore(repairer, VariableHolder.PlayerScore.ScoreType.Repairs, transform.position);
			//print("increment with repairer, about to call start trail, is left hand is " + isLefthand);
			repairer.GetComponent<Player>().StartTrail(isLefthand);
		}

		if (index < transform.childCount) {
			//print("index in range");
			transform.GetChild( index ).gameObject.SetActive(true);
			dmgObj.EnableRepairNode( index );


		} else if(index == transform.childCount) {
			//print( "index is last child" );
			if (repairer) {
				VariableHolder.instance.IncreasePlayerScore( repairer, VariableHolder.PlayerScore.ScoreType.Repairs, transform.position );
				repairer.GetComponent<Player>().DisableTrailRenderer();

			}
			//last node hit
			//run repair code
			if ( dmgObj.GetHealth() + 40 < dmgObj.maxHealth) {
				//index = 0;
				//Increment();
				dmgObj.ChangeHealth( 40, false );


				//print("healing, not full health");
				Init();
			} else {				
				GetComponentInParent<RepairTrigger>().repairPattern = null;
				gameObject.SetActive( false );

				dmgObj.ChangeHealth( 40, false );

			}
		}
	}

	private void OnDisable() {
		//print(name + " disabled, turning off nodes");
		
		for ( int i = 1; i < transform.childCount; i++ ) {
			transform.GetChild( i ).gameObject.SetActive( false );
		}
	}
}
