using System.Collections.Generic;
using UnityEngine;

public class VariableHolder : MonoBehaviour {
	public List<GameObject> players;
	public List<GameObject> ratmen;
	public List<GameObject> mastTargets;
	public List<GameObject> cannons;
	public Dictionary<GameObject, bool> ratmenPositions = new Dictionary<GameObject, bool>();
	public Dictionary<GameObject, bool> enemyRangedPositions = new Dictionary<GameObject, bool>();
	public Dictionary<GameObject, bool> enemyMeleePositions = new Dictionary<GameObject, bool>();
	public int numPlayers;
	public int numRangedUnits;
	public int maxNumRangedUnits = 6;
	public static VariableHolder instance;
	public Dictionary<GameObject, PlayerScore> playerScores = new Dictionary<GameObject, PlayerScore>();


	private void Awake() {
		if (instance != null) {
			Destroy(gameObject);
		} else {

			instance = this;
		}
	}

	public void RemoveRangedUnit() {
		if (numRangedUnits - 1 >= 0) {
			numRangedUnits--;
		}
	}

	public bool AddRangedUnit() {
		if (numRangedUnits + 1 < maxNumRangedUnits) {
			numRangedUnits++;
			return true;
		}

		return false;
	}

	public void NumPlayerChanged(string n) {
		//Debug.Log("String passed in " + n);
		numPlayers = int.Parse(n);
		if (numPlayers > 3 || numPlayers < 1) {
			Debug.Log("Invalid number of player");
		}
	}

	public int phaseOneTimer, phaseTwoTimer, phaseThreeTimer, breakTimer, lobbyTimer, bossTimer;

	public void NumChangedPhaseOne(string n) {
		phaseOneTimer = int.Parse(n);
		//if (phaseOneTimer > 4 || phaseOneTimer < 1) {
		//	Debug.Log("Invalid number of player");
		//}
	}

	public void NumChangedPhaseTwo(string n) {
		phaseTwoTimer = int.Parse(n);
		//if (phaseTwoTimer > 4 || phaseTwoTimer < 1) {
		//	Debug.Log("Invalid number of player");
		//}
	}

	public void NumChangedPhaseThree(string n) {
		phaseThreeTimer = int.Parse(n);
		//if (phaseThreeTimer > 4 || phaseThreeTimer < 1) {
		//	Debug.Log("Invalid number of player");
		//}
	}

	public void NumChangedBreak(string n) {
		breakTimer = int.Parse(n);
		//if (breakTimer > 4 || breakTimer < 1) {
		//	Debug.Log("Invalid number of player");
		//}
	}

	public void NumChangedBoss(string n) {
		bossTimer = int.Parse(n);
		//if (bossTimer > 4 || bossTimer < 1) {
		//	Debug.Log("Invalid number of player");
		//}
	}

	public void NumChangedLobby(string n) {
		lobbyTimer = int.Parse(n);
		//if (lobbyTimer > 4 || lobbyTimer < 1) {
		//	Debug.Log("Invalid number of player");
		//}
	}

	public void AddPlayerToScoreList(GameObject player) {
		if (!playerScores.ContainsKey(player)) {
			playerScores.Add(player, new PlayerScore());
		} else {
			//print("player already added to score list: " + player.name);
		}
	}

	public string GetPlayerPoints(GameObject player) {
		if (!playerScores.ContainsKey(player)) {
			return "not in collection";
		} else {
			return playerScores[player].points.ToString();
		}
	}

	public PlayerScore GetPlayerScore(GameObject player) {
		if (!playerScores.ContainsKey(player)) {
			Debug.LogError(player.name + "not in collection");
			return null;
		} else {
			return playerScores[player];
		}
	}

	public void IncreasePlayerScore(GameObject player, PlayerScore.ScoreType type, Vector3 pointPosition) {
		if (!playerScores.ContainsKey(player)) {
			Debug.LogWarning("Player: " + player.name + " was not in player score dictionary when score increase was made, adding them now. \n" +
							 "The playerscore dictionary holds " + playerScores.Count + " before adding " + player.name + ".");

			AddPlayerToScoreList(player);
		} else {
			//print(player.name + " is in score collection, adding points");
		}

		switch (type) {
			case PlayerScore.ScoreType.RatkinKills://
				playerScores[player].ratkinKills++;
				break;
			case PlayerScore.ScoreType.SkeletonKills://
				playerScores[player].skeletonKills++;
				break;
			case PlayerScore.ScoreType.DragonkinKills://
				playerScores[player].dragonkinKills++;
				break;
			case PlayerScore.ScoreType.Repairs://
				playerScores[player].repairs++;
				break;
			case PlayerScore.ScoreType.Deaths://
				playerScores[player].deaths++;
				break;
			case PlayerScore.ScoreType.CrystalsDetroyed://
				playerScores[player].crystalsDetroyed++;
				break;
			case PlayerScore.ScoreType.BoatsDestroyed://
				playerScores[player].boatsDestroyed++;
				break;
			case PlayerScore.ScoreType.CaptainDamage://
				playerScores[player].captainDamage++;
				break;
		}

		playerScores[player].points += (int)type;
		//spawn point display with (int)type as value. will need to call a method on the players networkBehaviour
		player.GetComponent<FSVRPlayer>().SpawnPointDisplay(pointPosition, (int)type, player);

	}

	public class PlayerScore {
		public int points;
		public int ratkinKills;
		public int skeletonKills;
		public int dragonkinKills;
		public int repairs;
		public int deaths;
		public int crystalsDetroyed;
		public int boatsDestroyed;
		public int captainDamage;

		/// <summary>
		/// each value represents the point increase for each action.
		/// each value must be different
		/// </summary>
		public enum ScoreType {
			RatkinKills = 150,
			SkeletonKills = 250,
			DragonkinKills = 750,
			Repairs = 100,
			Deaths = -100,
			CrystalsDetroyed = 500,
			BoatsDestroyed = 1500,
			CaptainDamage = 1000
		}

		public static PlayerScore ParseAsPlayerScore(string scoreAsString) {
			PlayerScore toReturn = new PlayerScore();
			string[] s = scoreAsString.Split(',');

			toReturn.points = int.Parse(s[0]);
			toReturn.ratkinKills = int.Parse(s[1]);
			toReturn.skeletonKills = int.Parse(s[2]);
			toReturn.dragonkinKills = int.Parse(s[3]);
			toReturn.repairs = int.Parse(s[4]);
			toReturn.crystalsDetroyed = int.Parse(s[5]);
			toReturn.boatsDestroyed = int.Parse(s[6]);
			toReturn.captainDamage = int.Parse(s[7]);

			return toReturn;
		}

		public override string ToString() {
			return string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", points,
				ratkinKills, skeletonKills, dragonkinKills, repairs, deaths,
				crystalsDetroyed, boatsDestroyed, captainDamage);
		}
	}
}
