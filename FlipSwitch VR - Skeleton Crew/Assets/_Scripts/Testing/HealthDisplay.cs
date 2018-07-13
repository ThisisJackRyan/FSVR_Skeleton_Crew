using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour {

	public Text text;

	public DamagedObject dmgObj;
	public Enemy enemy;
	public ScriptSyncPlayer player;
    public Ratman rat;

	// Use this for initialization
	void Start() {}

	// Update is called once per frame
	void Update() {
		if (dmgObj) {
			text.text = dmgObj.GetHealth().ToString();
		}

		if (enemy) {
			text.text = enemy.health.ToString();
		}

		if (player) {
            text.text = player.GetHealth().ToString();
        }

        if (rat) {
            text.text = rat.health.ToString();
        }
    }
}