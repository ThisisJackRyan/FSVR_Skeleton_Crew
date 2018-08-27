using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MoveChildToPlacement : NetworkBehaviour {

    public Transform toMove;
    public float speed = 1;
    public float thresholdDist = .05f;
    Vector3 newPos;
    // Update is called once per frame
    private void Awake() {
        newPos = toMove.position;
    }

    void Update() {
        if (!isServer) {
            return;
        }

        if (Mathf.Abs(Vector3.Distance(newPos, toMove.position)) > thresholdDist) {
            newPos = Vector3.MoveTowards(toMove.position, transform.position, .05f * speed);
            toMove.position = newPos;
        }
    }
}
