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
	}
}
