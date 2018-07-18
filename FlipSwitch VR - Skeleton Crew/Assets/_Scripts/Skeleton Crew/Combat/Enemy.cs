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

    [SyncVar(hook = "OnHealthChange")] public int health;
    public int soulCount;
    [Tooltip("souls per second")]
    public int drainRate;
    public WeaponData weapon;
    public Collider weaponCollider;
    public BehaviorTree tree;

    public bool isBoss = false;
    #endregion

    private void OnHealthChange(int n) {
        health = n;
    }

    private void Start() {
        if (weapon.type == WeaponData.WeaponType.Melee && weaponCollider.enabled)
            ToggleWeaponCollider();
    }

    private void OnTriggerEnter(Collider other) {
        if (!isServer)
            return;

        if (other.tag == "Weapon") {
            if (other.GetComponent<Weapon>().data.type == WeaponData.WeaponType.Melee) {
                //print("hit with " + health + " by " + other.GetComponent<Weapon>().data.damage);
                health -= other.GetComponent<Weapon>().data.damage;

                if (health <= 0) {
                    //print("dead");
                    if (isBoss) {
                        EnemySpawner.StopSpawning();
                    }
                    Destroy(gameObject);
                }
            }
        } else if (other.tag == "BulletPlayer" || other.tag == "CannonBallPlayer") {
            //print("hit with " + health + " by " + other.GetComponent<SCProjectile>().damage);
            health -= other.GetComponent<SCProjectile>().damage;

            if (health <= 0) {
                //print("dead");
                Destroy(gameObject);
            }
        }
    }

    public void EnableEnemy() {
        GlobalVariables.Instance.SetVariableValue("EnemiesEnabled", true);
    }

    public void ToggleWeaponCollider() {
        //print(name + " is toggling weapon, it is " + weaponCollider.enabled + " setting it to " + !weaponCollider.enabled);
        weaponCollider.enabled = !weaponCollider.enabled;
    }
}