using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class ScriptSyncPlayer : NetworkBehaviour {

    [SyncVar(hook = "OnHealthChange" )] int health = 100;

    private void OnHealthChange(int n)
    {
        health = n;
    }

    bool hasStartedTutorial = false;

    public void TellCaptainToStartTutorial() {
        if (isServer && !hasStartedTutorial) {
            hasStartedTutorial = true;
            StartCoroutine("WaitToTellCaptain");
        }
    }

    IEnumerator WaitToTellCaptain() {
        yield return new WaitForSecondsRealtime(3);
        FindObjectOfType<Captain>().StartTutorial();
    }

	public int ChangeHealth( int amount, bool damage = true ) {
		if ( damage ) {
			health -= Mathf.Abs( amount );
		} else {
			health += Mathf.Abs( amount );
		}
		print( "new health: " + health );
		return health;
	}

	public int GetHealth()
    {
        return health;
    }
}
