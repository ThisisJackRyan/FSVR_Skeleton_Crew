using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantSize : MonoBehaviour {

	public float sizeMod = 0.1f;

	void OnPreRender () {
		float size = (Camera.main.transform.position - transform.position).magnitude;
		transform.localScale = new Vector3( size, size, size ) * sizeMod;
	}
}
