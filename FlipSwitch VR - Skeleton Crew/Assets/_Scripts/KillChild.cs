using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class KillChild : NetworkBehaviour {

    public GameObject child;

    [Command]
	public void CmdKillChild()
    {
        RpcKillChild();
    }

    [ClientRpc]
    public void RpcKillChild()
    {
        Destroy(child);
    }
}
