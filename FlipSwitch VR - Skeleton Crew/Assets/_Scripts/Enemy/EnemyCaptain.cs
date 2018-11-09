using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using Opsive.ThirdPersonController.Abilities;
using Opsive.ThirdPersonController;
using BehaviorDesigner.Runtime;

public class EnemyCaptain : NetworkBehaviour {

	#region Variables
	public static EnemyCaptain instance;

	[Header( "Boss Fight Intro" )]
	// Publics
	public AudioClip[] introAudioClips;
	public GameObject[] playerTeleportAreas;
	public GameObject[] colorChangeParticles;
	public GameObject initialTeleportTargetPositionParticle;
	public GameObject initialTeleportCurrentPositionParticle;
	public Color targetColor;
	public GameObject captainTeleportTarget;
	public float timeBetweenSecondTeleportClipAndCannon = 4.5f;
	public float timeFromCannonToInitialDrain = 5f;

	// Privates
	private readonly int INTRO_CLIP = 0;
	private readonly int TELEPORT1_CLIP = 1;		// now unused
	private readonly int TELEPORT2_CLIP = 2;
	private readonly int CANNON_CLIP = 3;
	private readonly int START_OF_DRAIN_CLIP = 4;
	private readonly int SECOND_DRAIN_CLIP = 5;
	private readonly int THIRD_DRAIN_CLIP = 6;
	private readonly int END_OF_INTRO_CLIP = 7;

	private bool playersHaveTeleported;
	private bool firstTimeDrain = true;
	private bool firstTeleport = true;
	private int playersReadyForTeleport;

	[Header( "Dragonkin Summoning" )]
	// Publics
	public GameObject[] rangedSpawnPositions;
	public GameObject[] meleeSpawnPositions;
	public GameObject dragonkinSpawnParticles;
	public float timeBetweenParticlesAndEnemySpawn;
	public GameObject meleeDragonkin, rangedDragonkin;
	public List<GameObject> cannonsForMelee;
	public AudioClip[] summonAudioClips;

	// Privates
	private int numRanged = 1, numMelee = 1;
	private int summonClipToPlay;

	[Header("Meteor Summoning")]
	// Publics
	public GameObject[] meteorsToEnable;
	public AudioClip[] meteorAudioClips;

	// Privates
	private int curActiveIndex1 = -1;
	private int curActiveIndex2 = -1;
	private int curActiveIndex3 = -1;
	private int curActiveIndex4 = -1;
	private int meteorSoundIndex;

	[Header("Teleporting")]
	// Publics
	public GameObject[] captainTeleportPositionsSafe;
	public GameObject[] captainTeleportPositionsDrain;
	public GameObject captainCurrentPositionTeleportParticles;
	public GameObject captainTargetPositionTeleportParticles;
	public AudioClip teleportSound;

	// Privates
	private int currentIndexForSafety;
	private int currentIndexForDraining;
	private int curTeleportNumber;
	private BehaviorTree myTree;

	[Header( "EnergyDraining" )]
	// Publics
	public GameObject particlesToSpawnOnDragon;
	public GameObject energyTrail;
	public Transform skullTransform;
	public GameObject skullParticles;
	public GameObject dragon;
	public float timeToCaptainWinningInSeconds;
	public AudioClip[] drainAudioClips;

	// Privates
	private float curTime = 0f;
	private GameObject energyTrailInstance;
	private GameObject particlesOnDragonInstance;
	private bool isDraining, canDrain = true;
	private int drainClipToPlay;

	[Header("End Game")]
	// Publics
	public GameObject highScoreTable;

	// Privates


	[Space]
	[Header("Captain Defeat")]
	// Publics
	public GameObject captainRagdoll;
	public AudioClip defeatAudio;
	public GameObject defeatPosition;
	public GameObject defeatParticle1;
	public GameObject defeatParticle2;

	// Privates
	private GameObject defeatParticleInstance1;
	private GameObject defeatParticleInstance2;

	[Header("Captain Victory")]
	//Publics
	public GameObject[] victoryParticles;
	public AudioClip victoryAudio;
	public GameObject victoryPosition;

	// Privates
	[SyncVar] private bool lastTimeDrain;
	private GameObject trail1, trail2, trail3;
	private GameObject target1, target2, target3;
	private bool finalTeleport;
	private bool captainDeathParticlesSpawned;
	private Transform explosionPosition;

	[Header("Other")]
	// Publics
	public int difficulty = 1;
	public int maxDifficulty = 10;
	public int difficultyModifier = 0;
	public int timesToDeath = 6;
	public BossAmbientSound ambientSoundRef;

	// Privates
	private int incrementSize;
	private AudioSource source;
	private int numberOfTimesHit;

	#endregion

	#region Initialization

