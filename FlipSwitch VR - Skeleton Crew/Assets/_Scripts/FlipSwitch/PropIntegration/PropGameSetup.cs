using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class PropGameSetup : MonoBehaviour {

	public string ipAddress = "192.168.1.12";
	public int message;


	// Use this for initialization
	[Button]
	void StartSocket () {
		PropClientSocket.SetupSocketSpecfic(ipAddress);
	}

	[Button]
	void TriggerPiOn() {
		PropClientSocket.sendMessage( new message(4001, "Specific" ) );
	}

	[Button]
	void TriggerPiOff() {
		PropClientSocket.sendMessage( new message( 4002, "Specific" ) );

	}

	[Button]
	void TriggerPiAllInOne() {
		StartCoroutine("PlayCode");
	}

	IEnumerator PlayCode() {
		PropClientSocket.SetupSocketSpecfic( ipAddress );
		yield return new WaitForSeconds( 2 );
		PropClientSocket.sendMessage( new message( 1001, "Specific" ) );
		yield return new WaitForSeconds( 2 );

		PropClientSocket.SetupSocketSpecfic( ipAddress );
		yield return new WaitForSeconds( 2 );
		PropClientSocket.sendMessage( new message( 2001, "Specific" ) );
		yield return new WaitForSeconds( 2 );

		PropClientSocket.SetupSocketSpecfic( ipAddress );
		yield return new WaitForSeconds( 2 );
		PropClientSocket.sendMessage( new message( 3001, "Specific" ) );
		yield return new WaitForSeconds( 2 );

		PropClientSocket.SetupSocketSpecfic( ipAddress );
		yield return new WaitForSeconds( 2 );
		PropClientSocket.sendMessage( new message( 4001, "Specific" ) );
		yield return new WaitForSeconds( 2 );

		PropClientSocket.SetupSocketSpecfic( ipAddress );
		yield return new WaitForSeconds( 2 );
		PropClientSocket.sendMessage( new message( 1002, "Specific" ) );
		yield return new WaitForSeconds( 2 );

		PropClientSocket.SetupSocketSpecfic( ipAddress );
		yield return new WaitForSeconds( 2 );
		PropClientSocket.sendMessage( new message( 2002, "Specific" ) );
		yield return new WaitForSeconds( 2 );

		PropClientSocket.SetupSocketSpecfic( ipAddress );
		yield return new WaitForSeconds( 2 );
		PropClientSocket.sendMessage( new message( 3002, "Specific" ) );
		yield return new WaitForSeconds( 2 );

		PropClientSocket.SetupSocketSpecfic( ipAddress );
		yield return new WaitForSeconds( 2 );
		PropClientSocket.sendMessage( new message( 4002, "Specific" ) );
		yield return new WaitForSeconds( 2 );
	}
}
