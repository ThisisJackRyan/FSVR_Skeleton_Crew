using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Cannon : NetworkBehaviour {

    [SyncVar(hook = "OnChargeChange")] float charge = 25;
    public float maxCharge = 75, multiplier = 10;
    public float minCharge = 25;
    public GameObject projectile;
    public GameObject smoke;
    public Transform spawnPos;
    public Slider chargeSlider;
	public AudioClip fireSound;

	[SyncVar(hook = "OnFiringChange")] private bool isFiring;

    void OnFiringChange(bool n) {
        isFiring = n;
    }

    void OnChargeChange(float n)
    {
        charge = n;
    }

    public bool GetIsFiring()
    {
        return isFiring;
    }

    private void Start() {
        chargeSlider.minValue = minCharge;
        chargeSlider.maxValue = maxCharge;
    }

    private void Update() {
        if (charge > minCharge) {
            chargeSlider.transform.parent.gameObject.SetActive(true);
            chargeSlider.value = charge;
        } else {
            chargeSlider.transform.parent.gameObject.SetActive(false);
            chargeSlider.value = minCharge;
        }
    }

    public void SetInitialCharge()
    {
        charge = minCharge;
    }

    public void IncrementCharge()
    {
        charge++;
        if (charge > maxCharge)
        {
            charge = maxCharge;
        }
    }

    public void CreateCannonBall()
    {
		isFiring = true;
        GameObject bullet = Instantiate(projectile, spawnPos.position, Quaternion.identity);
		Instantiate(smoke, spawnPos.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody>().velocity = spawnPos.forward * charge;
		Invoke( "ReloadCannon", 3f );
		GetComponent<AudioSource>().clip = fireSound;
		GetComponent<AudioSource>().Play();
	}

	private void ReloadCannon() {
		isFiring = false;
		charge = minCharge;
	}

	private void OnDrawGizmos() {
		if (spawnPos) {
			Gizmos.DrawLine( spawnPos.transform.position, spawnPos.transform.position + spawnPos.transform.forward * 2 );
		}
	}
}

