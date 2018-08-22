using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crystal : MonoBehaviour {

	public Transform[] otherCrystals;
	public int health = 1;

	private void OnTriggerEnter(Collider other) {
		//Debug.LogWarning(other.tag + " hit crystal");

		if (other.tag == "BulletPlayer") {
			//destroy gameobject
			//print("in the bullet player if");
			HitByMusket(other.gameObject);
		} else if (other.tag == "CannonBallPlayer") {
			//destroy root
			//print("in the cannon player if");
			HitByCannon(other.gameObject);
		}
	}

	void HitByMusket(GameObject bullet) {
		Destroy(bullet);

		health--;
		if (health <= 0) {
			//print("called rpc bullet");
			transform.root.GetComponent<PathFollower>().DestroyCrystal(gameObject);
		}
	}

	void HitByCannon(GameObject bullet) {
		//print("called rpc cannon");
		Destroy(bullet);

		foreach (var t in otherCrystals) {
			transform.root.GetComponent<PathFollower>().DestroyCrystal( t.gameObject );
		}
	}
}