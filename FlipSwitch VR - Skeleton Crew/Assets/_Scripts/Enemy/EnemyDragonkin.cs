using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Opsive.ThirdPersonController.Wrappers;

public class EnemyDragonkin : NetworkBehaviour {

	public bool isRanged;

	public GameObject deathParticles;

	private GameObject myPosition;                  // Stores the key where they're located for number of enemy/position tracking.
	private bool canBeDamaged = true;
	
	private void OnCollisionEnter(Collision other) {
		if (!isServer)
			return;

		if (other.gameObject.tag == "Weapon") {
			if (other.gameObject.GetComponent<Weapon>().data.type == WeaponData.WeaponType.Melee) {
				if (other.gameObject.GetComponent<Weapon>().isBeingHeldByPlayer && canBeDamaged) {
					canBeDamaged = false;
					GetComponent<CharacterHealth>().Damage(other.gameObject.GetComponent<Weapon>().data.damage, other.contacts[0].point, (other.impulse / Time.fixedDeltaTime), other.gameObject.GetComponent<Weapon>().playerWhoIsHolding.transform.root.gameObject);
					Invoke("AllowDamage", 1f);
				}
			}
		} else if (other.gameObject.tag == "BulletPlayer" || other.gameObject.tag == "CannonBallPlayer") {
			canBeDamaged = false;
			GetComponent<CharacterHealth>().Damage(other.gameObject.GetComponent<SCProjectile>().damage, other.contacts[0].point, (other.impulse / Time.fixedDeltaTime));
			Invoke("AllowDamage", 1f);
		}
	}

	public void DestroyMe() {
		if (!isServer) {
			return;
		}

		//RpcSpawnDeathParticles();
		var g = Instantiate(deathParticles, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity);
		NetworkServer.Spawn(g);
		OnDeath();
		NetworkServer.Destroy(gameObject);
	}

	public void SetMyPosition(GameObject g ) {
		myPosition = g;
	}

	public GameObject GetMyPosition() {
		return myPosition;
	}

	public void OnDeath() {
		if ( !isServer ) {
			return;
		}

		if ( isRanged ) {
			VariableHolder.instance.enemyRangedPositions[myPosition] = false;
		} else {
			VariableHolder.instance.enemyMeleePositions[myPosition] = false;
		}
	}
}
