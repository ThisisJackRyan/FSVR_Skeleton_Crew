using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: Enemy
/// </summary>
public class Enemy : NetworkBehaviour {
	#region Fields

	[SyncVar(hook = "OnHealthChange")]
	public int health;
	public int soulCount;
	[Tooltip("souls per second")]
	public int drainRate;
	public WeaponData weapon;
	public Collider weaponCollider;
	public BehaviorTree tree;

	private bool canBeDamaged;
	public GameObject lastWeaponDamagedMe;

	public GameObject deathParticles;
	public bool tutorialGuard = false;

	#endregion

	private void OnHealthChange(int n) {
		health = n;

		if (health <= 0) {

			if ( tutorialGuard ) {
				print( "tut guard killed" );
				Captain.enemiesKilled[this] = true;
				Captain.instance.CheckEnemiesKilled();
			}
		}
	}

	private void Start() {
		

		if (weapon) {
			if (weapon.type == WeaponData.WeaponType.Melee && weaponCollider.enabled) {
				ToggleWeaponCollider();
			}
		}
	}

	private void OnTriggerEnter(Collider other) {
		if (!isServer)
			return;

		if (other.tag == "Weapon") {
			if (other.GetComponent<Weapon>().data.type == WeaponData.WeaponType.Melee) {
				// todo: test that enemies are only being damaged by melee weapons being held by player
				if (other.GetComponent<Weapon>().isBeingHeldByPlayer) {
					canBeDamaged = false;
					health -= other.GetComponent<Weapon>().data.damage;
					if (health <= 0) {
						Destroy(gameObject);
						RpcSpawnDeathParticles();
					}

					Invoke("AllowDamage", 3.5f);
				}
			}
		} else if (other.tag == "BulletPlayer" || other.tag == "CannonBallPlayer") {
			health -= other.GetComponent<SCProjectile>().damage;

			if (health <= 0) {
				Destroy(gameObject);
			}
		}
	}

	public int ChangeHealth( int amount, bool damage = true ) {
		if ( !isServer )
			return health;

		if ( damage ) {
			health -= Mathf.Abs( amount );
			health = ( health < 0 ) ? 0 : health;
		} else {
			health += Mathf.Abs( amount );
			health = ( health > maxHealth ) ? maxHealth : health;
		}

		return health;
	}

	public int maxHealth = 100;


	[ClientRpc]
	void RpcSpawnDeathParticles() {
		Instantiate(deathParticles, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity);
	}

	public void AllowDamage() {
		CancelInvoke();
		canBeDamaged = true;
	}

	public bool GetCanBeDamaged() {
		return canBeDamaged;
	}

	public void EnableEnemy() {
		GlobalVariables.Instance.SetVariableValue("EnemiesEnabled", true);
	}

	public void ToggleWeaponCollider() {
		weaponCollider.enabled = !weaponCollider.enabled;
	}
}