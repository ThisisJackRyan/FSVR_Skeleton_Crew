using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetInitialVelocity : MonoBehaviour {

    public Vector3 force;
    
	// Use this for initialization
	void Start () {
        GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
	}

    private void OnTriggerEnter(Collider other) {
        print("hit barrel");
        if (other.tag == "CannonBarrel") {
            other.GetComponentInParent<Cannon>().ReloadCannon();
            Destroy(gameObject);
        }
    }

 //   // Update is called once per frame
 //   void Update () {
		
	//}
}
