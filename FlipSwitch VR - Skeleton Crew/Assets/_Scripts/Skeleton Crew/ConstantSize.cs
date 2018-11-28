using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantSize : MonoBehaviour {

	public float sizeMod = 0.1f;

	private void Start() {
		Camera.onPreRender += this.MyPreRender;
	}

	void MyPreRender(Camera cam) {
		print("test");
		float size = (Camera.main.transform.position - transform.position).magnitude;
		transform.localScale = new Vector3( size, size, size ) * sizeMod;
	}

	public void OnEnable() {
		// register the callback when enabling object
		Camera.onPreRender += MyPreRender;
	}

	public void OnDisable() {
		// remove the callback when disabling object
		Camera.onPreRender -= MyPreRender;
	}

	private void OnDestroy() {
		Camera.onPreRender -= MyPreRender;
	}
}