	// Use this for initialization
	void Start () {
		source = GetComponent<AudioSource>();

		if ( !isServer ) {
			return;
		}

		VariableHolder.instance.enemyRangedPositions = new Dictionary<GameObject, bool>();
		VariableHolder.instance.enemyMeleePositions = new Dictionary<GameObject, bool>();		

		VariableHolder.instance.enemyRangedPositions.Clear();
		VariableHolder.instance.enemyMeleePositions.Clear();

		foreach(var r in rangedSpawnPositions ) {
			VariableHolder.instance.enemyRangedPositions.Add( r, false );
		}

		foreach(var m in meleeSpawnPositions ) {
			VariableHolder.instance.enemyMeleePositions.Add( m, false );
		}

		incrementSize = (int) Mathf.Ceil((float)(VariableHolder.instance.numPlayers) / 2) + difficultyModifier;
		
		myTree = GetComponent<BehaviorTree>();
		
		myTree.SetVariableValue( "dragon", dragon );

		StartCoroutine( "BossFightIntro" );

		if (instance != null) {
			Destroy(gameObject);
		} else {
			instance = this;
		}
	}

	#endregion

	#region Boss Fight Intro

	IEnumerator BossFightIntro() {
		// Intro clip
		yield return new WaitForSeconds( 5 );
		RpcPlayDialogue(INTRO_CLIP);
		source.clip = introAudioClips[INTRO_CLIP];
		source.Play();
		yield return new WaitForSeconds( introAudioClips[INTRO_CLIP].length);
		print("enable teleport pads called");
		EnableTeleportPads();

		yield return new WaitUntil(() => playersHaveTeleported == true );
		for (int i = 0; i < NumberOfPlayerHolder.instance.numberOfPlayers; i++) {
			playerTeleportAreas[i].SetActive(false);
		}

		yield return new WaitForSeconds(2f);
		StartCoroutine( "CaptainTeleport" );

		yield return new WaitForSeconds( 3.5f );
		RpcPlayDialogue(TELEPORT2_CLIP);
		print( "post teleport clip" );
		source.clip = introAudioClips[TELEPORT2_CLIP];
		source.Play();

		yield return new WaitForSeconds( introAudioClips[TELEPORT2_CLIP].length + timeBetweenSecondTeleportClipAndCannon);
		RpcPlayDialogue(CANNON_CLIP);
		print( "cannon clip" );
		source.clip = introAudioClips[CANNON_CLIP];
		source.Play();

		yield return new WaitForSeconds(introAudioClips[CANNON_CLIP].length + timeFromCannonToInitialDrain);
		RpcPlayDialogue(START_OF_DRAIN_CLIP);
		source.clip = introAudioClips[START_OF_DRAIN_CLIP];
		source.Play();

		yield return new WaitForSeconds(introAudioClips[START_OF_DRAIN_CLIP].length + 1.5f);
		StartDrainAbility();
	
		yield return new WaitForSeconds(4.5f);
		RpcPlayDialogue(SECOND_DRAIN_CLIP);
		source.clip = introAudioClips[SECOND_DRAIN_CLIP];
		source.Play();

		yield return new WaitForSeconds( introAudioClips[SECOND_DRAIN_CLIP].length + 2f );
		RpcPlayDialogue(THIRD_DRAIN_CLIP);
		RpcMakeCaptainEvil();
		StartCoroutine(ScaleOverTime( transform, new Vector3( 1.5f, 1.5f, 1.5f ), introAudioClips[THIRD_DRAIN_CLIP].length -1.5f ) );
		StartCoroutine(SwapColorOverTime(introAudioClips[THIRD_DRAIN_CLIP].length - 1.5f));
		source.clip = introAudioClips[THIRD_DRAIN_CLIP];
		source.Play();

		yield return new WaitForSeconds(introAudioClips[THIRD_DRAIN_CLIP].length + 3.5f);
		RpcPlayDialogue(END_OF_INTRO_CLIP);
		source.clip = introAudioClips[END_OF_INTRO_CLIP];
		source.Play();

		yield return new WaitForSeconds(introAudioClips[END_OF_INTRO_CLIP].length - 1.5f);
		StartBossMusic();

		print( "drain clip" );
		myTree.SetVariableValue( "introFinished", true );

	}

	private void StartBossMusic() {
		ambientSoundRef.PlayBossMusic();
		RpcStartBossMusic();
	}

	[ClientRpc]
	private void RpcStartBossMusic() {
		if (isServer) {
			return;
		}

		ambientSoundRef.PlayBossMusic();
	}

	[ClientRpc]
	private void RpcMakeCaptainEvil() {
		if (isServer) {
			return;
		}

		StartCoroutine(ScaleOverTime(transform, new Vector3(1.5f, 1.5f, 1.5f), introAudioClips[THIRD_DRAIN_CLIP].length - 1.5f));
		StartCoroutine(SwapColorOverTime(introAudioClips[THIRD_DRAIN_CLIP].length - 1.5f));
	}

	[ClientRpc]
	private void RpcPlayDialogue(int index) {
		if (isServer) {
			return;
		}

		source.clip = introAudioClips[index];
		source.Play();
	}

