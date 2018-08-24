using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairPattern : MonoBehaviour {

    // NOTES FOR MATT: 
    // Never disabling the path image itself throughout the increment process.
    // Possibly getting multiple patterns enabled at one time? Or different ones for client and server, 
    // and that's causing all sorts of issues with getting things desynced between client and server.
    // Need to test the int syncvar in damaged object to ensure that it's selecting the same pattern on both
    // Client and server. Server should be setting the int, then syncvar hooking that out.
    
    // I've added in a few things, but I have no real way to test. Look through changes to the script and see.
    // Should just be some isServer checks on RPCs to make sure things aren't being ran twice on server
    // And an onenable method in the RepairTrigger disabling everything besides the particles once the repair sphere is set to active (hopefully), 
    // not sure how that will work with network. Know we had issues with something similar before.

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
				print( "fully healed" ); //null ref here?
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
