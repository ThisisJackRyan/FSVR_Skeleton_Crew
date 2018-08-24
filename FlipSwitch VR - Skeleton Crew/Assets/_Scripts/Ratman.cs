using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;

public class Ratman : NetworkBehaviour {

	[SyncVar( hook = "OnHealthChange" )] public int health = 100;
	public GameObject rat;
	public bool isOnTheLeft;
	int maxHealth = 100;

	void OnHealthChange( int n ) {
		if ( isServer )
			return;

		if ( n < health ) {
			if ( n >= 0 ) {
				if ( isServer ) {
					int rng = Random.Range( 0, hitSounds.Length );
					GetComponent<AudioSource>().PlayOneShot( hitSounds[rng] );
					RpcPlayHitSound( rng );
				}
			}
		}

		health = n;

		if ( health <= 0 )
			KillRatman();
	}

	void Start() {
		VariableHolder.instance.ratmenPositions.Add( gameObject, isOnTheLeft );
		if ( isServer ) {
			//  print(name + " enabled server check");
			Captain.ratmenRespawned.Add( this, false );
			ChangeHealth( health );
		} else {
			OnHealthChange( health );
			rat.GetComponent<BehaviorTree>().enabled = false;
		}
	}

	public AudioClip[] hitSounds;

	[ClientRpc]
	private void RpcPlayHitSound( int rng ) {
		if ( isServer ) {
			return;
		}

		GetComponent<AudioSource>().PlayOneShot( hitSounds[rng] );
	}

	public void KillMe() {
		if ( isServer )
			ChangeHealth( maxHealth );
	}

	public void Respawn( Vector3 spawnPos ) {
		ChangeHealth( maxHealth, false );
		rat.transform.position = spawnPos;
		rat.SetActive( true );
		rat.GetComponent<BehaviorTree>().SetVariableValue( "MoveToCannon", true );
		// ratAnim.enabled = true;
		Captain.ratmenRespawned[this] = true;
		Captain.instance.CheckRatmenRespawns();

		RpcRespawn( spawnPos );
	}

	[ClientRpc]
	void RpcRespawn( Vector3 spawnPos ) {
		if ( isServer )
			return;

		rat.transform.position = spawnPos;
		rat.SetActive( true );
	}

	public int GetHealth() {
		return health;
	}

	public int ChangeHealth( int amount, bool damage = true ) {
		if ( damage ) {
			health -= Mathf.Abs( amount );
			health = ( health < 0 ) ? 0 : health;
		} else {
			health += Mathf.Abs( amount );
			health = ( health > maxHealth ) ? maxHealth : health;
		}

		if ( health == 0 ) {
			KillRatman();
		}

		return health;
	}

	void KillRatman() {
		rat.SetActive( false );
		//ratAnim.enabled = false;
		HatchActivator.EnableHatch( isOnTheLeft );
	}
}
