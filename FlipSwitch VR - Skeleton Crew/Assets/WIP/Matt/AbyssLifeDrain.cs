using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbyssLifeDrain : MonoBehaviour {

	public float damageRate = 2;
	public int damagePerTick = 5;


	float timer = 0;
	bool active = false;

	private void OnTriggerStay( Collider other ) {
		if ( other.transform.root.tag == "Player" && active) {
			timer += Time.deltaTime;

			if ( timer >= damageRate) {
				timer = 0;
				other.GetComponent<ScriptSyncPlayer>().ChangeHealth(damagePerTick);
			}
		}
	}

	private void OnTriggerEnter( Collider other ) {
		if ( other.transform.root.tag == "Player" ) {

			timer = 0;
			active = true;
		}
	}

	private void OnTriggerExit( Collider other ) {
		active = false;
	}
}
