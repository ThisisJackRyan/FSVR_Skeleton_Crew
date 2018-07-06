﻿using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using UnityEngine.UI;

public class DamagedObject : NetworkBehaviour {

	public enum DamageState {
		Full, ThreeQuarter, Half, Quarter, None
	}

	DamageState myState;
	[SyncVar( hook = "OnHealthChange" )] int health = 100;

	[Tooltip( "the number health percent much be at to reach the given damage state. anything below quarter is completely broken." )]
	public int fullAmount = 90, threeQuarterAmount = 75, halfAmount = 50, quarterAmount = 25;

	public Transform fullState, threequarter, halfState, quarterState, deadState;
	public Transform fullStateSpawnPos, threeQuarterSpawnPos, halfSpawnPos, quarterSpawnPos;
	public Slider repairSlider;
	public GameObject repairSphere;

	private Cannon cannonScript;

	private void OnHealthChange( int n ) {

		health = n;

		if ( health >= fullAmount ) {
			myState = DamageState.Full;
		} else if(health >= threeQuarterAmount ) {
			myState = DamageState.ThreeQuarter;
		} else if ( health >= halfAmount ) {
			myState = DamageState.Half;
		} else if ( health >= quarterAmount ) {
			myState = DamageState.Quarter;
		} else {
			myState = DamageState.None;
		}

		UpdateModel();

	}

	//// Use this for initialization
	//public override void OnStartServer() {
	//	base.OnStartServer();
	//}

	public void Start()
	{
		cannonScript = GetComponent<Cannon>();

		if (isServer) {
			health = Random.Range(0, 50);
		}else if (isClient) {
			OnHealthChange(health);
		}


		repairSlider.minValue = 0;
		repairSlider.maxValue = 100;		
	}

	//public override void OnStartClient()
	//{
	//	base.OnStartClient();
	//	if (isServer) {
	//		return;
	//	}
	//	OnHealthChange(health);
	//}


	private void Update() {
		if ( health < 100 ) {
			repairSlider.transform.parent.gameObject.SetActive(true);
			repairSphere.SetActive( true );
		} else {
			repairSlider.transform.parent.gameObject.SetActive( false );
			repairSphere.SetActive( false );
		}
	}

	void UpdateModel() {
		switch ( myState ) {
			case DamageState.Full:
				cannonScript.spawnPos = fullStateSpawnPos;
				fullState.gameObject.SetActive( true );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.ThreeQuarter:
				cannonScript.spawnPos = threeQuarterSpawnPos;
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( true );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.Half:
				cannonScript.spawnPos = halfSpawnPos;
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( true );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.Quarter:
				cannonScript.spawnPos = quarterSpawnPos;
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( true );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.None:
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( true );
				break;
		}
	
	}

	public int ChangeHealth(int amount, bool damage = true ) {
		if (!isServer)
			return health;

		if (damage) {
			health -= Mathf.Abs(amount);
			health = (health < 0) ? 0 : health;
		} else {
			health += Mathf.Abs( amount );
			health = (health > 100) ? 100 : health;
		}
		return health;
	}

	public int GetHealth()
	{
		return health;
	}

	[Command]
	void CmdZeroHealth()
	{
		print("health before: " + health);
		ChangeHealth(100);
		print("health after: " + health);
	}
	
	[Command]
	void CmdFullHealth() {
		print("health before: "+ health);
		ChangeHealth(100, false);
		print("health after: " + health);
	}
}
