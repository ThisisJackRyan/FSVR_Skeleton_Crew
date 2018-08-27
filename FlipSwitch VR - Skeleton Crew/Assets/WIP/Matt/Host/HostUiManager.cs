using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HostUiManager : MonoBehaviour {

    public Dropdown playerSelectDropdown;
    public Dropdown playerActionDropdown;
    public Button togglePerspectiveButton;
    public Button forceNextPhaseButton;
    public Button performActionButton;
    public Button togglePauseButton;
    public GameObject[] playerTexts;

    private GameObject currentlySelectedPlayer;
    private Host host;
    private bool isHostPerspective = true;

    public void UpdateUI() {
		playerSelectDropdown.options.Clear();
        List<string> playerNames = new List<string>();
        foreach (GameObject player in host.GetPlayerList()) {
            playerNames.Add(player.name);
        }

        playerSelectDropdown.AddOptions(playerNames);
    }

    public void _SelectPlayer(int n) {
        host.SetSelectedPlayer(host.GetPlayerList()[n]);
    }

    public void _TogglePerspective() {
        if (isHostPerspective) {
            togglePerspectiveButton.GetComponentInChildren<Text>().text = "Show Host Perspective";
            host.GetComponent<Camera>().enabled = false;
            for (int i = 0; i < host.GetPlayerList().Count; i++) {
                host.GetPlayerList()[i].GetComponent<FSVRPlayer>().EnableCamera();
                playerTexts[i].SetActive(true);
            }
            // Todo figure out how to look through players eyes without getting null ref on steamvr hmds
        } else {
            togglePerspectiveButton.GetComponentInChildren<Text>().text = "Show Players Perspective";
            host.GetComponent<Camera>().enabled = true;
            for (int i = 0; i < host.GetPlayerList().Count; i++) {
                host.GetPlayerList()[i].GetComponent<FSVRPlayer>().DisableCamera();
                playerTexts[i].SetActive(false);
            }
        }
    }

    public void _TogglePauseGame() {
        togglePauseButton.GetComponentInChildren<Text>().text = Time.timeScale == 0f ? "Pause Game" : "Unpause Game";
        
        host.TogglePause();
    }

    public void _ActionSelection(int n) {
        switch (n) {
            case 0: // Calibrate the player
                performActionButton.GetComponentInChildren<Text>().text = "Perform Calibration";
                break;
            case 1: // Reset the players tags
                performActionButton.GetComponentInChildren<Text>().text = "Perform Tag Reset";
                break;
            case 2: // Player is leaving the game
                performActionButton.GetComponentInChildren<Text>().text = "Perform Leave Game";
                break;
            case 3:
                performActionButton.GetComponentInChildren<Text>().text = "Perform Return";
                break;
        }
    }

    public void _PerformAction() {
        switch (playerActionDropdown.value) {
            case 0: // Calibrate the player
                host.PerformCalibration();
                break;
            case 1: // Reset the players tags
                
                break;
            case 2: // Player is leaving the game
                
                break;
            case 3: // Player is returning to the game
                break;
        }
    }

    public void SetHost(Host g) {
        host = g;
    }
}