	private void EnableTeleportPads() {
		for (int i = 0; i < NumberOfPlayerHolder.instance.numberOfPlayers; i++) {
			print("should have activated pad " + (i + 1));
			playerTeleportAreas[i].SetActive(true);
			print(playerTeleportAreas[i].name + " should be set to active");
			NetworkServer.Spawn(playerTeleportAreas[i]);
		}
	}

	[Button]
	public void PlayerSteppedOnPad() {
		if (!isServer) {
			return;
		}
		print("player stepped on pad called");
		playersReadyForTeleport++;
		DetermineIfPlayersCanTeleport();
	}

	[Button]
	public void PlayerSteppedOffPad() {
		if (!isServer) {
			return;
		}

		playersReadyForTeleport--;
	}

	private void DetermineIfPlayersCanTeleport() {
		print("determine if can teleport called");
		if (playersReadyForTeleport == NumberOfPlayerHolder.instance.numberOfPlayers) {
			print("players ready for teleport is equal to number of players in game");
			RpcTeleportPlayers();
			print("players should be teleported");
			playersHaveTeleported = true;
			print("dialogue should be continuing");
		}
	}

	[ClientRpc]
	private void RpcTeleportPlayers() {
		if (isServer) {
			return;
		}

		foreach (var v in FindObjectsOfType<FSVRPlayer>()) {
			if (v.isLocalPlayer) {
				v.transform.position = new Vector3(25, 0, 0);
			}
		}
	}

	// Scale over time gotten from https://stackoverflow.com/questions/46587150/scale-gameobject-over-time
	bool isScaling = false;

	IEnumerator ScaleOverTime( Transform objectToScale, Vector3 toScale, float duration ) {
		//Make sure there is only one instance of this function running
		if ( isScaling ) {
			yield break; ///exit if this is still running
		}
		isScaling = true;

		float counter = 0;

		//Get the current scale of the object to be moved
		Vector3 startScaleSize = objectToScale.localScale;

		while ( counter < duration ) {
			counter += Time.deltaTime;
			objectToScale.localScale = Vector3.Lerp( startScaleSize, toScale, counter / duration );
			yield return null;
		}

		isScaling = false;
	}
	
	bool isChangingColor = false;

	IEnumerator SwapColorOverTime(float duration) {
		//Make sure there is only one instance of this function running
		if (isChangingColor) {
			yield break; ///exit if this is still running
		}
		isChangingColor = true;

		float counter = 0;

		//Get the current scale of the object to be moved
		Color startColor = colorChangeParticles[0].GetComponent<ParticleSystem>().startColor;
		Vector3 startScale = colorChangeParticles[0].transform.localScale;

		while (counter < duration) {
			counter += Time.deltaTime;
			foreach(var v in colorChangeParticles) {
				v.GetComponent<ParticleSystem>().startColor = Color.Lerp(startColor, targetColor, counter / (duration * 0.5f));
				v.transform.localScale = Vector3.Lerp(startScale, new Vector3(3f, 3f, 3f), counter / duration);
			}
			yield return null;
		}

		isScaling = false;
	}

	#endregion

	#region General

	public void ModifyDifficultyIncrement(int toAdd) {
		incrementSize += toAdd;
	}

	public void IncrementDifficulty() {
		difficulty += incrementSize;
		difficulty = (difficulty < maxDifficulty) ? difficulty : maxDifficulty;
		print("increment difficulty called, increment size is " + incrementSize + " new difficulty is " + difficulty);
	}

	#endregion	

	#region Initial Draining

	[Button]
	public void StartDrainAbility() {
		if (!isServer) {
			return;
		}

		StartCoroutine( "DrainEnergy" );
	}

	IEnumerator DrainEnergy() {
		string abName = "DrainEnergy";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;
		print( "drain energy ability tried start" );
		GetComponent<ControllerHandler>().TryStartAbility( ab );
		print( "after try start drain ability" );
		yield return new WaitForSecondsRealtime( introAudioClips[START_OF_DRAIN_CLIP].length + introAudioClips[SECOND_DRAIN_CLIP].length + introAudioClips[THIRD_DRAIN_CLIP].length + 5.5f);
		GetComponent<ControllerHandler>().TryStopAbility( ab );
		Destroy( energyTrailInstance );
		Destroy( particlesOnDragonInstance );
		skullParticles.SetActive( false );
		firstTimeDrain = false;
		RpcDestroyDrain();
		canDrain = false;
		isDraining = false;
	}

	#endregion

	#region Drain Energy From Dragon

	public bool CanDrainFromDragon() {
		return canDrain;
	}

	public void DrainEnergyFromDragon() {			// called by animation event
		print( "Drain EnergyFromDragon called" );

		if (lastTimeDrain) {
			print("draining from players");
			SpawnFinalDrainObjects();
			return;
		} else {

			if (isServer) {
				if (numberOfTimesHit % 2 != 0) {
					PlayDrainAudio(drainClipToPlay);
					drainClipToPlay++;
					drainClipToPlay = drainClipToPlay % drainAudioClips.Length;
				}
			}
			print("draining from dragon");
			StartCoroutine( "StartTheDrain" );
		}
	}

