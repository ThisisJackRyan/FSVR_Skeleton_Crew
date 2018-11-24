using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;

public class DamagedObject : NetworkBehaviour {

	public enum DamageState {
		Full, ThreeQuarter, Half, Quarter, None
	}

    public DamageState CurrentHealthState {
        get {
            return myState;
        }
    }

	DamageState myState;
	[SyncVar( hook = "OnHealthChange" )] int health = 100;
	[SyncVar( hook = "OnPatternIndexChange" )] int rng = -1;
	public GameObject burstEffect;

	[Tooltip( "the number health percent much be at to reach the given damage state. anything below quarter is completely broken." )]
	public int fullAmount = 90, threeQuarterAmount = 75, halfAmount = 50, quarterAmount = 25, maxHealth = 100;

	public Transform fullState, threequarter, halfState, quarterState, deadState;
	public GameObject repairSphere;
	public RepairPattern[] repairPatterns;

	public GameObject dmgParticles, healParticles;
	public AudioClip damageClip, healClip;

    [Button]
    public void HealMe() {
        if (!isServer) {
            return;
        }

        ChangeHealth(maxHealth, false);
    }

    [Button]
    public void KillMe() {
        if (!isServer) {
            return;
        }

        ChangeHealth(maxHealth);
    }

	public void SpawnBurst(Vector3 pos) {
		if (!isServer) {
			return;
		}
		var g = Instantiate(burstEffect, pos, Quaternion.identity);
		NetworkServer.Spawn( g );
	}

	private void OnPatternIndexChange( int n ) {
		if (isServer) {
			return;
		}
		rng = n;

		if (rng != -1) {
			repairPattern = repairPatterns[rng];
			////print("repair patter is now " + repairPattern.name + " on the client damaged object");
		}
	}

	bool firstDamage = true;

	private void OnHealthChange( int n ) {
		if ( !isServer ) {
            //return;
            ////print("health change");
			if ( health > n && n >= 0) {
				if (!firstDamage) {
					GetComponent<AudioSource>().PlayOneShot( damageClip );
				}
				//Instantiate( dmgParticles, transform.position, Quaternion.identity );   
			} else if (health < n) {
				if ( !firstDamage ) {
					GetComponent<AudioSource>().PlayOneShot( healClip );
				}
				//Instantiate( healParticles, transform.position, Quaternion.identity );
			}
		}

		health = n;

		if ( health >= fullAmount ) {
			myState = DamageState.Full;
		} else if ( health >= threeQuarterAmount ) {
			myState = DamageState.ThreeQuarter;
		} else if ( health >= halfAmount ) {
			myState = DamageState.Half;
		} else if ( health >= quarterAmount ) {
			myState = DamageState.Quarter;
		} else {
            myState = DamageState.None;

            if (isServer) {
                if (Captain.instance) {
					//print(name + " has firstDamage at " + firstDamage);
					if (!firstDamage) {
						Captain.instance.AddEventToQueue(Captain.AudioEventType.Cannon);
					} else {
						firstDamage = false;
					}
                }
            }
		}

		if(health < maxHealth ) {
			repairSphere.SetActive( true );
		} else {
			repairSphere.SetActive( false );
		}

		UpdateModel();

	}

	public void InitDamagedObject() {
		if ( isServer ) {
			ChangeHealth( maxHealth );
			Captain.damagedObjectsRepaired.Add( this, false );
		} else if ( isClient ) {
			OnHealthChange( health );
		}
	}

	public RepairPattern repairPattern;

	internal RepairPattern SelectPattern() {
		if ( !isServer ) {
			return null;
		}

		rng = Random.Range( 0, repairPatterns.Length );
		repairPattern = repairPatterns[rng];
		////print( "setting repairPattern to " + repairPattern.name );
		foreach ( var pat in repairPatterns ) {
			pat.gameObject.SetActive( false );
		}

		return repairPattern;
	}

	internal void DisablePatternOnClients() {
		if ( !isServer ) {
			return;
		}

		RpcDisablePattern();
	}

