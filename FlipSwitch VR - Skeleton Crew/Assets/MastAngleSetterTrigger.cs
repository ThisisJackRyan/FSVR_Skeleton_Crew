﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonAngleSetterTrigger : MonoBehaviour {

    MastSwitch mast;
    public GameObject[] nodes;
    public MastAimNode[] aimNodes;
    GameObject activator;

    private void OnEnable()
    {
        mast = GetComponentInParent<MastSwitch>();
    }

    private void OnTriggerEnter(Collider other){
        if (!mast.isServer) {
            return;
        }
        //print("enter");
        if (other.GetComponentInParent<CannonInteraction>()) {
            activator = other.transform.root.gameObject;
            //print("player");

            //is player, show orbs
            foreach (var item in aimNodes) {
                if (item.particles.activeInHierarchy) {
                    return;
                }
            }

            //print("no aim nodes");

            //no aim node particle is active, turn these on
            TurnONNodes();
            activator.GetComponent<MastInteraction>().RpcTurnONHintNodes(transform.root.gameObject);

        }
    }

    private void OnTriggerExit(Collider other){
        if (!mast.isServer) {
            return;
        }
        //print("exit");

        if (other.transform.root.gameObject == activator) {
            //print("is activator");


            //is player, show orbs
            TurnOffNodes();

            activator.GetComponent<CannonInteraction>().RpcTurnOffHintNodes(transform.root.gameObject);
        }
    }

    public void TurnOffNodes() {
        //print("turn off");

        foreach (var item in nodes) {
            item.SetActive(false);
        }
    }

    public void TurnONNodes() {
        //print("turn on");

        foreach (var item in nodes) {
            item.SetActive(true);
        }
    }
}
