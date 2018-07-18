using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class ScriptSyncPlayer : NetworkBehaviour {

    [SyncVar(hook = "OnHealthChange" )] public int health = 100;
    int maxHealth = 100;

    [Tooltip("The particles within the bones (flames)")] public GameObject[] internalParticles;
    [Tooltip("The particles coming out of the bones (dust)")] public GameObject[] externalParticles; // external is dust
    [Tooltip("The death particles (old player particles)")] public GameObject[] deathParticles;



    public GameObject[] playerBody;
    public GameObject[] deathExplosion;
    public Transform explosionPosition;
    bool isDead;

    void OnHealthChange(int n) {
        health = n;

        if (health <= 0) { // enabled: death ; disabled: internal & external
            DisableBody();           
            UpdateParticles(false, false, true);
        } else if(health <= 25) { // enabled: internal ; disabled: external & death
            UpdateParticles(true, false, false);
        } else if(health <= maxHealth) { // enabled: internal & external ; disabled: death
            UpdateParticles(true, true, false);
        }
    }

    private void UpdateParticles(bool internalActive, bool externalActive, bool deathActive) {
        for(int i=0; i<internalParticles.Length; i++) {
            internalParticles[i].SetActive(internalActive);
            externalParticles[i].SetActive(externalActive);
            deathParticles[i].SetActive(deathActive);
        }
    }

    [Button]
    public void KillMe() {
        if (isServer)
            ChangeHealth(maxHealth);
    }

    private void Start() {
         OnHealthChange(health);
    }


    bool hasStartedTutorial = false;

    public void RevivePlayer() {
        ChangeHealth(maxHealth, false);
        EnableBody();
        RpcEnableBody();
    }

    void DisableBody() {
        foreach (GameObject g in playerBody) {
            g.SetActive(false);
        }
        GetComponent<ChangeAvatar>().DisableArmor();
        if (!isServer) 
            Instantiate(deathExplosion[GetComponent<ChangeAvatar>().GetColor()], explosionPosition.position, Quaternion.identity);
    }

    [ClientRpc]
    void RpcEnableBody() {
        if (isServer)
            return;

        foreach (GameObject g in playerBody) {
            g.SetActive(true);
        }

        GetComponent<ChangeAvatar>().EnableArmor();
    }

    void EnableBody() {
        foreach(GameObject g in playerBody) {
            g.SetActive(true);
        }

        GetComponent<ChangeAvatar>().EnableArmor();
    }

    public void TellCaptainToStartTutorial() {
        if (isServer && !hasStartedTutorial) {
            hasStartedTutorial = true;
            StartCoroutine("WaitToTellCaptain");
        }
    }

    IEnumerator WaitToTellCaptain() {
        yield return new WaitForSecondsRealtime(3);
        FindObjectOfType<Captain>().StartTutorial();
    }

    public int ChangeHealth(int amount, bool damage = true) {
        if (!isServer)
            return health;

        if (damage) {
            health -= Mathf.Abs(amount);
            health = (health < 0) ? 0 : health;
        } else {
            health += Mathf.Abs(amount);
            health = (health > maxHealth) ? maxHealth : health;
        }


        return health;
    }

    public int GetHealth()
    {
        return health;
    }
}
