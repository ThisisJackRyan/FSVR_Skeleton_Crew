using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;
using System;
using Random = UnityEngine.Random;

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
	[Tooltip( "The hit particles to play when hit" )] public GameObject[] hitParticles;


	#endregion

	private void OnHealthChange(int n) {
		if (n < health) {

			for ( int i = 0; i < hitParticles.Length; i++ ) {
				hitParticles[i].SetActive( true );
				var particles = hitParticles[i].GetComponent<ParticleSystem>();
				//particles.Simulate( 0, true, true );
				foreach ( ParticleSystem ps in particles.GetComponentsInChildren<ParticleSystem>() ) {
					particles.Simulate( 0, true, true );
				}
				particles.Play();
				print( particles.name + " should be emiitng" );
			}
			Invoke( "TurnOffHit", 2.0f );

			if (isServer) { 
				int rng = Random.Range( 0, hitSounds.Length );
				GetComponent<AudioSource>().PlayOneShot(hitSounds[rng]);
				RpcPlayHitSound( rng );
			}

		}

		health = n;

		if (health <= 0) {
			if ( tutorialGuard ) {
				print( "tut guard killed" );
				Captain.enemiesKilled[this] = true;
				Captain.instance.CheckEnemiesKilled();
			}

			Destroy( gameObject );
			Instantiate( deathParticles, new Vector3( transform.position.x, transform.position.y + 0.5f, transform.position.z ), Quaternion.identity );

		}
	}

	public AudioClip[] hitSounds;

	[ClientRpc]
	private void RpcPlayHitSound( int rng ) {
		if (isServer) {
			return;
		}

		GetComponent<AudioSource>().PlayOneShot( hitSounds[rng] );
	}

	public void KillMe() {
		if ( isServer )
			ChangeHealth( maxHealth );
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

	void TurnOffHit() {
		for ( int i = 0; i < hitParticles.Length; i++ ) {
			hitParticles[i].SetActive( false );
		}
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