using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairPattern : MonoBehaviour {

	private int index = 0;
	public DamagedObject dmgObj;

	private void Start() {
		Init();
	}

	public void Init() {
		index = 0;
		for(int i = index + 1; i < transform.childCount; i++ ) {
			transform.GetChild( i ).gameObject.SetActive( false );
		}

		Increment();
		//print("awerrrrrrrrrrrrrrrrrrrrrrrgwer");
	}

	// Use this for initialization
	public void Increment () {
		//print( "incrememnt called with index of " + index );
		index++;
		if (index < transform.childCount) {
			transform.GetChild( index ).gameObject.SetActive(true);

		} else if(index == transform.childCount) {
			//last node hit
			//run repair code
			if ( dmgObj.ChangeHealth( 20, false ) < dmgObj.maxHealth) {
				index = 0;
				Increment();
			} else {
				//full healed
				gameObject.SetActive( false );
			}
		}
	}
	

}
