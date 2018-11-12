using Dissonance;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public interface IDissonancePlayer {
    string PlayerId { get; }
    Vector3 Position { get; }
    Quaternion Rotation { get; }
    NetworkPlayerType Type { get; }
}

public class DissonanceCustomPosition : NetworkBehaviour {

    public Transform playerTransformForDissonance;

    private string _playerId;
    
    // This property implements the PlayerId part of the interface
    public string PlayerId { get { return _playerId; } }

    // When the network system starts this behaviour, this method runs
    public override void OnStartAuthority() {
        base.OnStartAuthority();

		// Get the local DissonanceComms object 
		if (FindObjectOfType<DissonanceComms>()) {
			var comms = FindObjectOfType<DissonanceComms>();

			// Call set player name, to sync the name across all peers
			SetPlayerName(FindObjectOfType<DissonanceComms>().LocalPlayerName);

			// Make sure that if the local name is changed, we sync the change across the network
			comms.LocalPlayerNameChanged += SetPlayerName;
		}
    }

    private void SetPlayerName(string playerName) {
        CmdSetPlayerName(playerName);
    }

    // This is a "Command" which means that it is run on *all* peers when invoked.
    // This is what does the actual synchronisation of the name across the network
    [Command]
    private void CmdSetPlayerName(string playerName) {
        _playerId = playerName;
    }

    public Vector3 Position {
        get { return playerTransformForDissonance.position; }
    }

    public Quaternion Rotation {
        get { return playerTransformForDissonance.rotation; }
    }

    public NetworkPlayerType Type {
        get { return isLocalPlayer ? NetworkPlayerType.Local : NetworkPlayerType.Remote; }
    }

    public void OnEnable() {
        
    }
}
