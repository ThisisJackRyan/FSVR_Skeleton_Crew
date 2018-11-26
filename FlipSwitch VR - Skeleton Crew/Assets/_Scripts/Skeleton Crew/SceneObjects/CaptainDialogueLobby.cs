using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class CaptainDialogueLobby : MonoBehaviour {

    public ClipAndDelay[] clips;
	public string[] subtitles;
    AudioSource source;

    public float finaleDelay;
    public UnityEvent finale; 
	// Use this for initialization
	void Start () {
        StartCoroutine("CaptainsSpeech");
        source = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	IEnumerator CaptainsSpeech() {
		int i = 0;
        foreach (var item in clips){
            yield return new WaitForSecondsRealtime(item.delay);
            source.clip = item.clip;
            source.Play();
			if (i < subtitles.Length) {

				if (PlayerHud.instance) {

				PlayerHud.instance.UpdateSubtitles(subtitles[i]);
				} else {
					print("hud instance is null");
				}
			}

			i++;
        }
        yield return new WaitForSecondsRealtime(finaleDelay);
        finale.Invoke();
    }
}

[System.Serializable]
public struct ClipAndDelay{
    public float delay;
    public AudioClip clip;
}
