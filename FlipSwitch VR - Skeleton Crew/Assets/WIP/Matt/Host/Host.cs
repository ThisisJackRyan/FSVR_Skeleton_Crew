﻿using System.Collections;
using System.Collections.Generic;
using Dissonance;
using RootMotion.Demos;
using UnityEngine;
using UnityEngine.Networking;

public class Host : NetworkBehaviour {

	public GameObject tagResetterPrefab;
	List<GameObject> players;
	private HostUiManager hostUI;

	private GameObject selectedPlayer;

	#region Getters & Setters
	public void SetSelectedPlayer(GameObject p) {
		selectedPlayer = p;
	}

	public List<GameObject> GetPlayerList() {
		return players;
	}
	#endregion

	#region Initialization
	void Start() {

		if (!isLocalPlayer && !isServer) {
			GetComponent<AudioListener>().enabled = false;
			return;
		}

		//var comms = FindObjectOfType<VoiceBroadcastTrigger>();
		//comms.BroadcastPosition = false;
		GetComponent<Camera>().enabled = true;
		GetComponent<AudioListener>().enabled = true;
		hostUI = GetComponent<HostUiManager>();
		//Resources.FindObjectsOfTypeAll<HostCanvas>()[0].gameObject.SetActive(true);
		//GameObject uiManager = GameObject.Find( "HostUIManager" );
		//uiManager.SetActive( true );
		//scriptHostUi = uiManager.GetComponent<HostUiManager>();
		//scriptHostUi.SetHost( this );

		if (isServer) {
			InitSecondaryDisplays();
		}

	}

	public void AddPlayerToHostList(GameObject playerToAdd) {
		//print("pre rpc " + playerToAdd.name);
		RpcAddPlayerToHost(playerToAdd);
	}

	[ClientRpc]
	private void RpcAddPlayerToHost(GameObject playerToAdd) {
		if (!isLocalPlayer) {
			return;
		}

		//print("should be adding " + playerToAdd.name + " to host list on host client");

		if (players == null) {
			players = new List<GameObject>();
		}

		players.Add(playerToAdd);
		playerToAdd.GetComponent<FSVRPlayer>().SetCameraSettings(players.Count, hostUI.mirrorViews[players.Count - 1]);
		playerToAdd.name = "Player " + players.Count;
		//  scriptHostUi.UpdateUI();
	}

	#endregion

	#region Handle Pausing
	public void TogglePause() {
		if (isLocalPlayer) {
			CmdTogglePause();
		}
	}

	[Command]
	private void CmdTogglePause() {
		RpcTogglePause();
	}

	[ClientRpc]
	private void RpcTogglePause() {
		if (Time.timeScale == 0f) {
			Time.timeScale = 1f;
		} else {
			Time.timeScale = 0f;
		}
	}
	#endregion

	#region Handle Calibrations

	public void PerformCalibration() {
		if (!selectedPlayer) {
			return;
		}

		CmdCalibratePlayer(selectedPlayer);
	}

	[Command]
	private void CmdCalibratePlayer(GameObject g) {
		RpcCalibratePlayer(g);
	}

	[ClientRpc]
	private void RpcCalibratePlayer(GameObject g) {
		if (isServer) {
			return;
		}

		g.GetComponentInChildren<VRIKCalibrationController>().Calibrate();
	}

	#endregion

	#region Handle Tag Resetting

	#endregion

	#region Handle Player Leaving

	#endregion

	#region Handle Player Returning

	#endregion

	#region Mirror Views

	public void ShowView(int player) {
		//turn on players camera -- should be rendering to mirrored display
		if (!isServer) {
			return;
		}

		foreach (var g in players) {
			if (g.name == "Player " + player) {

			}
		}
	}

	void InitSecondaryDisplays() {
		//get player cameras, set their display to 2
		//activate display 2
		//turn them all back off

		for (int i = 0; i < Display.displays.Length; i++) {
			if (i == 0) {
				continue;
			}

			Debug.Log("Activating display " + i);
			Display.displays[i].Activate();
		}
	}

	#endregion
}
