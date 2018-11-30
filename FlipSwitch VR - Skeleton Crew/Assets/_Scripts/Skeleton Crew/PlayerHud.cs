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

	public void UpdateSubtitles(string subtitle, bool selfClear = false) {
		CancelInvoke("ClearSubtitles");
		print( "should be updating subtitles to " + subtitle );
		subtitleText.text = subtitle;
		if ( selfClear ) {
			Invoke( "ClearSubtitles", 10f );
		}
	}

	public void ClearSubtitles() {
		subtitleText.text = "";
	}

	private void Start() {
		if (instance == null) {
			instance = this;
		}
		// else {
		//	Destroy(gameObject);
		//}
	}
}
