using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyAudioPlayer : MonoBehaviour {

    public AudioClip moveClip;

    public void PlayNewClip() {
        GetComponent<AudioSource>().clip = moveClip;
        GetComponent<AudioSource>().loop = false;
        GetComponent<AudioSource>().Play();
        Destroy(gameObject, moveClip.length + 0.5f);
    }

}
