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


	private void Awake()
	{
		if (instance != null ) {
			Destroy(gameObject);
		} else {

			instance = this;
		}
	}

    public void RemoveRangedUnit() {
        if(numRangedUnits - 1 >= 0) {
            numRangedUnits--; 
        }
    }

    public bool AddRangedUnit() {
        if(numRangedUnits + 1 < maxNumRangedUnits) {
            numRangedUnits++;
            return true;
        }

        return false;
    }

	public void NumPlayerChanged(string n)
	{
		//Debug.Log("String passed in " + n);
		numPlayers = int.Parse(n);
		if(numPlayers > 4 || numPlayers < 1)
		{
			Debug.Log("Invalid number of player");
		}
	}

	public void AddPlayerToScoreList(GameObject player) {
		if (!playerScores.ContainsKey(player)) {
			playerScores.Add( player, new PlayerScore());
		} else {
			print("player already added to score list: " + player.name);
		}
	}

	public void IncreasePlayerScore(GameObject player, PlayerScore.ScoreType type, Vector3 pointPosition) {
		if ( !playerScores.ContainsKey(player) ) {
			Debug.LogWarning("Player: " + player.name + " was not in player score dictionary when score increase was made, adding them now. \n"+
							 "The playerscore dictionary holds " + playerScores.Count + " before adding " + player.name + ".");

			AddPlayerToScoreList( player );
		} 

		switch ( type ) {
			case PlayerScore.ScoreType.RatkinKills:
				playerScores[player].ratkinKills ++;
				break;
			case PlayerScore.ScoreType.SkeletonKills:
				playerScores[player].skeletonKills ++;
				break;
			case PlayerScore.ScoreType.DragonkinKills:
				playerScores[player].dragonkinKills ++;
				break;
			case PlayerScore.ScoreType.Repairs:
				playerScores[player].repairs ++;
				break;
			case PlayerScore.ScoreType.Deaths:
				playerScores[player].deaths ++;
				break;
			case PlayerScore.ScoreType.crystalsDetroyed:
				playerScores[player].crystalsDetroyed ++;
				break;
			case PlayerScore.ScoreType.BoatsDestroyed:
				playerScores[player].boatsDestroyed ++;
				break;
			case PlayerScore.ScoreType.CaptainDamage:
				playerScores[player].captainDamage ++;
				break;
		}

		playerScores[player].points += (int)type;
		//spawn point display with (int)type as value. will need to call a method on the players networkBehaviour
		player.GetComponent<FSVRPlayer>().SpawnPointDisplay( pointPosition, (int)type, player );

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
			RatkinKills				= 150,
			SkeletonKills			= 250,
			DragonkinKills			= 750,
			Repairs					= 100,
			Deaths					= -100,
			crystalsDetroyed		= 500,
			BoatsDestroyed			= 1500,
			CaptainDamage			= 1000
		}
	}
}
