using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonFuse : MonoBehaviour {

    public Cannon cannonScript;
	float timer = 0;
	bool active = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Fire" && !cannonScript.GetIsFiring() && cannonScript.isServer)
        {
			print("resetting timer");
			timer = 0;
			active = true;
		}
    }

	private void OnTriggerStay( Collider other ) {
		if ( other.tag == "Fire" && !cannonScript.GetIsFiring() && cannonScript.isServer && active) {
			timer += Time.deltaTime;
			print( "increasing timer " + timer );

			if ( timer >= 1 ) {
				active = false;
				other.GetComponentInParent<Weapon>().owningPlayerCannonScript.Fire( cannonScript.gameObject );
				print( "timer reached, should fire" );

			}
		}
	}	

	private void OnTriggerExit( Collider other ) {
		if ( other.tag == "Fire" && !cannonScript.GetIsFiring() ) {
			active = false;
			print( "trigger exit" );

		}
	}
}
