using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: Player
/// </summary>
public class Player : NetworkBehaviour {
	#region Fields

	public int health;
	public int maxHealth = 100;

	#endregion

	public int ChangeHealth( int amount, bool damage = true ) {
		if ( !isServer )
			return health;

		if ( damage ) {
			health -= Mathf.Abs( amount );
			health = ( health < 0 ) ? 0 : health;
		} else {
			health += Mathf.Abs( amount );
			health = ( health > maxHealth ) ? maxHealth : health;
		}

		//any tie ins go here

		return health;
	}

	public int GetHealth() {
		return health;
	}
}