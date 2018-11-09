using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplay : MonoBehaviour {

	
	
	// Update is called once per frame
	void Update () {
		GetComponent<Text>().text =  VariableHolder.instance.GetPlayerScore( transform.root.gameObject );
	}
}
