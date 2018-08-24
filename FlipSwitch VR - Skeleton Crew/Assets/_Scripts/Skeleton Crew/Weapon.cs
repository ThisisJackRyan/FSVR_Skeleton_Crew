using UnityEngine;
using UnityEngine.Networking;

public class Weapon : NetworkBehaviour {
	public WeaponData data;
	public Transform projectileSpawnPos;
	public Transform fire;
	public CannonInteraction owningPlayerCannonScript;
	public Outline myOutline;
	public bool isBeingHeldByPlayer = false;
	public GameObject playerWhoHolstered = null;
	[SyncVar(hook = "OnAmmoNumChange")] int ammo = -1;

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
	}

	public void SpawnBullet() {
		//needs ammo check, do we want ammo based on weapon or player? prolly weapon
		if (ammo-- <= 0) {  //decrements after check
			GetComponent<AudioSource>().clip = data.outOfAmmoSound;
		} else {
			var bullet = Instantiate(data.projectile, projectileSpawnPos.position, Quaternion.identity);
			bullet.GetComponent<Rigidbody>().AddForce(projectileSpawnPos.forward * data.power, ForceMode.Impulse);
			bullet.GetComponent<SCProjectile>().damage = data.damage;
			Instantiate(data.particles, projectileSpawnPos.position, Quaternion.Euler(transform.forward));

			GetComponent<AudioSource>().clip = data.firesound;
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

	}

	public void TurnOffFire() {
		if (!fire) {
			return;
		}

		if (fire.gameObject.activeInHierarchy) {
			fire.gameObject.SetActive(false);
		}
	}

	private void OnDrawGizmos() {
		if (projectileSpawnPos) {
			Gizmos.DrawLine(projectileSpawnPos.transform.position,
				projectileSpawnPos.transform.position + projectileSpawnPos.transform.forward * .5f);
		}
	}
}