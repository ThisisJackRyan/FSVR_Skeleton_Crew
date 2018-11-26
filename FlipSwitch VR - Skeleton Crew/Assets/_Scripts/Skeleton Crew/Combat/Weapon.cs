using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.Networking;

public class Weapon : NetworkBehaviour {
	public WeaponData data;
	public Transform projectileSpawnPos;
	public Transform fire;
	public CannonInteraction owningPlayerCannonScript;
	public Outline myOutline;
	public bool isBeingHeldByPlayer = false;
	public GameObject playerWhoIsHolding;
	public GameObject playerWhoHolstered = null;

	public Transform reloadParticlePosition;
	public GameObject reloadParticles;

	[SyncVar(hook = "OnAmmoNumChange")] int ammo = -1;
	float lastShottime = 0;
	
	public bool IsFullOnAmmo {
		get {
			//if ammo is the same as data.ammo then return true
			return ( ammo == data.ammo ) ? true : false;
		}
	}

	private void OnAmmoNumChange(int num) {
		ammo = num;
	}

	private void Start() {
		if (fire != null) {
			//print("called");
			fire.gameObject.SetActive(false);
		}

		myOutline = GetComponent<Outline>();
		myOutline.enabled = false;

		if (data.type == WeaponData.WeaponType.Gun) {
			ammo = data.ammo;
		}		
	}

	public void Reload() {
		ammo = data.ammo;
        GetComponent<AudioSource>().PlayOneShot(data.reloadClip);
		GameObject g = Instantiate(reloadParticles, reloadParticlePosition.position, Quaternion.identity);
		NetworkServer.Spawn(g);
    }

    public void SpawnBullet(bool isLeft, ushort hapticSize) {
		if (lastShottime + data.timeBetweenShots > Time.time) {
			return;
		}

        if (ammo-- <= 0) {  //decrements after check
			//print("out of ammo");
			GetComponent<AudioSource>().clip = data.outOfAmmoSound;
			if (playerWhoIsHolding.GetComponentInParent<Player>().isLocalPlayer) {
				Controller.PlayHaptics(isLeft, data.hapticsOutOfAmmo);
			}

		} else {
            if (playerWhoIsHolding.GetComponentInParent<Player>().isLocalPlayer) {
                Controller.PlayHaptics( isLeft, data.hapticsFiring );
			}

			lastShottime = Time.time;

			GetComponent<AudioSource>().clip = data.firesound;

            if (isServer) {
				Vector3 rot = Quaternion.identity.eulerAngles;
				if (data.spread > 0) {
					var variance = Quaternion.AngleAxis(Random.Range(0, 360), rot) * Vector3.up * Random.Range(0, data.spread);
					rot += variance;
				}

                var bullet = Instantiate(data.projectile, projectileSpawnPos.position, Quaternion.Euler(rot));
			    bullet.GetComponent<Rigidbody>().AddForce(projectileSpawnPos.forward * data.power, ForceMode.Impulse);
			    bullet.GetComponent<SCProjectile>().damage = data.damage;
				bullet.GetComponent<SCProjectile>().playerWhoFired = playerWhoIsHolding.transform.root.gameObject;

                //NetworkServer.Spawn(smoke);
                NetworkServer.Spawn(bullet);
            }

			//Instantiate(data.particles, projectileSpawnPos.position, projectileSpawnPos.rotation);

        }

		GetComponent<AudioSource>().Play();
	}

	public void ToggleFire() {
		if (fire.gameObject.activeInHierarchy) {
			fire.gameObject.SetActive(false);
		}
		else {
			fire.gameObject.SetActive(true);
		}

		if (data.firesound) {

			GetComponent<AudioSource>().clip = data.firesound;
		}

		GetComponent<AudioSource>().Play();

	}

	public void TurnOffFire() {
		if (!fire) {
			return;
		}

		if (fire.gameObject.activeInHierarchy) {
			fire.gameObject.SetActive(false);

			if (data.firesound) {
				GetComponent<AudioSource>().clip = data.firesound;
			}

			GetComponent<AudioSource>().Play();
		}

	}

	public void TurnOnFire() {
		if (!fire) {
			return;
		}

		if (!fire.gameObject.activeInHierarchy) {
			fire.gameObject.SetActive(true);

			if (data.firesound) {
				GetComponent<AudioSource>().clip = data.firesound;
			}

			GetComponent<AudioSource>().Play();
		}

	}

	private void OnDrawGizmos() {
		if (projectileSpawnPos) {
			Gizmos.DrawLine(projectileSpawnPos.transform.position,
			projectileSpawnPos.transform.position + projectileSpawnPos.transform.forward * .5f);
		}
	}
}