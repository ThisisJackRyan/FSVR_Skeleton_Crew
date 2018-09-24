using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


public class ScriptNetworkManager : NetworkManager {

	public GameObject hostPrefab;
	bool firstSpawn = true;
	public bool testing = true;

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
		//if (testing)
		//{
		//    GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
		//    NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
		//} else
		//{
		if ( firstSpawn ) {
			GameObject host = Instantiate( hostPrefab, Vector3.zero, Quaternion.identity );
			NetworkServer.AddPlayerForConnection( conn, host, playerControllerId );
			firstSpawn = false;
		} else {
			GameObject player = Instantiate( playerPrefab, Vector3.zero, Quaternion.identity );
			NetworkServer.AddPlayerForConnection( conn, player, playerControllerId );
		}
		//}



		//if ( NetworkHelper.hostIpAddress.Equals( conn.address ) ) {
		//	GameObject host = Instantiate( hostPrefab, Vector3.zero, Quaternion.identity );
		//	NetworkServer.AddPlayerForConnection( conn, host, playerControllerId );
		//} else {
		//	GameObject player = Instantiate( playerPrefab, Vector3.zero, Quaternion.identity );
		//	NetworkServer.AddPlayerForConnection( conn, player, playerControllerId );
		//}
	}
}
