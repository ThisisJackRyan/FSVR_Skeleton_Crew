using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

public class Cannon : NetworkBehaviour {

	public float power;
	public GameObject projectile;
	public GameObject smoke;
	public Transform spawnPos;
	public AudioClip fireSound, fuseClip;

	public Transform cannonBarrel;
	public float minAngle, maxAngle;
	public Transform[] aimingNodes;
	public AudioClip aimClip;
    public Animator cannonBarrelAnimator;


    [SyncVar( hook = "OnFiringChange" )]
	private bool isFiring;
	[SyncVar( hook = "OnReload" )]
	private bool isReloaded = true;

	private void Start() {
		if ( minAngle > maxAngle ) {
			float temp = maxAngle;
			maxAngle = minAngle;
			minAngle = temp;
		}

		cannonBarrel.localEulerAngles = new Vector3( maxAngle, 0, 0 );
	}

	[ClientRpc]
	public void RpcPlayFuse() {
		if (isServer) {
			return;
		}
		GetComponent<AudioSource>().PlayOneShot(fuseClip);
	}

	public void PlayFuse() {
		if (!isServer) {
			return;
		}
		GetComponent<AudioSource>().PlayOneShot( fuseClip );
		RpcPlayFuse();

	}

	[ClientRpc]
	public void RpcPlayAim() {
		if ( isServer ) {
			return;
		}
		GetComponent<AudioSource>().PlayOneShot( aimClip );
	}

	public void PlayAim() {
		if ( !isServer ) {
			return;
		}
		GetComponent<AudioSource>().PlayOneShot( aimClip );
		RpcPlayAim();
	}

	void OnReload( bool n ) {
		isReloaded = n;
	}

	void OnFiringChange( bool n ) {
		isFiring = n;
	}

	public bool GetIsFiring() {
		return isFiring;
	}

    [Button]
	public void CreateCannonBall(GameObject shooter) {
		if ( !isReloaded ) {
			return;
		}

		isFiring = true;
		isReloaded = false;
		GameObject bullet = Instantiate( projectile, spawnPos.position, Quaternion.identity );
		Instantiate( smoke, spawnPos.position, spawnPos.rotation );
		bullet.GetComponent<Rigidbody>().velocity = spawnPos.forward * power;
		bullet.GetComponent<SCProjectile>().playerWhoFired = shooter;

        if (isMagicCannon) {
            Invoke("ReloadCannon", 3f);
        }

        GetComponent<AudioSource>().clip = fireSound;
		GetComponent<AudioSource>().Play();

        //cannonBarrelAnimator.SetTrigger("Fire");

        GetComponent<NetworkAnimator>().SetTrigger("Fire");

		if (isServer) {
			StartCoroutine("FireProp");
		}

	}

	public float cannonPropWait = 0.5f;

	IEnumerator FireProp() {
#if PROP_ENABLED
		yield return new WaitForSeconds(cannonPropWait);
		PropController.Instance.ActivateProp(cannonProp);
#endif
	}

	public bool isMagicCannon = false;

	public Prop cannonProp;

    public void TriggerReload() {
        if (!isServer || isMagicCannon) {
            return;
        }

        //print("received anim event to reload on server");
        if (assignedSlave.GetHealth() > 0) {
            assignedSlave.PlayReload();
        } else {
            //print("reload called but rat is dead");
            //NeedsReloaded = true;
        }
    }

    //[SyncVar]
    public bool NeedsReloaded {
        get {
            return !isReloaded;
        }
    }

    public Ratman assignedSlave;

    public GameObject reloadEffect;
    public Transform reloadEffectPlacement;
    public void ReloadCannon() {
        ////print("Reloading");
        //spawn effect
        if (isServer) {
            var g = Instantiate(reloadEffect, reloadEffectPlacement.position, Quaternion.identity);
            NetworkServer.Spawn(g);            
        }

		isFiring = false;
		isReloaded = true;
        //NeedsReloaded = false;
	}

	public int indexOfFirstGrabbed = -1; //only being set on local player
	int angleIncrement = 5;

	public void RotateBarrel( int indexOfNode ) {
        if (!isServer) {
            return;
        }

        //aiming is weird, -5 is the lowest, -45 is the highest. take in as positive and convert min and max to negative for best results
        if ( indexOfFirstGrabbed >= 0 ) {
			int raiseSign = ( indexOfNode > indexOfFirstGrabbed ) ? -1 : 1; //if index is greater (closer to back of cannon) then you are raising the cannon

			float barrelRotation = cannonBarrel.localEulerAngles.x;
			float targetAngle = ( barrelRotation + ( raiseSign * angleIncrement ) + 360 ) % 360;
			////print( "current index " + indexOfFirstGrabbed + " index that called " + indexOfNode + " " + barrelRotation + " plus " + ( raiseSign * angleIncrement ) + " becomes target of " + targetAngle );

			//if (targetAngel <= maxAngle && targetAngel >= minAngle) {
			//perform rotation
			////print( targetAngle + " is within range, rotate barrel" );
			//targetAngle += 360;

			if ( targetAngle >= minAngle && targetAngle <= maxAngle ) {
				cannonBarrel.localEulerAngles = new Vector3( targetAngle, 0, 0 );
				////print( "AFTER " + barrelRotation + " is old,  " + cannonBarrel.localRotation + " is new, target was " + targetAngle );

				indexOfFirstGrabbed = indexOfNode;
			}

			//} else {
			//	//print( targetAngel + " is not within range, do not rotate barrel" );
			//}

			//RpcRotateBarrel( cannonBarrel.localRotation );
		}
	}

    [ClientRpc]
	public void RpcRotateBarrel( Quaternion newRot ) {
		if ( isServer ) {
			return;
		}
		cannonBarrel.localRotation = newRot;
	}

	private void OnDrawGizmos() {
		if ( spawnPos ) {
			Gizmos.DrawLine( spawnPos.transform.position, spawnPos.transform.position + spawnPos.transform.forward * 2 );
		}
	}
}