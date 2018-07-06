using Dissonance;
using Dissonance.Audio.Playback;
using UnityEngine;

public class ScriptDissonanceChanger : MonoBehaviour {

    public VoiceBroadcastTrigger broadcastTriggerScript;
    public DissonanceComms dissCommScript;
    public VoicePlayback playbackPrefab;

    private VoicePlayback defaultPlayback;
    private bool isPositional = false;

    private void Start()
    {
        defaultPlayback = dissCommScript.PlaybackPrefab;
    }

    public void togglePositional()
    {
        broadcastTriggerScript.BroadcastPosition = !broadcastTriggerScript.BroadcastPosition;
        dissCommScript.PlaybackPrefab = isPositional ? defaultPlayback : playbackPrefab;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player" && !isPositional)
        {
            isPositional = true;
            togglePositional();
        }
    }
}