	public IEnumerator StartTheDrain() {
		particlesOnDragonInstance = Instantiate( particlesToSpawnOnDragon, dragon.transform.position, Quaternion.identity );

		yield return new WaitForSecondsRealtime( 2.5f );

		energyTrailInstance = Instantiate( energyTrail, transform.position, Quaternion.identity );

		foreach ( var v in energyTrailInstance.GetComponentsInChildren<LineRenderer>() ) {
			v.SetPosition( 0, dragon.transform.position );
			v.SetPosition( 1, skullTransform.position );
		}
		skullParticles.SetActive( true );
		isDraining = true;
		if (isServer && !firstTimeDrain) {
			myTree.SetVariableValue("isDraining", true);
		}
	}

	private void OnCollisionEnter( Collision collision ) {
		if ( !isServer ) {
			return;
		}

		if(collision.transform.tag == "CannonBallPlayer") {
			if ( isDraining ) {
				StopDrainingAbility();
				Destroy( energyTrailInstance );
				Destroy( particlesOnDragonInstance );
				skullParticles.SetActive( false );
				RpcDestroyDrain();
				isDraining = false;
				GetComponent<CharacterHealth>().Damage(0.1f, collision.contacts[0].point, Vector3.zero);
				VariableHolder.instance.IncreasePlayerScore(collision.transform.GetComponent<SCProjectile>().playerWhoFired, VariableHolder.PlayerScore.ScoreType.CaptainDamage, transform.position);
				NetworkServer.Destroy(collision.gameObject);
				numberOfTimesHit++;

			}

			CheckIfPlayersWin();
		}

	}

	[Button]
	public void PlayerWinWorkaround() {
		if (!isServer) {
			return;
		}

		numberOfTimesHit = timesToDeath;
		StopDrainingAbility();
		Destroy(energyTrailInstance);
		Destroy(particlesOnDragonInstance);
		skullParticles.SetActive(false);
		RpcDestroyDrain();
		CheckIfPlayersWin();
	}

	private void CheckIfPlayersWin() {
		if (numberOfTimesHit >= timesToDeath) {
			myTree.DisableBehavior();                               // Stop him from doing more.
			Debug.LogWarning("Players have won the game");
			StartCoroutine(PlayerVictory());
		}
	}

	private void Update() {
		if (lastTimeDrain) { // updates the end point of the drain so it follows the player as they move around
			if (trail1) {
				foreach (var v in trail1.GetComponentsInChildren<LineRenderer>()) {
					v.SetPosition(0, target1.transform.position);
					v.SetPosition(1, skullTransform.position);
				}
			}

			if (trail2) {
				foreach (var v in trail2.GetComponentsInChildren<LineRenderer>()) {
					v.SetPosition(0, target2.transform.position);
					v.SetPosition(1, skullTransform.position);
				}
			}

			if (trail3) {
				foreach (var v in trail3.GetComponentsInChildren<LineRenderer>()) {
					v.SetPosition(0, target3.transform.position);
					v.SetPosition(1, skullTransform.position);
				}
			}
		}

		if (energyTrailInstance && firstTimeDrain) {
			foreach(var v in energyTrailInstance.GetComponentsInChildren<LineRenderer>()) {
				v.SetPosition(1, skullTransform.position);
			}
		}

		if ( !isServer ) {
			return;
		}
		if ( firstTimeDrain ) {
			return;
		}

		if ( isDraining ) {
			curTime += Time.deltaTime;
			print( "is draining. Current Elapsed Time: " + curTime );
			if(curTime >= timeToCaptainWinningInSeconds ) {				// Player defeat / Captain victory check
				StopDrainingAbility();                                  // Stop draining
				skullParticles.SetActive(false);
				Destroy(energyTrailInstance);
				Destroy(particlesOnDragonInstance);
				RpcDestroyDrain();
				myTree.DisableBehavior();								// Stop him from doing more.
				StartCoroutine(PlayerDefeat());
			}
		}
	}

	private void StopDrainingAbility() {
		string abName = "DrainEnergy";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStopAbility( ab );
		myTree.SetVariableValue("isDraining", false);
	}

	[ClientRpc]
	private void RpcDestroyDrain() {
		if (isServer) {
			return;
		}

		skullParticles.SetActive(false);
		Destroy(energyTrailInstance);
		Destroy(particlesOnDragonInstance);
	}

	private void PlayDrainAudio(int index) {
		RpcPlayDrainAudio(index);
		source.clip = drainAudioClips[index];
		source.Play();
	}

	[ClientRpc]
	private void RpcPlayDrainAudio(int index) {
		if (isServer) {
			return;
		}

		source.clip = drainAudioClips[index];
		source.Play();

	}

	#endregion

	#region Teleport Testing

	[Button]
	public void TeleportingToSafety() {
		if (!isServer) {
			return;
		}

		myTree.SetVariableValue("teleportingToSafety", true);
	}

	[Button]
	public void TeleportingToDrain() {
		if (!isServer) {
			return;
		}

		myTree.SetVariableValue("teleportingToSafety", false);
	}

