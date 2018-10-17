using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;
using UnityEngine.AI;
//using System;

public class Ratman : NetworkBehaviour {

	[SyncVar( hook = "OnHealthChange" )] public int health = 100;
	public GameObject rat;
	public bool isOnTheLeft;
	int maxHealth = 100;
    public GameObject[] hitParticles;
    public Transform reloadMarker;
    public Animator cannonBarrel;
	public GameObject deathParticles;
    public AudioClip deathSound;
    public AudioClip[] hitSounds;

	[ClientRpc]
	private void RpcPlayDeathSound() {
		if ( isServer ) {
			return;
		}

		GetComponent<AudioSource>().PlayOneShot( deathSound );
	}
    
	void OnHealthChange( int n ) {
		if ( n < health ) {
			if ( n >= 0 ) {
				if ( isServer ) {
					int rng = Random.Range( 0, hitSounds.Length );
					GetComponent<AudioSource>().PlayOneShot( hitSounds[rng] );
					RpcPlayHitSound( rng );
				}
			}

            for (int i = 0; i < hitParticles.Length; i++) {
                hitParticles[i].SetActive(true);
                var particles = hitParticles[i].GetComponent<ParticleSystem>();
                particles.Simulate(0, true, true);

                particles.Play();

            }

            Invoke("TurnOffHit", 1.0f);
        }

		health = n;

		if ( health <= 0 )
			KillRatman();
	}

    public float reloadTimer = 0.5f;
    internal void StartWaitToReload() {
        Invoke("StartReload", reloadTimer);
    }

    void StartReload() {
        GetComponent<Animator>().SetTrigger("Reload");
    }

    private void TurnOffHit() {
        for (int i = 0; i < hitParticles.Length; i++) {
            hitParticles[i].SetActive(false);
        }
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

        for (int i = 0; i < hitParticles.Length; i++) {
            hitParticles[i].SetActive(false);
        }

    }

    public GameObject magicParticles, spellParticles;
    public void EnableMagicParticles() {
        magicParticles.SetActive(true);

        foreach (var p in magicParticles.GetComponentsInChildren<ParticleSystem>()) {
            p.Simulate(0, true, true);
            p.Play();
        }
    }

    public void FireMagicParticles() {
        //turn off magic and spawn spell
        magicParticles.SetActive(false);
            var g = Instantiate(spellParticles, magicParticles.transform.position, Quaternion.identity);
        if (isServer) {
            NetworkServer.Spawn(g);
        }
    }

    public void PlayReload() {
        //StartCoroutine("MoveToPositionThenReload");
        GetComponent<Animator>().SetTrigger("CannonFired");

    }

    IEnumerator MoveToPositionThenReload() {
        GetComponentInChildren<NavMeshAgent>().SetDestination(reloadMarker.position);
        while (!TestAgent()) {
            yield return new WaitForEndOfFrame();
        }

        GetComponent<Animator>().SetTrigger("CannonFired");
        //cannonBarrel.SetTrigger("Reload");
    }

    IEnumerator CheckIfReloadNeeded() {
        print("che4ck if reload started");
        while ((bool)rat.GetComponent<BehaviorTree>().GetVariable("MoveToCannon").GetValue()) {
            print("move to cannon is true");
            yield return new WaitForEndOfFrame();
        }

        print("move to cannon is false, checking if cannon needs reloaded");

        if(cannonBarrel.GetComponentInParent<Cannon>().NeedsReloaded) {
            print("cannon needs reloaded, playing reload");
            PlayReload();
        } else {
            print("cannon barrel says: " + cannonBarrel.GetComponentInParent<Cannon>().NeedsReloaded);
        }
    }

    bool TestAgent() {   
        if (!GetComponentInChildren<NavMeshAgent>().pathPending) {
            if (GetComponentInChildren<NavMeshAgent>().remainingDistance <= GetComponentInChildren<NavMeshAgent>().stoppingDistance) {
                if (!GetComponentInChildren<NavMeshAgent>().hasPath || GetComponentInChildren<NavMeshAgent>().velocity.sqrMagnitude == 0f) {
                    return true;
                }
            }
        }

        return false;
    }

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
        StartCoroutine("CheckIfReloadNeeded");
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
        if (!isServer) {
            return health;
        }

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

		if (!isServer) {

			return;
		}

        if (Time.timeSinceLevelLoad > 5) {

		var g = Instantiate( deathParticles, new Vector3( rat.transform.position.x, rat.transform.position.y + 0.5f, rat.transform.position.z ), Quaternion.identity );
		NetworkServer.Spawn(g);

        Captain.instance.AddEventToQueue(Captain.AudioEventType.Ratmen);

        }
    }
}
