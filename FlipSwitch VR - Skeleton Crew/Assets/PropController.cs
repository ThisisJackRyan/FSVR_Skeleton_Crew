using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropController : MonoBehaviour {


	static PropController instance;
	public static PropController Instance {
		get {
			if (instance == null) {
				var g = new GameObject( "PropController" );
				instance = g.AddComponent<PropController>();
				return instance;
			} else {
				return instance;
			}
		}
	}

	public void ActivateProp(Prop prop) {
		switch ( prop ) {
			case Prop.WindOff:
				StartCoroutine( SendMessages(PhysicalEffect.Wind, new int[] {1002, 2002, 3002, 4002 } ));
				break;
			case Prop.WindLow:
				StartCoroutine( SendMessages( PhysicalEffect.Wind, new int[] { 1001, 2002, 3001, 4002 } ) );

				break;
			case Prop.WindMed:
				StartCoroutine( SendMessages( PhysicalEffect.Wind, new int[] { 1002, 2001, 3002, 4001 } ) );

				break;
			case Prop.WindHigh:
				StartCoroutine( SendMessages( PhysicalEffect.Wind, new int[] { 1001, 2001, 3001, 4001 } ) );
				break;
			case Prop.CannonLeftOne:
				StartCoroutine( SendMessage( PhysicalEffect.CannonLeft, 1001 ) );

				break;
			case Prop.CannonLeftTwo:
				StartCoroutine( SendMessage( PhysicalEffect.CannonLeft, 2001 ) );

				break;
			case Prop.CannonLeftThree:
				StartCoroutine( SendMessage( PhysicalEffect.CannonLeft, 3001 ) );

				break;
			case Prop.CannonRightOne:
				StartCoroutine( SendMessage( PhysicalEffect.CannonRight, 1001 ) );

				break;
			case Prop.CannonRightTwo:
				StartCoroutine( SendMessage( PhysicalEffect.CannonRight, 2001 ) );

				break;
			case Prop.CannonRightThree:
				StartCoroutine( SendMessage( PhysicalEffect.CannonRight, 3001 ) );

				break;
			default:
				break;
		}
	}

	IEnumerator SendMessage(PhysicalEffect effect, int code) {
		print( "opening" );
		PropClientSocket.OpenSocket( effect );
		yield return new WaitForSeconds( 0.5f );
		print( "triggering" );
		PropClientSocket.SendMessage( new Message( effect, code ) );
	}

	IEnumerator SendMessages( PhysicalEffect effect, int[] codes ) {
		for ( int i = 0; i < codes.Length; i++ ) {
			print( "opening" );
			PropClientSocket.OpenSocket( effect );
			yield return new WaitForSeconds( 0.5f );
			print( "triggering code " + i );
			PropClientSocket.SendMessage( new Message( effect, codes[i] ) );
		}
	}
}
