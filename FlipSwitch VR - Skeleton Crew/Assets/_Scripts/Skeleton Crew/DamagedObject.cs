using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;

public class DamagedObject : NetworkBehaviour {

	public enum DamageState {
		Full, ThreeQuarter, Half, Quarter, None
	}

	DamageState myState;
	[SyncVar( hook = "OnHealthChange" )] int health = 100;
	[SyncVar( hook = "OnPatternIndexChange" )] int rng = -1;


	[Tooltip( "the number health percent much be at to reach the given damage state. anything below quarter is completely broken." )]
	public int fullAmount = 90, threeQuarterAmount = 75, halfAmount = 50, quarterAmount = 25, maxHealth = 100;

	public Transform fullState, threequarter, halfState, quarterState, deadState;
	public GameObject repairSphere;
	public RepairPattern[] repairPatterns;

	public GameObject dmgParticles, healParticles;
	public AudioClip damageClip, healClip;

	private void OnPatternIndexChange( int n ) {
		rng = n;
		repairPattern = repairPatterns[rng];
	}

	private void OnHealthChange( int n ) {
		if ( health > n ) {
			GetComponent<AudioSource>().PlayOneShot( damageClip );
			Instantiate( dmgParticles, transform.position, Quaternion.identity );
		} else if (health < n) {
			GetComponent<AudioSource>().PlayOneShot( healClip );
			Instantiate( healParticles, transform.position, Quaternion.identity );
		}

		health = n;

		if ( health >= fullAmount ) {
			myState = DamageState.Full;
		} else if ( health >= threeQuarterAmount ) {
			myState = DamageState.ThreeQuarter;
		} else if ( health >= halfAmount ) {
			myState = DamageState.Half;
		} else if ( health >= quarterAmount ) {
			myState = DamageState.Quarter;
		} else {
			myState = DamageState.None;
		}

		if(health < maxHealth ) {
			repairSphere.SetActive( true );
			DisablePatternOnClients();
		} else {
			repairSphere.SetActive( false );
		}

		UpdateModel();

	}


	//// Use this for initialization
	//public override void OnStartServer() {
	//	base.OnStartServer();
	//}

	public void Start() {
		//ChangeHealth( maxHealth );

		if ( isServer ) {
			print( name + " enabled server check" );
			health = 0;
			Captain.damagedObjectsRepaired.Add( this, false );
		} else if ( isClient ) {
			OnHealthChange( health );
		}


	}

	RepairPattern repairPattern;

	internal RepairPattern SelectPattern() {
		if ( !isServer ) {
			return null;
		}

		rng = Random.Range( 0, repairPatterns.Length );
		repairPattern = repairPatterns[rng];
		print( "setting repairPattern to " + repairPattern.name );
		foreach ( var pat in repairPatterns ) {
			pat.gameObject.SetActive( false );
		}

		return repairPattern;
	}

	internal void DisablePatternOnClients() {
		if ( !isServer ) {
			return;
		}

		RpcDisablePattern();
	}

	[ClientRpc]
	private void RpcDisablePattern() {
		repairPattern.gameObject.SetActive( false );
		if ( health < maxHealth ) {
			repairSphere.transform.GetChild(0).gameObject.SetActive( true );
		}
	}

	internal void EnablePatternOnClients() {
		if ( !isServer ) {
			return;
		}

		RpcEnablePattern();
	}

	[ClientRpc]
	private void RpcEnablePattern() {
		repairPattern.gameObject.SetActive( true );//

		repairSphere.transform.GetChild(0).gameObject.SetActive(false);
	}

	internal void DisableRepairNode( int index ) {
		if ( !isServer ) {
			return;
		}

		RpcDisableNode( index );
	}

	[ClientRpc]
	private void RpcDisableNode( int index ) {
		print( "pattern name: " + repairPattern.name );
		print( "index received: " + index );
		repairPattern.transform.GetChild( index ).gameObject.SetActive( false );
	}

	internal void EnableRepairNode( int index ) {
		if ( !isServer ) {
			return;
		}
		repairPattern.transform.GetChild( 0 ).gameObject.SetActive( true );

		RpcEnableNode( index );
	}

	[ClientRpc]
	private void RpcEnableNode( int index ) {
		repairPattern.transform.GetChild( 0 ).gameObject.SetActive( true );
		repairPattern.transform.GetChild( index ).gameObject.SetActive( true );
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

	public int ChangeHealth( int amount, bool damage = true ) {
		print(name+"change health called");
		if ( !isServer )
			return health;

		if ( damage ) {
			health -= Mathf.Abs( amount );
			health = ( health < 0 ) ? 0 : health;
		} else {
			health += Mathf.Abs( amount );
			health = ( health > maxHealth ) ? maxHealth : health;
		}

		if ( health >= maxHealth ) {
			Captain.damagedObjectsRepaired[this] = true;
			Captain.instance.CheckDamagedObjects();
		}

		return health;
	}

	public int GetHealth() {
		return health;
	}
}