	[Button]
	public void StartTeleportAbility() {
		if (!isServer) {
			return;
		}

		StartCoroutine( "CaptainTeleport" );
	}

	IEnumerator CaptainTeleport() {
		string abName = "CaptainTeleport";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility( ab );
		yield return new WaitForSecondsRealtime( 2.5f );
		GetComponent<ControllerHandler>().TryStopAbility( ab );
	}
	#endregion

	#region Teleporting

	// toSafe: 1 teleports to safe target, otherwise it teleports to a drain target
	public void TeleportCaptain() {
		if (!isServer) {
			return;
		}

		GameObject tpTarget;
		int temp;

		if ( firstTeleport ) {
			PlayTeleportSound();
			// Spawn the ball particles at current position
			GameObject tpCurPos = Instantiate(initialTeleportCurrentPositionParticle, transform.position, Quaternion.identity);
			tpCurPos.transform.position = new Vector3(tpCurPos.transform.position.x, tpCurPos.transform.position.y + 1.5f, tpCurPos.transform.position.z);
			NetworkServer.Spawn(tpCurPos);

			// Spawn the flame particles at target position
			tpTarget = Instantiate( initialTeleportTargetPositionParticle, captainTeleportTarget.transform.position, Quaternion.identity );
			NetworkServer.Spawn( tpTarget );

			firstTeleport = false;
		} else if (finalTeleport) {
			// Spawn the ball particles at current position
			GameObject tpCurPos = Instantiate(captainCurrentPositionTeleportParticles, transform.position, Quaternion.identity);
			tpCurPos.transform.position = new Vector3(tpCurPos.transform.position.x, tpCurPos.transform.position.y + 1.5f, tpCurPos.transform.position.z);
			NetworkServer.Spawn(tpCurPos);

			// Spawn the flame particles at target position
			tpTarget = Instantiate(captainTargetPositionTeleportParticles, captainTeleportTarget.transform.position, Quaternion.identity);
			NetworkServer.Spawn(tpTarget);
		} else {
			PlayTeleportSound();

			GameObject tpCurPos = Instantiate(captainCurrentPositionTeleportParticles, transform.position, Quaternion.identity);
			tpCurPos.transform.position = new Vector3(tpCurPos.transform.position.x, tpCurPos.transform.position.y + 1.5f, tpCurPos.transform.position.z);
			NetworkServer.Spawn(tpCurPos);

			if ( (bool)myTree.GetVariable( "teleportingToSafety" ).GetValue() ) {
				do {
					temp = Random.Range( 0, captainTeleportPositionsSafe.Length );
				} while ( temp == currentIndexForSafety );

				currentIndexForSafety = temp;
				tpTarget = Instantiate( captainTargetPositionTeleportParticles, captainTeleportPositionsSafe[currentIndexForSafety].transform.position, Quaternion.identity );
			} else {
				do {
					temp = Random.Range( 0, captainTeleportPositionsDrain.Length );
				} while ( temp == currentIndexForDraining );

				currentIndexForDraining = temp;
				tpTarget = Instantiate( captainTargetPositionTeleportParticles, captainTeleportPositionsDrain[currentIndexForDraining].transform.position, Quaternion.identity );
			}

			NetworkServer.Spawn( tpTarget );
			canDrain = !(bool)myTree.GetVariable( "teleportingToSafety" ).GetValue();
		}

		transform.position = tpTarget.transform.position;
	}

	private void PlayTeleportSound() {
		RpcPlayTeleportSound();
		source.clip = teleportSound;
		source.Play();
	}

	[ClientRpc]
	private void RpcPlayTeleportSound() {
		if (isServer) {
			return;
		}

		source.clip = teleportSound;
		source.Play();
	}

	#endregion

	#region Meteor Summoning Testing

	[Button]
	public void StartSummonMeteorAbility() {
		if (!isServer) {
			return;
		}

		StartCoroutine("MeteorSummon");
	}

	IEnumerator MeteorSummon() {
		string abName = "SummonMeteor";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents(TaskUtility.GetTypeWithinAssembly(abName));

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility(ab);
		yield return new WaitForSecondsRealtime(2.5f);
		GetComponent<ControllerHandler>().TryStopAbility(ab);
	}

	#endregion

	#region Meteor Summoning

