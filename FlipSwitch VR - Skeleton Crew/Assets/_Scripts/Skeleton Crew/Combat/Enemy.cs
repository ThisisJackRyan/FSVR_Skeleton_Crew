using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using BehaviorDesigner.Runtime;
using System;
using Random = UnityEngine.Random;
using Opsive.ThirdPersonController.Wrappers;
using UnityEngine.AI;

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
	private int myAvoidance;
	public GameObject lastWeaponDamagedMe;

	public GameObject deathParticles;
	public bool tutorialGuard = false;
    public bool rangedUnit = false;
	public bool ratkin = false;
    public GameObject rangedTeleTarget;
	public PrimaryItemType primaryItemType;
	[Tooltip( "The hit particles to play when hit" )] public GameObject[] hitParticles;
    [SyncVar] public GameObject boardingPartyShip;
	private GameObject playerWhoLastHitMe;
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

    public void DestroyMe() {
        if (!isServer) {
            return;
        }
        if (tutorialGuard) {
            //print( "tut guard killed" );
            Captain.enemiesKilled[this] = true;
            Captain.instance.CheckEnemiesKilled();
        }

		// Put score death stuff here using playerWhoLastHitMe
		VariableHolder.PlayerScore.ScoreType scoreType = (ratkin) ? VariableHolder.PlayerScore.ScoreType.RatkinKills: VariableHolder.PlayerScore.ScoreType.SkeletonKills;
		VariableHolder.instance.IncreasePlayerScore( playerWhoLastHitMe.transform.root.gameObject, scoreType, transform.position );


		var g = Instantiate(deathParticles, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity);
		NetworkServer.Spawn(g);

		if (rangedUnit) {
			VariableHolder.instance.RemoveRangedUnit();
			VariableHolder.instance.enemyRangedPositions[rangedTeleTarget] = false;
		}

		NetworkServer.Destroy(gameObject);
	}

    public GameObject teleportParticles;
	internal void TellCaptainIveBoarded() {
		if (!isServer) {
			Captain.instance.CrewmanHaveBoarded();
        } else {
            var g = Instantiate(teleportParticles, transform.position, Quaternion.identity);
            NetworkServer.Spawn(g);
        }
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
		if (!ratkin) {
			if (!isServer) {
				if (boardingPartyShip) {
					transform.parent = boardingPartyShip.transform;
				}

				return;
			}
			//GetComponent<Inventory>().EquipItem(primaryItemTypes[temp]);
			GetComponent<BehaviorTree>().SetVariableValue("weaponType", primaryItemType);
			//RpcEquipItem(itemToEquip);
		}
    }

    [ClientRpc]          
    private void RpcEquipItem(int toEquip) {
        if (isServer) {
            return;
        }

        GetComponent<Inventory>().EquipItem(toEquip);

    }

	private void OnCollisionEnter(Collision other) {
		if (!isServer)
			return;

		if (other.gameObject.tag == "Weapon") {
			if (other.gameObject.GetComponent<Weapon>().data.type == WeaponData.WeaponType.Melee) {
				if (other.gameObject.GetComponent<Weapon>().isBeingHeldByPlayer && canBeDamaged) {
					playerWhoLastHitMe = other.gameObject.GetComponent<Weapon>().playerWhoIsHolding;
					if (!ratkin) {
						canBeDamaged = false;
						GetComponent<CharacterHealth>().Damage(other.gameObject.GetComponent<Weapon>().data.damage, other.contacts[0].point, (other.impulse / Time.fixedDeltaTime), other.gameObject.GetComponent<Weapon>().playerWhoIsHolding.transform.root.gameObject);
						Invoke("AllowDamage", 1f);
					} else {
						DestroyMe();
					}
				}
			}
		} else if (other.gameObject.tag == "BulletPlayer" || other.gameObject.tag == "CannonBallPlayer") {
			playerWhoLastHitMe = other.gameObject.GetComponent<SCProjectile>().playerWhoFired;

			if (!ratkin) {
				canBeDamaged = false;
				GetComponent<CharacterHealth>().Damage(other.gameObject.GetComponent<SCProjectile>().damage, other.contacts[0].point, (other.impulse / Time.fixedDeltaTime));
				//todo PLAYER SCORE INTEGRATION FOR PROJECTILE
				//VariableHolder.instance.IncreasePlayerScore( other.gameObject.GetComponent<SCProjectile>().playerWhoFired, VariableHolder.PlayerScore.ScoreType.SkeletonKills, transform.position );

				Invoke( "AllowDamage", 1f);
			} else {
				DestroyMe();
			}
        }

		if(other.gameObject.GetComponent<NavMeshAgent>() && GetComponent<NavMeshAgent>()) {
			if(other.transform.GetSiblingIndex() > transform.GetSiblingIndex()) {
				myAvoidance = GetComponent<NavMeshAgent>().avoidancePriority;
				GetComponent<NavMeshAgent>().avoidancePriority = 0;
				Invoke("RevertAvoidance", 1.5f);
			}
		}
	}

	private void RevertAvoidance() {
		GetComponent<NavMeshAgent>().avoidancePriority = myAvoidance;
	}

	private void TurnOffHit() {
		for ( int i = 0; i < hitParticles.Length; i++ ) {
			hitParticles[i].SetActive( false );
		}
	}

	public void AllowDamage() {
		CancelInvoke();
		canBeDamaged = true;
	}

	public bool GetCanBeDamaged() {
		return canBeDamaged;
	}

	public GameObject PlayerWhoKilledMe() {
		return playerWhoLastHitMe;
	}
}