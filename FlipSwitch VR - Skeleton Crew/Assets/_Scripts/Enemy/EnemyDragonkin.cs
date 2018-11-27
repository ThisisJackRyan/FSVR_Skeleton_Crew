using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Opsive.ThirdPersonController.Wrappers;
using BehaviorDesigner.Runtime;

public class EnemyDragonkin : NetworkBehaviour {

	public bool isRanged;

	public GameObject deathParticles;
	public PrimaryItemType primaryItemType;
	public GameObject teleportParticles;
	[Tooltip("The hit particles to play when hit")] public GameObject[] hitParticles;
	private GameObject myPosition;                  // Stores the key where they're located for number of enemy/position tracking.
	private bool canBeDamaged = true;
	private GameObject playerWhoLastHitMe;

	private void Start() {
		if (isServer) {
			//print("should be setting the weapon type");
			GetComponent<BehaviorTree>().SetVariableValue("weaponType", primaryItemType);	
		}
	}

	private void OnCollisionEnter(Collision other) {
		if (!isServer)
			return;

		if (other.gameObject.tag == "Weapon") {
			if (other.gameObject.GetComponent<Weapon>().data.type == WeaponData.WeaponType.Melee) {
				if (other.gameObject.GetComponent<Weapon>().isBeingHeldByPlayer && canBeDamaged) {
					playerWhoLastHitMe = other.gameObject.GetComponent<Weapon>().playerWhoIsHolding;

					canBeDamaged = false;
					GetComponent<CharacterHealth>().Damage(other.gameObject.GetComponent<Weapon>().data.damage, other.contacts[0].point, (other.impulse / Time.fixedDeltaTime));
					Invoke("AllowDamage", 1f);
				}
			}
		} else if (other.gameObject.tag == "BulletPlayer" || other.gameObject.tag == "CannonBallPlayer") {
			playerWhoLastHitMe = other.gameObject.GetComponent<SCProjectile>().playerWhoFired;


			canBeDamaged = false;
			GetComponent<CharacterHealth>().Damage(other.gameObject.GetComponent<SCProjectile>().damage, other.contacts[0].point, (other.impulse / Time.fixedDeltaTime));
			//todo PLAYER SCORE INTEGRATION FOR PROJECTILE
			VariableHolder.instance.IncreasePlayerScore( other.gameObject.GetComponent<SCProjectile>().playerWhoFired, VariableHolder.PlayerScore.ScoreType.DragonkinKills, transform.position );

			Invoke( "AllowDamage", 1f);
		}
	}

	public void PlayHitParticles() {
		//print("play hit particles called");

		foreach (var p in hitParticles) {
			p.SetActive(true);
			var par = p.GetComponent<ParticleSystem>();
			par.Simulate(0, true, true);
			par.Play();
		}

		Invoke("TurnOffHit", 1.0f);
	}

	private void TurnOffHit() {
		for (int i = 0; i < hitParticles.Length; i++) {
			hitParticles[i].SetActive(false);
		}
	}

	public void TeleportToDeath() {
		if (!isServer) {
			return;
		}

		GameObject p = Instantiate(teleportParticles, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity);
		NetworkServer.Spawn(p);
		NetworkServer.Destroy(gameObject);
	}

	// Called by death animation event
	public void DestroyMe() {
		if (!isServer) {
			return;
		}

		if (isRanged) {
			VariableHolder.instance.enemyRangedPositions[myPosition] = false;
		} else {
			VariableHolder.instance.enemyMeleePositions[myPosition] = false;
		}

		VariableHolder.instance.IncreasePlayerScore( playerWhoLastHitMe.transform.root.gameObject, VariableHolder.PlayerScore.ScoreType.DragonkinKills, transform.position );

		GameObject d = Instantiate(deathParticles, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity);
		NetworkServer.Spawn(d);
		TeleportToDeath();
	}

	public void SetMyPosition(GameObject g ) {
		myPosition = g;
	}

	public GameObject GetMyPosition() {
		return myPosition;
	}
}
