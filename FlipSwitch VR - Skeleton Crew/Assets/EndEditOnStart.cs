using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class EndEditOnStart : MonoBehaviour {

	// Use this for initialization
	void Start () {
		var i = GetComponent<InputField>();
		i.onEndEdit.Invoke(i.text);
	}

}
