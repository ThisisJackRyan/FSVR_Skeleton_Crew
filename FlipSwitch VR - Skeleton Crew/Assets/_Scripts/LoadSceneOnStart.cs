using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ConnectWithPress))]
public class LoadSceneOnStart : MonoBehaviour {

	public string offlineScene;
	private string onlineScene;
	bool loaded = false;


	public static LoadSceneOnStart s;

	// Use this for initialization
	void Awake() {
		if (s == null) {
			s = this;

			DontDestroyOnLoad(gameObject);
			if (!loaded) {
				onlineScene = SceneManager.GetActiveScene().name;
				SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
				SceneManager.LoadScene(offlineScene);
				loaded = true;
			}
		} else {
			Destroy(gameObject);
		}
	}

	private void SceneManagerOnSceneLoaded(Scene arg0, LoadSceneMode loadSceneMode) {
		if ( arg0.name == offlineScene ) {
			NetworkManager.singleton.onlineScene = onlineScene;
			NetworkManager.singleton.offlineScene = offlineScene;
			SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
		}
	}
	
}