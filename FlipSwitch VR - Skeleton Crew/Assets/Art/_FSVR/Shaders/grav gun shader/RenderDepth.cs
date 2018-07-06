using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: RenderDepth
/// </summary>
[ExecuteInEditMode]
public class RenderDepth : MonoBehaviour {
	void OnEnable() {
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
	}
}