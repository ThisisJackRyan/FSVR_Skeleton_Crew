using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ScriptSyncObject : NetworkBehaviour {

    private Transform myTrans;
    [SerializeField] float lerpRate = 5;
    [SyncVar] private Vector3 syncPos;
   // private NetworkIdentity netId;

    private Vector3 lastPos;
    private float threshold = 0.5f;

	// Use this for initialization
	void Start ()
    {
        myTrans = GetComponent<Transform>();
        syncPos = myTrans.position;	
	}
	
	void FixedUpdate ()
    {
        TransmitPos();
        LerpPosition();
	}

    private void LerpPosition()
    {
        if (!hasAuthority)
            myTrans.position = Vector3.Lerp(myTrans.position, syncPos, Time.deltaTime * lerpRate);
    }

    [Command]
    private void Cmd_ProvidePositionToServer(Vector3 pos)
    {
        syncPos = pos;
    }

    [ClientCallback]
    private void TransmitPos()
    {
        if(hasAuthority && Vector3.Distance(myTrans.position, lastPos) > threshold)
        {
            Cmd_ProvidePositionToServer(myTrans.position);
            lastPos = myTrans.position;
        }
    }
}
