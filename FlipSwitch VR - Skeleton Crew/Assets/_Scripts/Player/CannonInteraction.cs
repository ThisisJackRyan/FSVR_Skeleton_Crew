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
        Captain.instance.playersFiredCannons[this] = true;
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

    private void OnEnable() {
        Captain.instance.playersFiredCannons.Add(this, false);
    }

    private void OnDisable() {
        Captain.instance.playersFiredCannons.Remove(this);
    }
}
