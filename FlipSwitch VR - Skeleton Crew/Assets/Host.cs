using System.Collections;
using System.Collections.Generic;
using RootMotion.Demos;
using UnityEngine;
using UnityEngine.Networking;

public class Host : NetworkBehaviour {

    public GameObject tagResetterPrefab;

    private List<GameObject> players;
    private HostUiManager scriptHostUi;

    private GameObject selectedPlayer;

    #region Getters & Setters
    public void SetSelectedPlayer(GameObject p)
    {
        selectedPlayer = p;
    }

    public List<GameObject> GetPlayerList()
    {
        return players;
    }
    #endregion

    #region Initialization
    void Start()
    {
        if (!isLocalPlayer)
        {
            return;
        }

		GetComponent<Camera>().enabled = true;

		//GameObject.Find("Canvas").SetActive(true);
		//GameObject uiManager = GameObject.Find("HostUIManager");
		//uiManager.SetActive(true);

		//scriptHostUi = uiManager.GetComponent<HostUiManager>();
		//scriptHostUi.SetHost(this);
	}

	public void AddPlayerToHostList(GameObject playerToAdd) {
        RpcAddPlayerToHost(playerToAdd);
    }

    [ClientRpc]
    private void RpcAddPlayerToHost(GameObject playerToAdd) {
        if (!isLocalPlayer) {
            return;
        }

        players.Add(playerToAdd);
        playerToAdd.GetComponent<PlayerSetup>().SetCameraSettings(players.Count);
        playerToAdd.name = "Player " + players.Count;
        scriptHostUi.UpdateUI();
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
}
