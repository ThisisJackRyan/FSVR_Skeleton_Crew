using System.Collections.Generic;
using UnityEngine;

public class VariableHolder : MonoBehaviour {
	public List<GameObject> players;
	public List<GameObject> ratmen;
	public List<GameObject> mastTargets;
	public List<GameObject> cannons;

	
    public int numPlayers;

    public static VariableHolder instance;

    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;
    }

    public void NumPlayerChanged(string n)
    {
        Debug.Log("String passed in " + n);
        numPlayers = int.Parse(n);
        if(numPlayers > 4 || numPlayers < 1)
        {
            Debug.Log("Invalid number of player");
        }
    }
}
