using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingRockPattern : RepairPattern {

	public FloatingRock[] floatingRocks;
	public int timeToCast = 2;
	int castRun = 0;

	void OnEnable() {
		castRun = 0;
	}

	// Use this for initialization
	public override void Increment() {
		print( "incrememnt called with index of " + index + " with casting count at "  + castRun + " and cast times needed " + timeToCast  );
		index++;
		if ( index < transform.childCount ) {
			transform.GetChild( index ).gameObject.SetActive( true );

		} else if ( index == transform.childCount ) {
			//last node hit
			//run repair code
			castRun++;
			if ( castRun < timeToCast ) {
				index = 0;
				Increment();
			} else {
				//full healed
				foreach (var rock in floatingRocks) {
					rock.RaiseRock();
				}
				castRun = 0;
				gameObject.SetActive(false);
			}
		}
	}
}
