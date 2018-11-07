using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAmbientSound : MonoBehaviour {

	public AudioClip ambientSound;
	public AudioClip bossFightMusic;

	private AudioSource source;

	private void Start() {
		source = GetComponent<AudioSource>();
		PlayAmbientSound();
	}

	public void PlayAmbientSound() {
		source.clip = ambientSound;
		source.Play();
	}

	public void PlayBossMusic() {
		source.clip = bossFightMusic;
		source.Play();
	}

}
