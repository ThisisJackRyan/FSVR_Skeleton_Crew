using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonBarrel : MonoBehaviour {

    public void TriggerReload() {
        GetComponentInParent<Cannon>().TriggerReload();
    }
}
