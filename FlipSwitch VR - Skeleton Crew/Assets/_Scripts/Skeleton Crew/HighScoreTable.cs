using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HighScoreTable : NetworkBehaviour {

	public HighScoreDisplay[] scoreDisplays;

	// Use this for initialization
	public void DisplayScores () {
		if ( !isServer ) {
			print( "returning as not server" );

			return;
		}

		for ( int i = 0; i < scoreDisplays.Length; i++ ) {

			if ( !scoreDisplays[i].Init() ) {
				scoreDisplays[i].gameObject.SetActive(false);
				RpcDisableColumn( i );
			}

		}
	}
	
	internal void UpdateScores(HighScoreDisplay display, string score ) {
		for ( int i = 0; i < scoreDisplays.Length; i++ ) {
			if (display == scoreDisplays[i]) {
				RpcUpdateTableScores( i, score );
				return;
			}
		}

		Debug.LogError(display.name + " was not in the highscore table.");
	}
	[ClientRpc]
	void RpcDisableColumn(int i) {
		scoreDisplays[i].gameObject.SetActive( false );

	}


	[ClientRpc]
	void RpcUpdateTableScores(int displayIndex, string score ) {
		scoreDisplays[displayIndex].UpdateScores( score );
	}
}
