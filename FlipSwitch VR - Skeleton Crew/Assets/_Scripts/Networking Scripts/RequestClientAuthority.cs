using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RequestClientAuthority : NetworkBehaviour {
	[Command]
	public void CmdRequestAuthority(GameObject obj) {
		if(obj.GetComponent<NetworkIdentity>().clientAuthorityOwner == null ) { // Make sure object doesn't already have a client owner
			obj.GetComponent<NetworkIdentity>().AssignClientAuthority( GetComponent<NetworkIdentity>().connectionToClient ); // Set the client owner to this client 
		} else {
			// Object already has an owner, can't take control of it.
		}
	}

	[Command]
	public void CmdRequestRemoveAuthority(GameObject obj) {
		if(obj.GetComponent<NetworkIdentity>().clientAuthorityOwner == GetComponent<NetworkIdentity>().connectionToClient ) { // Make sure that the object is owner by this client
			obj.GetComponent<NetworkIdentity>().RemoveClientAuthority( GetComponent<NetworkIdentity>().connectionToClient ); // Remove this client as the objects owner
		} else {
			// Either the owner of the object is null, or another client has authority.
		}
	}
}
