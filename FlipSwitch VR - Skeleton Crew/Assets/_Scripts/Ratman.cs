using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Ratman : NetworkBehaviour {

    [SyncVar(hook = "OnHealthChange")] public int health = 100;
    public GameObject rat;
    int maxHealth = 100;

    void OnHealthChange(int n) {
        if (isServer)
            return;

        health = n;

        if (health <= 0)
            KillRatman();
    }

    private void Start() {
        VariableHolder.instance.ratmenPositions.Add(gameObject);
        if (isServer)
            ChangeHealth(health);
        else
            OnHealthChange(health);
    }

    public static void RespawnRatmen(Vector3 spawnPos) {
        print("respawnrats called");
        bool hasRespawned = false;
        bool needsRespawn = false;
        foreach (var item in VariableHolder.instance.ratmenPositions) {
            if (item.GetComponent<Ratman>().GetHealth() <= 0) {
                if (!hasRespawned) {
                    item.GetComponent<Ratman>().Respawn(spawnPos);
                    hasRespawned = true;
                } else {
                    needsRespawn = true;
                    break;
                }
            } else {
                print(item.name + " has health of " + item.GetComponent<Ratman>().GetHealth() + " in respawn");
            }
        }

        if (!needsRespawn) {
            print("doesn't need to respawn, disabling hatches");
            HatchActivator.DisableHatches();
            print(HatchActivator.instance);

            HatchActivator.instance.RpcDisableHatches();
        }

    }

 

    void Respawn(Vector3 spawnPos) {
        ChangeHealth(maxHealth, false);
        rat.transform.position = spawnPos;
        rat.SetActive(true);
        RpcRespawn(spawnPos);
    }

    [ClientRpc]
    void RpcRespawn(Vector3 spawnPos) {
        if (isServer)
            return;

        rat.transform.position = spawnPos;
        rat.SetActive(true);
    }

    public int GetHealth() {
        return health;
    }

    public int ChangeHealth(int amount, bool damage = true) {

        if (damage) {
            health -= Mathf.Abs(amount);
            health = (health < 0) ? 0 : health;
        } else {
            health += Mathf.Abs(amount);
            health = (health > maxHealth) ? maxHealth : health;
        }

        if (health == 0) {
            KillRatman();
        }

        return health;
    }

    void KillRatman() {
        rat.SetActive(false);
        HatchActivator.EnableHatches();
    }
}
