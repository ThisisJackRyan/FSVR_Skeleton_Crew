using UnityEngine;
using UnityEngine.Networking;

public class Cannon : NetworkBehaviour {

	public float power;
    public GameObject projectile;
    public GameObject smoke;
    public Transform spawnPos;
	public AudioClip fireSound;

	[SyncVar(hook = "OnFiringChange")] private bool isFiring;
	[SyncVar( hook = "OnReload" )] private bool isReloaded;

	void OnReload( bool n ) {
		isReloaded = n;
	}

	void OnFiringChange(bool n) {
        isFiring = n;
    }

    public bool GetIsFiring()
    {
        return isFiring;
    }

	public void CreateCannonBall()
    {
		if (!isReloaded) {
			return;
		}

		isFiring = true;
		isReloaded = false;
        GameObject bullet = Instantiate(projectile, spawnPos.position, Quaternion.identity);
		Instantiate(smoke, spawnPos.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody>().velocity = spawnPos.forward * power;
		Invoke( "ReloadCannon", 3f );
		GetComponent<AudioSource>().clip = fireSound;
		GetComponent<AudioSource>().Play();
	}

	private void ReloadCannon() {
		isFiring = false;
		isReloaded = true;
	}

	private void OnDrawGizmos() {
		if (spawnPos) {
			Gizmos.DrawLine( spawnPos.transform.position, spawnPos.transform.position + spawnPos.transform.forward * 2 );
		}
	}
}

