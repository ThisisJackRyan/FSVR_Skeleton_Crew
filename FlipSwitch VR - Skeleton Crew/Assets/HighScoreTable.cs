using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HighScoreTable : NetworkBehaviour {
	public int playerNumber;
	public Text[] scoreTexts;

	private void OnEnable() {
		if (!isServer) {
			print("returning as not server");

			return;
		}
		var player = GameObject.Find("Player " + playerNumber);
		var score = VariableHolder.instance.GetPlayerScore(player);

		print("player: " + player + " " + score);

		RpcUpdateScores( score.ToString() );
	}

	[ClientRpc]
	void RpcUpdateScores(string scores ) {

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
