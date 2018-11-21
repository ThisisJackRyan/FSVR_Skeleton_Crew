using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour {

	public static PlayerHud instance;
	//public static PlayerHud Instance {
	//	get {
	//		if (instance == null) {
	//			var g = new GameObject("PlayerHud");
	//			instance = g.AddComponent<PlayerHud>();
	//			return instance;
	//		} else {
	//			return instance;
	//		}
	//	}
	//}

	public Text subtitleText;

	public void UpdateSubtitles(string subtitle) {
		subtitleText.text = subtitle;
	}

	public void ClearSubtitles() {
		subtitleText.text = "";
	}

	private void Start() {
		if (instance == null) {
			instance = this;
		}// else {
		//	Destroy(gameObject);
		//}
	}
}
