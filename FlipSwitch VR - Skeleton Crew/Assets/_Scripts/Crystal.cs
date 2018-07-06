using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Crystal : NetworkBehaviour {

	public Transform[] otherCrystals;

	private void OnTriggerEnter( Collider other ) {
		if (!isServer) {
			return;
		}

			Debug.LogWarning(other.tag + " hit crystal");

		if ( other.tag == "BulletPlayer" ) {
			//destroy gameobject
			print("in the bullet player if");
			RpcHitByMusket(other.gameObject);
		} else if (other.tag == "CannonBallPlayer") {
			//destroy root
			print("in the cannon player if");
			RpcHitByCannon(other.gameObject);
		}
	}

	[ClientRpc]
	void RpcHitByMusket( GameObject bullet ) {
		print("called rpc bullet");
		Destroy(bullet);
		Destroy(gameObject);
	}

	[ClientRpc]
	void RpcHitByCannon( GameObject bullet ) {
		print("called rpc cannon");
		Destroy( bullet );

		foreach ( var t in otherCrystals) {
			Destroy( t.gameObject );
		}
	}
}
