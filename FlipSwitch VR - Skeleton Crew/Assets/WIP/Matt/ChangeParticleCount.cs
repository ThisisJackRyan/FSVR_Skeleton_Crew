using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeParticleCount : MonoBehaviour {

	public string systemName;
	public int particleCount;

	[Button]
	public void AdjustCounts() {
		foreach (var item in GameObject.FindObjectsOfType<ParticleSystem>()) {
			if(item.name == systemName) {
				var main = item.main;
				main.maxParticles = particleCount;
			}
		}
	}
}
