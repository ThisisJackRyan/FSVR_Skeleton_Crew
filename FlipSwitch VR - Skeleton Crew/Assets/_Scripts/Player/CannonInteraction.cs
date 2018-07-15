using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CannonInteraction : NetworkBehaviour {

	public void Fire(GameObject cannon)
    {
        if (!isLocalPlayer)
            return;

        CmdFireCannon(cannon);
    }

    [Command]
    private void CmdFireCannon(GameObject cannon)
    {
        cannon.GetComponent<Cannon>().CreateCannonBall();
        Captain.playersFiredCannons[this] = true;
        Captain.instance.CheckPlayersCannonFiring();

        RpcFireCannon(cannon);
    }

    [ClientRpc]
    private void RpcFireCannon(GameObject cannon)
    {
        if (isServer)
            return;
        cannon.GetComponent<Cannon>().CreateCannonBall();
    }

    private void Start() {
        if (isServer) {
            print(name + " enabled server check");
            Captain.playersFiredCannons.Add(this, false);
        }
    }
}
