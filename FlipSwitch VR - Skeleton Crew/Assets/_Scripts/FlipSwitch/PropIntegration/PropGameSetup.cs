using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class PropGameSetup : MonoBehaviour {

	public string ipAddress = "190.5f.168.1.105";
	public int message;


	// Use this for initialization
	[Button]
	void StartSocket () {
		print("opening");
		PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		StartCoroutine("PlayCode");
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

	IEnumerator PlayCode() {
		  PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		yield return new WaitForSeconds( 0.5f );
		PropClientSocket.SendMessage( new Message( PhysicalEffect.Wind, 2001) );
		yield return new WaitForSeconds( 0.5f );

		  PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		yield return new WaitForSeconds( 0.5f );
		PropClientSocket.SendMessage( new Message( PhysicalEffect.Wind, 3001 ) );

		yield return new WaitForSeconds( 0.5f );

		  PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		yield return new WaitForSeconds( 0.5f );
		PropClientSocket.SendMessage( new Message( PhysicalEffect.Wind, 4001 ) );

		yield return new WaitForSeconds( 0.5f );

		  PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		yield return new WaitForSeconds( 0.5f );
		PropClientSocket.SendMessage( new Message( PhysicalEffect.Wind, 1001 ) );

		yield return new WaitForSeconds( 0.5f );

		  PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		yield return new WaitForSeconds( 0.5f );
		PropClientSocket.SendMessage( new Message( PhysicalEffect.Wind, 2000 ) );

		yield return new WaitForSeconds( 0.5f );

		  PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		yield return new WaitForSeconds( 0.5f );
		PropClientSocket.SendMessage( new Message( PhysicalEffect.Wind, 3000 ) );

		yield return new WaitForSeconds( 0.5f );

		  PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		yield return new WaitForSeconds( 0.5f );
		PropClientSocket.SendMessage( new Message( PhysicalEffect.Wind, 4000 ) );

		yield return new WaitForSeconds( 0.5f );

		  PropClientSocket.OpenSocket(PhysicalEffect.Wind);
		yield return new WaitForSeconds( 0.5f );
		PropClientSocket.SendMessage( new Message( PhysicalEffect.Wind, 1000 ) );

		yield return new WaitForSeconds( 0.5f );
	}
}
