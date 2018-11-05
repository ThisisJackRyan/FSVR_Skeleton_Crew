using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class LoadBossScene : NetworkBehaviour {

	AsyncOperation thing;
	bool hasCalled = false;

	[Button]
	public void NetworkLoadBossScene() {
		if (!isServer) {
			return;
		}


		if (!hasCalled) {

		print("started loading scene at " + Time.time);
		RpcLoadBossScene();

			hasCalled = true;
		}
	}

	IEnumerator LoadBossSceneOnline() {
		thing = SceneManager.LoadSceneAsync("Boss_Online", LoadSceneMode.Additive);
		//RpcLoadBossScene();
		while (!thing.isDone) {
			print(thing.progress);
			yield return null;
		}
	}

	IEnumerator PrintLoadProgress() {
		while(thing.progress < 1f) {
			yield return null;
		}

		print("Scene ended loading at " + Time.time);
	}

	[ClientRpc]
	private void RpcLoadBossScene() {
		StartCoroutine(LoadBossSceneOnline());

	}

	[ClientRpc]
	private void RpcFadePlayerCameras() {
		foreach(var v in FindObjectsOfType<FSVRPlayer>()) {
			if (v.isLocalPlayer) {
				SteamVR_Fade.Start(Color.black, 0);
			}
		}
	}


}
