using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HostUiManager : NetworkBehaviour {

    public Dropdown playerSelectDropdown;
    public Dropdown forceEventDropdown;
    public Button forceEventButton;
    public Button mirrorViewButton;
    public Button performCalibrateButton;
    public Button togglePauseButton;
	public Text togglePauseText;
	public Button speedUp, speedDown;

	public Text headerText;

	public GameObject canvas;
	public GameObject[] playerViewPanel;
    public GameObject[] playerTexts;
	public RenderTexture[] mirrorViews;

    private GameObject currentlySelectedPlayer;
    private Host host;
    private bool isHostPerspective = true;

	private void Start() {
		if (!isServer) {
			canvas.SetActive(false);
		}

		host = GetComponent<Host>();
	}

	public void UpdateUI() {
		playerSelectDropdown.options.Clear();
        List<string> playerNames = new List<string>();
        foreach (GameObject player in host.GetPlayerList()) {
            playerNames.Add(player.name);
        }

        playerSelectDropdown.AddOptions(playerNames);
    }

    public void _SelectPlayer(int n) {
        host.SetSelectedPlayer(host.GetPlayerList()[n-1]);
		headerText.text = "Player " + n;
		//update buttons here
		mirrorViewButton.onClick.RemoveAllListeners();
		mirrorViewButton.onClick.AddListener(()=> host.ShowView(n));
    }

    public void _TogglePerspective(int i) {
		//if (isHostPerspective) {
		//    togglePerspectiveButton.GetComponentInChildren<Text>().text = "Show Host Perspective";
		//    host.GetComponent<Camera>().enabled = false;
		//    for (int i = 0; i < host.GetPlayerList().Count; i++) {
		//        host.GetPlayerList()[i].GetComponent<FSVRPlayer>().EnableCamera();
		//        playerTexts[i].SetActive(true);
		//    }
		//    // Todo figure out how to look through players eyes without getting null ref on steamvr hmds
		//} else {
		//    togglePerspectiveButton.GetComponentInChildren<Text>().text = "Show Players Perspective";
		//    host.GetComponent<Camera>().enabled = true;
		//    for (int i = 0; i < host.GetPlayerList().Count; i++) {
		//        host.GetPlayerList()[i].GetComponent<FSVRPlayer>().DisableCamera();
		//        playerTexts[i].SetActive(false);
		//    }
		//}

		host.ShowView(i);
    }

	internal void EnablePlayerView(int i) {
		playerViewPanel[i].SetActive(true);
	}

	public void _TogglePauseGame() {
		togglePauseText.text = Time.timeScale == 0f ? "Pause Game" : "Unpause Game";
        
        host.TogglePause();
    }

	public void _ForceEvent() {
		switch (forceEventDropdown.value) {
			case 0: // 
				break;
			case 1: // to deck
				if (FindObjectOfType<ExitLobbySwitch>()) {
					FindObjectOfType<ExitLobbySwitch>().TeleportWorkAround();
				} else {
					Debug.LogWarning("couldnt find exit lobby switch");
				}
				break;
			case 2: // first phase
				FindObjectOfType<PathFollower>().StartMoving();
				break;
			case 3: //first break
				FindObjectOfType<PathFollower>().StartFirstBreak();

				break;
			case 4: //second phase
				FindObjectOfType<PathFollower>().StartSecondPhase();

				break;
			case 5: //third phase
				FindObjectOfType<PathFollower>().StartThirdPhase();

				break;
			case 6: //spawn boss cave
				FindObjectOfType<PathFollower>().SpawnBossCave();

				break;
		}
	}

	public void CalibratePlayer() {
		if (!isServer) {
			return;
		}
		//find client to calibrate on
		//playerSelectDropdown.value is the player needing calibration

		//not the correct player, this is the local player - not the player selected.
		currentlySelectedPlayer.GetComponent<FSVRPlayer>().RpcReCalibrate();

	}


	public void SetHost(Host g) {
        host = g;
    }
}
