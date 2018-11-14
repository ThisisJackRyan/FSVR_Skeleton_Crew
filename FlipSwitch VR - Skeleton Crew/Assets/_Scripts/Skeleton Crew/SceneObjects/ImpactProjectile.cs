using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ImpactProjectile : NetworkBehaviour {

    public int damage;
    public int health = 1;

    public bool isDestructible = false;
    public GameObject deathParticles;
    public GameObject particles;
    public float particleKillTimer = 2f;
    [HideInInspector]
    public GameObject reticle;

    void Awake() {
        Invoke("KillProjectile", 10f);
    }

    public void SetReticle(GameObject ret) {
        reticle = ret;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "PlayerCollider") {
            other.GetComponentInParent<Player>().ChangeHealth(150);
        } else if (other.tag == "Ratman") {
            other.GetComponentInParent<Ratman>().ChangeHealth(150);
        } else if (other.tag == "BulletPlayer") {
            //destroy gameobject
            //print("in the bullet player if");
            HitByMusket(other.gameObject);
        }
    }

    public void KillProjectile() {
        //if ( !isServer ) {
        //	return;
        //}

        //RpcKillProjectile();

        //todo needs fixed
        if (particles) {

            particles.transform.parent = null;
            Destroy(particles, particleKillTimer);
        }

        //print("Should be destroying the bullet");
        NetworkServer.Destroy(gameObject);
    }

    void HitByMusket(GameObject bullet) {
        Destroy(bullet);

        if (!isServer) {
            return;
        }

        if (GetComponent<Rigidbody>().isKinematic) {

            health--;
            if (health <= 0) {
                //print("called rpc bullet");
                NetworkServer.Destroy(gameObject);
                NetworkServer.Destroy(reticle);
                if (deathParticles) {
                    var dp = Instantiate(deathParticles, transform.position, Quaternion.identity);
                    NetworkServer.Spawn(dp);
					VariableHolder.instance.IncreasePlayerScore(bullet.GetComponent<SCProjectile>().playerWhoFired, VariableHolder.PlayerScore.ScoreType.CrystalsDetroyed, transform.position);
                }

            }
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (!isServer) {
            return;
        }

        OnTriggerEnter(collision.collider);

        //print("collision enter on " + name);
        KillProjectile();
    }
}
