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

	public void CreateCannonBall() {
		if ( !isReloaded ) {
			return;
		}

		isFiring = true;
		isReloaded = false;
		GameObject bullet = Instantiate( projectile, spawnPos.position, Quaternion.identity );
		Instantiate( smoke, spawnPos.position, Quaternion.identity );
		bullet.GetComponent<Rigidbody>().velocity = spawnPos.forward * power;
		Invoke( "ReloadCannon", 3f );
		GetComponent<AudioSource>().clip = fireSound;
		GetComponent<AudioSource>().Play();
	}

	private void ReloadCannon() {
		isFiring = false;
		isReloaded = true;
	}

	public int indexOfFirstGrabbed = -1; //only being set on local player
	int angleIncrement = 5;


	public void RotateBarrel( int indexOfNode ) {
		if ( !isServer )
			return;
		//aiming is weird, -5 is the lowest, -45 is the highest. take in as positive and convert min and max to negative for best results
		if ( indexOfFirstGrabbed >= 0 ) {
			int raiseSign = ( indexOfNode > indexOfFirstGrabbed ) ? -1 : 1; //if index is greater (closer to back of cannon) then you are raising the cannon

			float barrelRotation = cannonBarrel.localEulerAngles.x;
			float targetAngle = ( barrelRotation + ( raiseSign * angleIncrement ) + 360 ) % 360;
			//print( "current index " + indexOfFirstGrabbed + " index that called " + indexOfNode + " " + barrelRotation + " plus " + ( raiseSign * angleIncrement ) + " becomes target of " + targetAngle );

			//if (targetAngel <= maxAngle && targetAngel >= minAngle) {
			//perform rotation
			print( targetAngle + " is within range, rotate barrel" );
			//targetAngle += 360;

			if ( targetAngle >= minAngle && targetAngle <= maxAngle ) {
				cannonBarrel.localEulerAngles = new Vector3( targetAngle, 0, 0 );
				//print( "AFTER " + barrelRotation + " is old,  " + cannonBarrel.localRotation + " is new, target was " + targetAngle );

				indexOfFirstGrabbed = indexOfNode;
			}

			//} else {
			//	print( targetAngel + " is not within range, do not rotate barrel" );
			//}

			//RpcRotateBarrel( cannonBarrel.localRotation );
		}
	}

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