using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class LoadBossScene : NetworkBehaviour {

	AsyncOperation thing;
	bool hasCalled = false;

	public static LoadBossScene instance;

	private void Start() {
		if (!isServer) {
			return;
		}

		if (instance == null) {
			instance = this;
		}
	}

	[Button]
	public void NetworkLoadBossScene() {
		if (!isServer) {
			return;
		}

		if (!hasCalled) {
			//print("started loading scene at " + Time.time);
			NetworkManager.singleton.ServerChangeScene("Boss_Online");
			//RpcLoadBossScene();
			hasCalled = true;
		}
	}

	IEnumerator LoadBossSceneOnline() {
		//print("started at " + Time.time);

		thing = SceneManager.LoadSceneAsync("Boss_Online", LoadSceneMode.Additive);

		while (!thing.isDone) {
			//print(thing.progress);
			yield return new WaitForEndOfFrame();
		}
		//print("done at " + Time.time);
	}

	[ClientRpc]
	private void RpcLoadBossScene() {
		//StartCoroutine(LoadBossSceneOnline());

	}

	[ClientRpc]
	public void RpcFadePlayerCameras() {
		foreach (var v in FindObjectsOfType<FSVRPlayer>()) {
			v.ResetAnims();
			if (v.isLocalPlayer) {
				SteamVR_Fade.Start(Color.black, 2f);
				StartCoroutine(MovePlayerToZero(v.gameObject));
			}
		}

		foreach (var v in FindObjectsOfType<Host>()) {
			StartCoroutine(MovePlayerToZero(v.gameObject));

		}
	}
	IEnumerator MovePlayerToZero(GameObject p) {
		yield return new WaitForSeconds(2f);
		p.transform.position = Vector3.zero;
	}

}
