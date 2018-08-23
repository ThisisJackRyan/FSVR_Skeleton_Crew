using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MastSwitch : MonoBehaviour {

	public Transform mastUp, mastDown;
	public float swapTime = 3;
	bool raiseMast;

	public GameObject upImage, downImage;
	bool firstLoad = false;
	public UnityEvent firstRunEvent;
	public GameObject pathFollower;
	public AudioClip raise, lower;
	AudioSource source;

	private void Start() {
		mastUp.gameObject.SetActive(true);
		mastDown.gameObject.SetActive(false);
		downImage.SetActive(true);
		upImage.SetActive(false);
		source = GetComponent<AudioSource>();
		//firstRunEvent.AddListener(EnableEnemy);
	}

	private void OnMastChange(bool n) {
		raiseMast = n;
	}

	[Button]
	public void FirstRun() {
		if (FindObjectOfType<Host>().isServer)
			firstRunEvent.Invoke();
	}

	public void SwapMode() {
		Debug.LogWarning("swap called");
		if (raiseMast) {
			mastUp.gameObject.SetActive(true);
			mastDown.gameObject.SetActive(false);
			downImage.SetActive(true);
			upImage.SetActive(false);
			pathFollower.GetComponent<PathFollower>().ChangeSpeed( false );
			source.PlayOneShot(raise);
			raiseMast = !raiseMast;
		} else {
			mastUp.gameObject.SetActive(false);
			mastDown.gameObject.SetActive(true);
			downImage.SetActive(false);
			upImage.SetActive(true);
			pathFollower.GetComponent<PathFollower>().ChangeSpeed( true );
			source.PlayOneShot( lower );
			raiseMast = !raiseMast;
		}

		if (!firstLoad) {
			firstRunEvent.Invoke();
			BehaviorDesigner.Runtime.GlobalVariables.Instance.GetVariable( "EnemiesEnabled" ).SetValue( true );
			firstLoad = true;
		}
	}

	public void EnableEnemy() {
		foreach (Enemy enScript in FindObjectsOfType<Enemy>()) {
			enScript.EnableEnemy();
		}
	}

}