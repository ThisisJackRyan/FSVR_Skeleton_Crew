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
		print("opening");
		PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		StartCoroutine("PlayEffect");
	}

	[Button]
	void CloseSocket() {
		print("closing socket");
		PropClientSocket.CloseSocket(PhysicalEffect.Wind);

	}

	IEnumerator PlayEffect() {
		yield return new WaitForSeconds(0.5f);
		print("sending message");
		PropClientSocket.SendMessage(new Message(PhysicalEffect.Wind, 1001));
	}

	//IEnumerator PlayCode() {
	//	PropClientSocket.SetupSocketSpecfic( ipAddress );
	//	yield return new WaitForSeconds( 2 );
	//	PropClientSocket.sendMessage( new message( 1001, "Specific" ) );
	//	yield return new WaitForSeconds( 2 );

	//	PropClientSocket.SetupSocketSpecfic( ipAddress );
	//	yield return new WaitForSeconds( 2 );
	//	PropClientSocket.sendMessage( new message( 2001, "Specific" ) );
	//	yield return new WaitForSeconds( 2 );

	//	PropClientSocket.SetupSocketSpecfic( ipAddress );
	//	yield return new WaitForSeconds( 2 );
	//	PropClientSocket.sendMessage( new message( 3001, "Specific" ) );
	//	yield return new WaitForSeconds( 2 );

	//	PropClientSocket.SetupSocketSpecfic( ipAddress );
	//	yield return new WaitForSeconds( 2 );
	//	PropClientSocket.sendMessage( new message( 4001, "Specific" ) );
	//	yield return new WaitForSeconds( 2 );

	//	PropClientSocket.SetupSocketSpecfic( ipAddress );
	//	yield return new WaitForSeconds( 2 );
	//	PropClientSocket.sendMessage( new message( 1002, "Specific" ) );
	//	yield return new WaitForSeconds( 2 );

	//	PropClientSocket.SetupSocketSpecfic( ipAddress );
	//	yield return new WaitForSeconds( 2 );
	//	PropClientSocket.sendMessage( new message( 2002, "Specific" ) );
	//	yield return new WaitForSeconds( 2 );

	//	PropClientSocket.SetupSocketSpecfic( ipAddress );
	//	yield return new WaitForSeconds( 2 );
	//	PropClientSocket.sendMessage( new message( 3002, "Specific" ) );
	//	yield return new WaitForSeconds( 2 );

	//	PropClientSocket.SetupSocketSpecfic( ipAddress );
	//	yield return new WaitForSeconds( 2 );
	//	PropClientSocket.sendMessage( new message( 4002, "Specific" ) );
	//	yield return new WaitForSeconds( 2 );
	//}
}
