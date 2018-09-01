using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MastAimNode : MonoBehaviour {


    public MastInteraction player;
    public GameObject particles;
    MastSwitch mast;
    bool active;
    int index;

    private void OnEnable() {
        mast = GetComponentInParent<MastSwitch>();
        index = transform.GetSiblingIndex();
    }

    private void OnTriggerEnter(Collider other) {
        if (!player || !mast.isServer) {
            return;
        }

        if (other.transform.root == player.transform.root) {
            //adjust mast speed
            mast.AdjustSails(index);
            //GetComponentInParent<MastSwitch>().PlayAim();// play sound
        }
    }
}