	[ClientRpc]
	private void RpcDisablePattern() {
		if (isServer) {
			return;
		}


		repairPattern.gameObject.SetActive( false );
		if ( health < maxHealth ) {
			repairSphere.transform.GetChild(0).gameObject.SetActive( true );
		}
	}

	internal void SpawnBurst(GameObject burst, Vector3 pos) {
		if (!isServer) {
			return;
		}

		var g = Instantiate(burst, pos, Quaternion.identity);
		NetworkServer.Spawn(g);
	}

	internal void EnablePatternOnClients() {
		if ( !isServer ) {
			return;
		}
		print("Enable Pattern on clients called");
		RpcEnablePattern();
	}

	[ClientRpc]
	private void RpcEnablePattern() {
		print("rpc enable pattern called, pre server check, server states " + isServer + " ");

		if (isServer) {
			return;
		}

		print("rpc enable pattern called");
		repairPattern.gameObject.SetActive( true ); // turns on the pattern gameobject
		repairSphere.transform.GetChild(0).gameObject.SetActive(false); //  disables the particles
	}

	internal void DisableRepairNode( int index ) {
		if ( !isServer ) {
			return;
		}

		if (index != 0) {
			SpawnBurst( repairPattern.transform.GetChild( index ).position );
		}

		RpcDisableNode( index );
	}
	public HapticEvent haptics;

	[ClientRpc]
	private void RpcDisableNode( int index ) {
		if (isServer) {
			return;
		}


		////print( "pattern name: " + repairPattern.name );
		////print( "index received: " + index );
		repairPattern.
			transform.
			GetChild( index ).
			gameObject.
			SetActive( false );
	}

	internal void EnableRepairNode( int index ) {
		if ( !isServer ) {
			return;
		}
		repairPattern.transform.GetChild( 0 ).gameObject.SetActive( true ); // Enables the pattern on server?

		RpcEnableNode( index );
	}

	[ClientRpc]
	private void RpcEnableNode( int index ) {
		if (isServer) {
			return;
		}


		repairPattern.transform.GetChild( 0 ).gameObject.SetActive( true );
		repairPattern.transform.GetChild( index ).gameObject.SetActive( true );
	}

	void UpdateModel() {
		switch ( myState ) {
			case DamageState.Full:
				fullState.gameObject.SetActive( true );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.ThreeQuarter:
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( true );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.Half:
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( true );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.Quarter:
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( true );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.None:
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( true );
				break;
		}

	}

	public int ChangeHealth( int amount, bool damage = true ) {
		////print(name+"change health called");
		if ( !isServer )
			return health;

		if (damage) {
			//health -= Mathf.Abs(amount);
			health = (health - Mathf.Abs( amount ) < 0) ? 0 : health - Mathf.Abs( amount );
			////print(health);
			if (health > 0) {
				if (Time.timeSinceLevelLoad > 2) {
					////print("time since level loaded is " + Time.timeSinceLevelLoad);
					GetComponent<AudioSource>().PlayOneShot(damageClip);
					var g = Instantiate(dmgParticles, transform.position, Quaternion.identity);
					NetworkServer.Spawn(g);
				}
			} else if (health <= 0) {
				if (VariableHolder.instance.cannons.Contains(gameObject)) {
					VariableHolder.instance.cannons.Remove(gameObject);
					GetComponent<AudioSource>().PlayOneShot(damageClip);
					var g = Instantiate(dmgParticles, transform.position, Quaternion.identity);
					NetworkServer.Spawn(g);
				}
			}
		} else {
			health += Mathf.Abs(amount);
			health = (health > maxHealth) ? maxHealth : health;
			GetComponent<AudioSource>().PlayOneShot(healClip);
            var g = Instantiate(healParticles, transform.position, Quaternion.identity);
            NetworkServer.Spawn(g);
        }    

		if ( health >= maxHealth ) {
			Captain.damagedObjectsRepaired[this] = true;
			Captain.instance.CheckDamagedObjects();
		}

		// Changes health on the server
		if (health < maxHealth) {
			repairSphere.SetActive(true);
		} else {
			repairSphere.SetActive(false);
		}

		return health;
	}

	public int GetHealth() {
		return health;
	}
}
