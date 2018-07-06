using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: CharacterSelection
/// </summary>
public class CharacterSelection : EventSwitch {
	#region Fields

	public GameObject player;
	public GameObject sceneSuit;
	public Texture[] textures;

	#endregion

	//wont use in demo
	public void ChangeColor(int i) {
		print("called");
		//1 blue, 2 red, 3 green, 4 yellow
		switch (i) {
			case 1:
				player.GetComponent<Renderer>().material.color = Color.blue;
				//sceneSuit.GetComponent<Renderer>().material.color = Color.blue;

				break;
			case 2:
				player.GetComponent<Renderer>().material.color = Color.red;
				//sceneSuit.GetComponent<Renderer>().material.color = Color.red;

				break;
			case 3:
				player.GetComponent<Renderer>().material.color = Color.green;
				//sceneSuit.GetComponent<Renderer>().material.color = Color.red;

				break;
			case 4:
				player.GetComponent<Renderer>().material.color = Color.yellow;
				//sceneSuit.GetComponent<Renderer>().material.color = Color.yellow;

				break;
		}
	}

	public void ChangeCharacter(int i) {
		//1 cop, 2 scientist, 3 student 4 assitant
		player.GetComponent<Renderer>().material.mainTexture = textures[i - 1];
		//sceneSuit.GetComponent<Renderer>().material.mainTexture = textures[i - 1];

	}
}