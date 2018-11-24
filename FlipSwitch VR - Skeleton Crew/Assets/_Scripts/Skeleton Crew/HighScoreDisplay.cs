using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HighScoreDisplay : MonoBehaviour {
	public int playerNumber;
	public Text[] scoreTexts;

	public bool Init() {		
		var player = GameObject.Find("Player " + playerNumber);
		if (player == null) {
			Debug.Log( "Player " + playerNumber + " was not found, disabling column" );
			return false;
		}

		var score = VariableHolder.instance.GetPlayerScore(player);
		print("player: " + player + " " + score);

		GetComponentInParent<HighScoreTable>().UpdateScores(this, score.ToString() );
		return true;
	}
	
	public void UpdateScores(string scores ) {

		var score = VariableHolder.PlayerScore.ParseAsPlayerScore( scores );
		print( score.ToString() );

		scoreTexts[0].text = score.points.ToString();
		scoreTexts[1].text = score.skeletonKills.ToString();
		scoreTexts[2].text = score.ratkinKills.ToString();
		scoreTexts[3].text = score.dragonkinKills.ToString();
		scoreTexts[4].text = score.repairs.ToString();
		scoreTexts[5].text = score.deaths.ToString();
		scoreTexts[6].text = score.crystalsDetroyed.ToString();
		scoreTexts[7].text = score.boatsDestroyed.ToString();
		scoreTexts[8].text = score.captainDamage.ToString();
	}
}
