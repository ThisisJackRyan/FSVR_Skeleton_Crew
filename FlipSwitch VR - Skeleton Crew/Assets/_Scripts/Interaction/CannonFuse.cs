using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonFuse : MonoBehaviour {

    public Cannon cannonScript;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Fire" && !cannonScript.GetIsFiring() && cannonScript.isServer)
        {
            cannonScript.SetInitialCharge();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Fire" && !cannonScript.GetIsFiring())
        {
            other.GetComponentInParent<Weapon>().owningPlayerCannonScript.Fire(cannonScript.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Fire" && !cannonScript.GetIsFiring() && cannonScript.isServer)
        {
            cannonScript.IncrementCharge();

        }
    }

}
