using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;
using System;
using Random = UnityEngine.Random;
using Opsive.ThirdPersonController.Wrappers;       

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: Enemy
/// </summary>
public class Enemy : NetworkBehaviour {
	#region Fields
    
	private bool canBeDamaged = true;
	public GameObject lastWeaponDamagedMe;

	public GameObject deathParticles;
	public bool tutorialGuard = false;
    public bool rangedUnit = false;
	[Tooltip( "The hit particles to play when hit" )] public GameObject[] hitParticles;
    [SyncVar] public GameObject boardingPartyShip;

	#endregion

    private void OnBoardingShipChange(GameObject n) {

        if (isServer) {
            return;
        }
        boardingPartyShip = n;
        transform.parent = boardingPartyShip.transform;
    }

    public void UnParentMe() {
        if (!isServer) {
            return;
        }

        transform.parent = null;
        RpcUnParentMe();
    }

    [ClientRpc]
    private void RpcUnParentMe() {
        if (isServer) {
            return;
        }

        transform.parent = null;
    }

    private void DestroyMe() {
        if (!isServer) {
            return;
        }
        if (tutorialGuard) {
            //print( "tut guard killed" );
            Captain.enemiesKilled[this] = true;
            Captain.instance.CheckEnemiesKilled();
        }

        RpcSpawnDeathParticles();
        EnemyUnitDeath();
        NetworkServer.Destroy(gameObject);
    }

    public void PlayHitParticles() {
        //print("play hit particles called");

        foreach(var p in hitParticles) {
            p.SetActive(true);
            var par = p.GetComponent<ParticleSystem>();
            par.Simulate(0, true, true);
            par.Play();
        }

        Invoke("TurnOffHit", 1.0f);
    }

	private void Start() {

        //Opsive.ThirdPersonController.EventHandler.RegisterEvent("OnHealthAmountChange", PlayHitParticles);

        if (!isServer) {
            if (boardingPartyShip) {
                transform.parent = boardingPartyShip.transform;
            }

            return;
        }

        int itemToEquip;
                            
        itemToEquip = Random.Range(0, GetComponent<Inventory>().DefaultLoadout.Length);    
        GetComponent<Inventory>().EquipItem(itemToEquip);

        RpcEquipItem(itemToEquip);

    }

    [ClientRpc]          
    private void RpcEquipItem(int toEquip) {
        if (isServer) {
            return;
        }

        GetComponent<Inventory>().EquipItem(toEquip);

    }

    public void EnemyUnitDeath() {
        if (rangedUnit) {
            VariableHolder.instance.RemoveRangedUnit();
        }
    }

	private void OnCollisionEnter(Collision other) {
		if (!isServer)
			return;

		if (other.gameObject.tag == "Weapon") {
			if (other.gameObject.GetComponent<Weapon>().data.type == WeaponData.WeaponType.Melee) {
				// todo: test that enemies are only being damaged by melee weapons being held by player
				if (other.gameObject.GetComponent<Weapon>().isBeingHeldByPlayer && canBeDamaged) {
					canBeDamaged = false;
                    GetComponent<CharacterHealth>().Damage(other.gameObject.GetComponent<Weapon>().data.damage, other.contacts[0].point, (other.impulse / Time.fixedDeltaTime));
					Invoke("AllowDamage", 1f);
				}
			}
		} else if (other.gameObject.tag == "BulletPlayer" || other.gameObject.tag == "CannonBallPlayer") {
            canBeDamaged = false;
            GetComponent<CharacterHealth>().Damage(other.gameObject.GetComponent<SCProjectile>().damage, other.contacts[0].point, (other.impulse / Time.fixedDeltaTime));   
            Invoke("AllowDamage", 1f);
        }
	}

	private void TurnOffHit() {
		for ( int i = 0; i < hitParticles.Length; i++ ) {
			hitParticles[i].SetActive( false );
		}
	}

	public int maxHealth = 100;


	[ClientRpc]
	void RpcSpawnDeathParticles() {
		if (isServer) {
			return;
		}

		Instantiate(deathParticles, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity);
	}

	public void AllowDamage() {
		CancelInvoke();
		canBeDamaged = true;
	}

	public bool GetCanBeDamaged() {
		return canBeDamaged;
	}
}