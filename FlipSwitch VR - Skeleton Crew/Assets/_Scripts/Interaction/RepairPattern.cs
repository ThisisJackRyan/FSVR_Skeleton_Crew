using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairPattern : MonoBehaviour {

	internal int index = 0;
	public DamagedObject dmgObj;

	public void Init() {
		print( name + " init called" );

		index = 0;
		for(int i = index + 1; i < transform.childCount; i++ ) {
			transform.GetChild( i ).gameObject.SetActive( false );
		}

		Increment();
		//print("awerrrrrrrrrrrrrrrrrrrrrrrgwer");
	}

	// Use this for initialization
	public virtual void Increment () {
		print( "incrememnt called with index of " + index );
		dmgObj.DisableRepairNode( index );
		index++;
		if (index < transform.childCount) {
			print("index in range");
			transform.GetChild( index ).gameObject.SetActive(true);
			dmgObj.EnableRepairNode( index );
		} else if(index == transform.childCount) {
			print( "index is last child" );

			//last node hit
			//run repair code
			if ( dmgObj.ChangeHealth( 20, false ) < dmgObj.maxHealth) {
				//index = 0;
				//Increment();
				print("healing, not full health");
				Init();
			} else {
				//full healed
				print( "fully healed" );
				GetComponentInParent<RepairTrigger>().repairPattern = null;
				gameObject.SetActive( false );
			}
		}
	}

	private void OnDisable() {
		print(name + " disabled, turning off nodes");
		
		for ( int i = 1; i < transform.childCount; i++ ) {
			transform.GetChild( i ).gameObject.SetActive( false );
		}
	}




}