	public void SpawnMeteor() {
		if (!isServer) {
			return;
		}

		if(numberOfTimesHit % 2 == 0) {
			PlayMeteorSound(meteorSoundIndex);
			meteorSoundIndex++;
			meteorSoundIndex = meteorSoundIndex % meteorAudioClips.Length;
		}

		List<int> meteorIndexHolder = new List<int> {
			1,
			2,
			3,
			4
		};

		int temp = -1;

		switch (difficulty) {
			case 1:
				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex1 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);
				RpcEnableMeteor(curActiveIndex1);
				break;
			case 2:
				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex1 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex2 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				RpcEnableMeteor(curActiveIndex1);
				RpcEnableMeteor(curActiveIndex2);
				break;
			case 3:
				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex1 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex2 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex3 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				RpcEnableMeteor(curActiveIndex1);
				RpcEnableMeteor(curActiveIndex2);
				RpcEnableMeteor(curActiveIndex3);
				break;
			default:            // 4+
				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex1 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex2 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex3 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				temp = Random.Range(0, meteorIndexHolder.Count);
				curActiveIndex4 = meteorIndexHolder[temp];
				meteorIndexHolder.RemoveAt(temp);

				RpcEnableMeteor(curActiveIndex1);
				RpcEnableMeteor(curActiveIndex2);
				RpcEnableMeteor(curActiveIndex3);
				RpcEnableMeteor(curActiveIndex4);
				break;
		}
		Invoke("EnableMeteors", 0.1f);
		Invoke("DisableMeteors", 5.5f);
	}

	private void EnableMeteors() {
		if (curActiveIndex1 != -1) {
			meteorsToEnable[curActiveIndex1].SetActive(true);
		}
		if(curActiveIndex2 != -1) {
			meteorsToEnable[curActiveIndex2].SetActive(true);
		}
		if (curActiveIndex3 != -1) {
			meteorsToEnable[curActiveIndex3].SetActive(true);
		}
		if (curActiveIndex4 != -1) {
			meteorsToEnable[curActiveIndex4].SetActive(true);
		}
	}

	[ClientRpc]
	private void RpcEnableMeteor(int index) {
		if (isServer) {
			return;
		}

		if (curActiveIndex1 == -1) {
			curActiveIndex1 = index;
			meteorsToEnable[curActiveIndex1].SetActive(true);
		} else if (curActiveIndex2 == -1) {
			curActiveIndex2 = index;
			meteorsToEnable[curActiveIndex2].SetActive(true);
		} else if (curActiveIndex3 == -1) {
			curActiveIndex3 = index;
			meteorsToEnable[curActiveIndex3].SetActive(true);
		} else if (curActiveIndex4 == -1) {
			curActiveIndex4 = index;
			meteorsToEnable[curActiveIndex4].SetActive(true);
		}
		Invoke("DisableMeteors", 5.5f);
	}

	private void DisableMeteors() {
		if (curActiveIndex1 != -1) {
			meteorsToEnable[curActiveIndex1].SetActive(false);
			curActiveIndex1 = -1;
		}
		if (curActiveIndex2 != -1) {
			meteorsToEnable[curActiveIndex2].SetActive(false);
			curActiveIndex2 = -1;
		}
		if (curActiveIndex3 != -1) {
			meteorsToEnable[curActiveIndex3].SetActive(false);
			curActiveIndex3 = -1;
		}
		if (curActiveIndex4 != -1) {
			meteorsToEnable[curActiveIndex4].SetActive(false);
			curActiveIndex4 = -1;
		}
	}

	private void PlayMeteorSound(int index) {
		RpcPlayMeteorSound(index);
		source.clip = meteorAudioClips[index];
		source.Play();
	}

	[ClientRpc]
	private void RpcPlayMeteorSound(int index) {
		if (isServer) {
			return;
		}

		source.clip = meteorAudioClips[index];
		source.Play();
	}

	#endregion

	#region Dragonkin Summoning Testing
	[Button]
	public void StartSpawnAbility() {
		if (!isServer) {
			return;
		}

		StartCoroutine( "DragonkinSummon" );
	}

	IEnumerator DragonkinSummon() {
		string abName = "SummonDragonkin";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents( TaskUtility.GetTypeWithinAssembly( abName ) );

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility( ab );
		yield return new WaitForSecondsRealtime( 2.5f );
		GetComponent<ControllerHandler>().TryStopAbility( ab );
	}

	#endregion

	#region Dragonkin Summoning

	public void SpawnDragonkin() {
		if ( !isServer) {
			return;
		}

		if (!VariableHolder.instance.enemyRangedPositions.ContainsValue(false) && !VariableHolder.instance.enemyMeleePositions.ContainsValue(false)) {
			return;
		}

		if (numberOfTimesHit % 2 == 0) {
			PlaySummonSound(summonClipToPlay);
			summonClipToPlay++;
			summonClipToPlay = summonClipToPlay % 2;
		}

		for (int i=0; i<difficulty; i++) {

			int rand = Random.Range(0, 2);

			if(rand == 0) {
				if (!VariableHolder.instance.enemyRangedPositions.ContainsValue(false)) {
					rand = 1;
				}
			} else if (rand == 1) {
				if (!VariableHolder.instance.enemyMeleePositions.ContainsValue(false)) {
					rand = 0;
				}
			}
			switch (rand) {
				case 0:                     // Range
					foreach (GameObject key in VariableHolder.instance.enemyRangedPositions.Keys) {
						if (VariableHolder.instance.enemyRangedPositions[key] == false) {
							GameObject p = Instantiate(dragonkinSpawnParticles, key.transform.position, key.transform.rotation);
							NetworkServer.Spawn(p);
							VariableHolder.instance.enemyRangedPositions[key] = true;
							StartCoroutine(GenerateEnemy(key, true, timeBetweenParticlesAndEnemySpawn));
							break;
						}
					}
					break;
				case 1:                     // Melee
					foreach (GameObject key in VariableHolder.instance.enemyMeleePositions.Keys) {
						if (VariableHolder.instance.enemyMeleePositions[key] == false) {
							GameObject p = Instantiate(dragonkinSpawnParticles, key.transform.position, key.transform.rotation);
							NetworkServer.Spawn(p);
							VariableHolder.instance.enemyMeleePositions[key] = true;
							StartCoroutine(GenerateEnemy(key, false, timeBetweenParticlesAndEnemySpawn));
							break;
						}
					}
					break;
			}

		}

	}

	IEnumerator GenerateEnemy(GameObject key, bool ranged, float delayTime ) {
		yield return new WaitForSeconds( delayTime );
		if ( ranged ) {
			numRanged++;
			GameObject g = Instantiate( rangedDragonkin, key.transform.position, key.transform.rotation );
			g.GetComponent<EnemyDragonkin>().SetMyPosition( key );
			//print("should be spawning ranged enemy");
			NetworkServer.Spawn( g );
		} else {
			numMelee++;
			GameObject g = Instantiate( meleeDragonkin, key.transform.position, key.transform.rotation );
			g.GetComponent<EnemyDragonkin>().SetMyPosition( key );
			g.GetComponent<BehaviorTree>().SetVariableValue("listOfCannons", cannonsForMelee);
			//print("should be spawning melee enemy");
			NetworkServer.Spawn( g );
		}
	}

	private void PlaySummonSound(int index) {
		RpcPlaySummonSound(index);
		source.clip = summonAudioClips[index];
		source.Play();
	}

	[ClientRpc]
	private void RpcPlaySummonSound(int index) {
		if (isServer) {
			return;
		}

		source.clip = summonAudioClips[index];
		source.Play();
	}

	#endregion

	#region End Game

	IEnumerator PlayerVictory() {
		StartDeathAbility();
		PlayEndGameAudio(true);
		yield return new WaitForSeconds(defeatAudio.length);
		foreach(var v in FindObjectsOfType<EnemyDragonkin>()) {
			v.TeleportToDeath();
		}
		SpawnDeathObjects(true);

		//yield return new WaitForSeconds(0.5f);
		transform.position = defeatPosition.transform.position; // defeat audio because variables are flipped
		yield return new WaitForSeconds(4f);
		EnableScoreboard();
	}

	IEnumerator PlayerDefeat() {
		isDraining = false;
		print("player defeat called");
		PlayEndGameAudio(false);
		finalTeleport = true;
		StartTeleportAbility();
		yield return new WaitForSeconds(3.5f);
		print("after post-teleport wait");
		StartFinalDrainAbility();
		print("called start final drain ability");
		yield return new WaitForSeconds(victoryAudio.length + 2f); // Victory audio because variables are flipped

		SpawnDeathObjects(false);
		print("should have spawned the death objects");
		yield return new WaitForSeconds(5f);
		EnableScoreboard();
	}

	[Button]
	public void StartDeathAbility() {
		if (!isServer) {
			return;
		}

		StartCoroutine(PlayCaptainDeath());
	}

	IEnumerator PlayCaptainDeath() {
		string abName = "CaptainDeath";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents(TaskUtility.GetTypeWithinAssembly(abName));

		Ability ab = abilities[0] as Ability;

		GetComponent<ControllerHandler>().TryStartAbility(ab);
		yield return new WaitForSecondsRealtime(2.5f);
		GetComponent<ControllerHandler>().TryStopAbility(ab);		
	}

	public void SpawnDeathParticles() {
		if (!isServer || captainDeathParticlesSpawned) {
			return;
		}
		explosionPosition = transform;
		print("explosionPosition set to " + explosionPosition.position);
		captainDeathParticlesSpawned = true;
		print("spawn death particles called");
		defeatParticleInstance1 = Instantiate(defeatParticle1, transform.position, Quaternion.identity);
		NetworkServer.Spawn(defeatParticleInstance1);
		Invoke("DestroyDeathParticles", 3f);

		//Invoke("SpawnExplosionParticles", 2f);
	}

	private void SpawnExplosionParticles() {
		defeatParticleInstance2 = Instantiate(defeatParticle2, explosionPosition.position, Quaternion.identity);
		defeatParticleInstance2.transform.position = explosionPosition.position;
		print("spawned the defeatParticleInstance2 at " + defeatParticle2.transform.position + " with explosionPosition being " + explosionPosition.position);
		NetworkServer.Spawn(defeatParticleInstance2);
		Invoke("DestroyDeathParticles", 3f);
	}

	private void DestroyDeathParticles() {
		if (!isServer) {
			return;
		}

		NetworkServer.Destroy(defeatParticleInstance1);
		NetworkServer.Destroy(defeatParticleInstance2);
	}

	private void SpawnDeathObjects(bool playerVictory) {
		if (!isServer) {
			return;
		}

		if (playerVictory) {
			GameObject r = Instantiate(captainRagdoll, transform.position, transform.rotation);
			NetworkServer.Spawn(r);

		} else {

		}
	}

	public void StartFinalDrainAbility() {
		if (!isServer) {
			return;
		}
		lastTimeDrain = true;
		print("last time drain set to " + lastTimeDrain);
		StartCoroutine("FinalDrainEnergy");
	}

	private void SpawnFinalDrainObjects() {
		FSVRPlayer[] players = FindObjectsOfType<FSVRPlayer>();
		for(int i=0; i<NumberOfPlayerHolder.instance.numberOfPlayers; i++) {
			switch (i) {
				case 0:
					target1 = players[i].GetComponentInChildren<EnemyTargetInit>().gameObject;
					trail1 = Instantiate(energyTrail, transform.position, Quaternion.identity);

					foreach (var v in trail1.GetComponentsInChildren<LineRenderer>()) {
						v.SetPosition(0, target1.transform.position);
						v.SetPosition(1, skullTransform.position);
					}
					break;
				case 1:
					target2 = players[i].GetComponentInChildren<EnemyTargetInit>().gameObject;
					trail2 = Instantiate(energyTrail, transform.position, Quaternion.identity);

					foreach (var v in trail2.GetComponentsInChildren<LineRenderer>()) {
						v.SetPosition(0, target2.transform.position);
						v.SetPosition(1, skullTransform.position);
					}
					break;
				case 2:
					target3 = players[i].GetComponentInChildren<EnemyTargetInit>().gameObject;
					trail3 = Instantiate(energyTrail, transform.position, Quaternion.identity);

					foreach (var v in trail3.GetComponentsInChildren<LineRenderer>()) {
						v.SetPosition(0, target3.transform.position);
						v.SetPosition(1, skullTransform.position);
					}
					break;
				case 3: // unused til 6p test
					break;
				case 4: // unused til 6p test
					break;
				case 5: // unused til 6p test
					break;
			}
		}
	}

	IEnumerator FinalDrainEnergy() {
		print("final drain called");
		string abName = "DrainEnergy";

		RigidbodyCharacterController controller = GetComponent<RigidbodyCharacterController>();
		var abilities = controller.GetComponents(TaskUtility.GetTypeWithinAssembly(abName));

		Ability ab = abilities[0] as Ability;
		print("drain energy ability tried start");
		GetComponent<ControllerHandler>().TryStartAbility(ab);
		print("after try start drain ability");
		yield return new WaitForSecondsRealtime(15f);
		GetComponent<ControllerHandler>().TryStopAbility(ab);
		if (trail1) {
			// disable player 1 particles here
			RpcDrainCleanup(0);
			Destroy(trail1);
		}
		if (trail2) {
			// disable player 2 particles here
			RpcDrainCleanup(1);
			Destroy(trail2);
		}
		if (trail3) {
			// disable player 3 particles here
			RpcDrainCleanup(2);
			Destroy(trail3);
		}

		foreach(var v in FindObjectsOfType<Player>()) {
			v.TurnOffAllParticles();
		}

		GameObject tpCurPos = Instantiate(captainCurrentPositionTeleportParticles, transform.position, Quaternion.identity);
		tpCurPos.transform.position = new Vector3(tpCurPos.transform.position.x, tpCurPos.transform.position.y + 1.5f, tpCurPos.transform.position.z);
		NetworkServer.Spawn(tpCurPos);
		transform.position = defeatPosition.transform.position;

		foreach (var v in FindObjectsOfType<EnemyDragonkin>()) {
			v.TeleportToDeath();
		}
	}

	[ClientRpc]
	private void RpcDrainCleanup(int num) {
		if (isServer) {
			return;
		}

		switch (num) {
			case 0:
				// disable player 1 particles here
				Destroy(trail1);
				break;
			case 1:
				// disable player 2 particles here
				Destroy(trail2);
				break;
			case 2:
				// disable player 3 particles here
				Destroy(trail3);
				break;
		}
	}

	private void EnableScoreboard() {
		highScoreTable.SetActive(true);
		RpcEnableScoreboard();
	}

	[ClientRpc]
	private void RpcEnableScoreboard() {
		if (isServer) {
			return;
		}

		highScoreTable.SetActive(true);
	}

	private void PlayEndGameAudio(bool playerVictory) {
		RpcPlayEndGameAudio(playerVictory);
		ambientSoundRef.PlayAmbientSound();
		print("should be playing end game audio, playerVictory? " + playerVictory);
		source.clip = playerVictory ? defeatAudio : victoryAudio; // backwards because variables are flipped
		source.Play();
	}

	[ClientRpc]
	private void RpcPlayEndGameAudio(bool playerVictory) {
		if (isServer) {
			return;
		}

		ambientSoundRef.PlayAmbientSound();
		source.clip = playerVictory ? defeatAudio : victoryAudio; // backwards because variables are flipped
		source.Play();
	}

	#endregion

}
