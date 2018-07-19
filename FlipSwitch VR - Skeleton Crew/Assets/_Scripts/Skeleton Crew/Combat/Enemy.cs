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

    private bool canBeDamaged;
    public GameObject lastWeaponDamagedMe;
    
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
                canBeDamaged = false;
                health -= other.GetComponent<Weapon>().data.damage;
                if (health <= 0) {
                    Destroy(gameObject);
                }
                Invoke("AllowDamage", 3.5f);
            }
        } else if (other.tag == "BulletPlayer" || other.tag == "CannonBallPlayer") {
            health -= other.GetComponent<SCProjectile>().damage;

            if (health <= 0) {
                Destroy(gameObject);
            }
        }
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