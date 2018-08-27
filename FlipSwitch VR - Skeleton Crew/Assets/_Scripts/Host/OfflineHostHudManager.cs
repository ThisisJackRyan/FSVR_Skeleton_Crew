using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class OfflineHostHudManager : MonoBehaviour {

    [Tooltip("Objects to turn on for host")] public GameObject[] thingsToTurnOn;
    [Tooltip("Objects to turn off for host")] public GameObject[] thingsToTurnOff;



    private void OnEnable()
    {
        if (NetworkHelper.hostIpAddress.Equals(NetworkHelper.GetLocalIPAddress()))
        {
            foreach(GameObject obj in thingsToTurnOn)
            {
                obj.SetActive(true);
            }
            foreach(GameObject obj in thingsToTurnOff)
            {
                obj.SetActive(false);
            }
        }
    }

    public void _StartGame()
    {
        NetworkManager.singleton.StartHost();
    }

}
