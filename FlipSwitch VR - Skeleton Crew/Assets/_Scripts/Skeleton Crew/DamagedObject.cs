using UnityEngine;
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
	public int fullAmount = 90, threeQuarterAmount = 75, halfAmount = 50, quarterAmount = 25, maxHealth = 100;

	public Transform fullState, threequarter, halfState, quarterState, deadState;
	public GameObject repairSphere;

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


		if (isServer) {
			health = Random.Range(0, 50); 
            print(name + " enabled server check");
            Captain.damagedObjectsRepaired.Add(this, false);
            
        } else if (isClient) {
			OnHealthChange(health);
		}	
	}

	private void Update() {
		if ( health < 100 ) {
			repairSphere.SetActive( true );
		} else {
			repairSphere.SetActive( false );
		}
	}

	void UpdateModel() {
		switch ( myState ) {
			case DamageState.Full:
				fullState.gameObject.SetActive( true );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.ThreeQuarter:
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( true );
				halfState.gameObject.SetActive( false );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.Half:
				fullState.gameObject.SetActive( false );
				threequarter.gameObject.SetActive( false );
				halfState.gameObject.SetActive( true );
				quarterState.gameObject.SetActive( false );
				deadState.gameObject.SetActive( false );
				break;
			case DamageState.Quarter:
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
			health = (health > maxHealth) ? maxHealth : health;
		}

        if (health >= maxHealth) {
            Captain.damagedObjectsRepaired[this] = true;
            Captain.instance.CheckDamagedObjects();
        }

		return health;
	}

	public int GetHealth()
	{
		return health;
	}
}
