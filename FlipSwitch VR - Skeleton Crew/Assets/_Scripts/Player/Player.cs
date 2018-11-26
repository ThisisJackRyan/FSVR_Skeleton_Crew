using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player : NetworkBehaviour {

	[SyncVar(hook = "OnHealthChange")] public int health = 100;
	int maxHealth = 100;

	[Tooltip("The particles within the bones (flames)")] public GameObject[] internalParticles;
	[Tooltip("The particles coming out of the bones (dust)")] public GameObject[] externalParticles; // external is dust
	[Tooltip("The death particles (old player particles)")] public GameObject[] deathParticles;
	[Tooltip("The hit particles to play when hit")] public GameObject[] hitParticles;

	public GameObject[] playerBody;
	public Collider[] playerColliders;
	public GameObject[] deathExplosion;
	public Transform explosionPosition;

	public GameObject activeTrailHand;

	[SyncVar]
	bool isDead = false;

	public bool IsDead {
		get {
			return isDead;
		}
	}

	public AudioClip deathSound;
	[ClientRpc]
	private void RpcPlayDeathSound() {
		if (isServer) {
			return;
		}

		GetComponent<AudioSource>().PlayOneShot(deathSound);
	}

	void OnHealthChange(int n) {
		////print("player health change");
		if (n < health) {
			for (int i = 0; i < hitParticles.Length; i++) {
				hitParticles[i].SetActive(true);
				var particles = hitParticles[i].GetComponent<ParticleSystem>();
				particles.Simulate(0, true, true);
				particles.Play();
			}
			Invoke("TurnOffHit", 1.0f);
		}

		health = n;

		if (health <= 0) { // enabled: death ; disabled: internal & external
			DisableBody();
			UpdateParticles(false, false, true);
		} else if (health <= 25) { // enabled: internal ; disabled: external & death
			UpdateParticles(true, false, false);
		} else if (health <= maxHealth) { // enabled: internal & external ; disabled: death
			UpdateParticles(true, true, false);
		}
	}

	internal void DisableTrailRenderer() {
		if (!isServer) {
			return;
		}
		activeTrailHand = null;
		foreach (var v in GetComponentsInChildren<TrailRenderer>()) {
			v.enabled = false;
		}

		RpcDisableTrailRenderer();
	}

	[ClientRpc]
	private void RpcDisableTrailRenderer() {
		if (isServer) {
			return;
		}

		activeTrailHand = null;
		foreach (var v in GetComponentsInChildren<TrailRenderer>()) {
			v.enabled = false;
		}
	}

	void TurnOffHit() {
		for (int i = 0; i < hitParticles.Length; i++) {
			hitParticles[i].SetActive(false);
		}
	}

	private void UpdateParticles(bool internalActive, bool externalActive, bool deathActive) {
		for (int i = 0; i < internalParticles.Length; i++) {
			internalParticles[i].SetActive(internalActive);
			//externalParticles[i].SetActive( externalActive );
			deathParticles[i].SetActive(deathActive);
		}
	}

	[Button]
	public void KillMe() {
		if (isServer)
			ChangeHealth(maxHealth);
	}

	[Button]
	public void ReviveMe() {
		if (isServer) {
			RevivePlayer();
		}
	}

	private void Start() {
		OnHealthChange(health);

		respawnText.gameObject.SetActive(false);
		countdownText.gameObject.SetActive(false);
		hudOverlay.color = new Color(hudOverlay.color.r, hudOverlay.color.g, hudOverlay.color.b, 0);
	}


	bool hasStartedTutorial = false;

	public void RevivePlayer() {
		////print("revive called with isDead: " + isDead);

		if (isDead) {
			ChangeHealth(maxHealth, false);
			VariableHolder.instance.players.Add(GetComponentInChildren<EnemyTargetInit>().gameObject);
			EnableBody();
			RpcEnableBody();
			isDead = false;
		}
	}

	void DisableBody() {
		////print("disable body called with isDead: " + isDead);
		if (!isDead) {
			if (isServer) {
				//if (Captain.instance) {
				//	Captain.instance.AddEventToQueue(Captain.AudioEventType.Respawn);
				//}

				VariableHolder.instance.IncreasePlayerScore(gameObject.transform.root.gameObject, VariableHolder.PlayerScore.ScoreType.Deaths, transform.position);
			}

			foreach (GameObject g in playerBody) {
				g.SetActive(false);
			}
			foreach (Collider c in playerColliders) {
				c.enabled = false;
			}

			GetComponent<ChangeAvatar>().DisableArmor();
			GetComponent<GrabWeapon>().Death();

			if (isServer) {
				var g = Instantiate(deathExplosion[GetComponent<ChangeAvatar>().GetColor()], explosionPosition.position, Quaternion.identity);//todo add server spawning
				NetworkServer.Spawn(g);
			}

			isDead = true;

			StartCoroutine("StartRespawnCounter");
		}
	}


	public Text countdownText, respawnText;
	public int respawnCounter = 10;
	public int hudTransparency = 175;
	public GameObject animObject;
	private GameObject animInstance;
	public Image hudOverlay;
	IEnumerator StartRespawnCounter() {
		//fade in hud
		for (int i = 0; i < hudTransparency; i += 3) {
			//print(i / 255+ " is i in fade in");

			hudOverlay.color = new Color(hudOverlay.color.r, hudOverlay.color.g, hudOverlay.color.b, (float)i / 255);
			yield return new WaitForEndOfFrame();
		}

		//print("show text");

		//show text
		respawnText.gameObject.SetActive(true);
		countdownText.gameObject.SetActive(true);

		//start countdown
		for (int i = respawnCounter; i >= 0; i--) {
			countdownText.text = i.ToString();
			yield return new WaitForSeconds(1);
		}

		//print("hide text");
		animInstance = Instantiate(animObject, GetComponentInChildren<HipMarker>().gameObject.transform.position, Quaternion.identity);
		float timeOfSpawn = Time.time;

		//hide text
		respawnText.gameObject.SetActive(false);
		countdownText.gameObject.SetActive(false);

		//fade out hud
		for (int i = hudTransparency; i > 0; i -= 3) {
			//print(i / 255 + " is i in fade out");
			hudOverlay.color = new Color(hudOverlay.color.r, hudOverlay.color.g, hudOverlay.color.b, (float)i/255);
			yield return new WaitForEndOfFrame();
		}

		//spawn anim
		yield return new WaitForSeconds(animObject.GetComponentInChildren<Animation>().clip.length - (Time.time - timeOfSpawn));

		//actual revive
		if (isServer) {
			RevivePlayer();
		}

	}

	public void TurnOffAllParticles() {
		if (!isServer) {
			return;
		}

		UpdateParticles(false, false, false);
		RpcTurnOffAllParticles();
	}

	[ClientRpc]
	private void RpcTurnOffAllParticles() {
		if (isServer) {
			return;
		}

		UpdateParticles(false, false, false);
	}

	public void TurnOffColliders() { //todo rename for nathans sake
		foreach (Collider c in playerColliders) {
			c.enabled = false;
		}
	}

	[ClientRpc]
	void RpcEnableBody() {
		if (isServer)
			return;

		foreach (GameObject g in playerBody) {
			g.SetActive(true);
		}

		GetComponent<ChangeAvatar>().EnableArmor();
		GetComponent<GrabWeapon>().Revive();
	}

	void EnableBody() {
		foreach (GameObject g in playerBody) {
			g.SetActive(true);
		}

		foreach (Collider c in playerColliders) {
			c.enabled = true;
		}

		GetComponent<ChangeAvatar>().EnableArmor();
	}

	public void TellCaptainToStartTutorial() {
		////print( "tell captain to start tut called" );

		if (isServer && !hasStartedTutorial) {
			hasStartedTutorial = true;
			StartCoroutine("WaitToTellCaptain");
		}
	}

	IEnumerator WaitToTellCaptain() {
		yield return new WaitForSecondsRealtime(3);
		FindObjectOfType<Captain>().StartTutorial();
	}

	public int ChangeHealth(int amount, bool damage = true) {
		if (!isServer)
			return health;

		if (damage) {
			health -= Mathf.Abs(amount);
			health = (health < 0) ? 0 : health;
		} else {
			health += Mathf.Abs(amount);
			health = (health > maxHealth) ? maxHealth : health;
		}


		if (health == 0) {
			if (VariableHolder.instance.players.Contains(GetComponentInChildren<EnemyTargetInit>().gameObject)) {
				VariableHolder.instance.players.Remove(GetComponentInChildren<EnemyTargetInit>().gameObject);
			}
		}


		return health;
	}

	public int GetHealth() {
		return health;
	}

	public void StartTrail(bool? isLeftHand) {
		if (!isServer) {
			return;
		}

		////print("start trail called on server. should be rpcing out now");

		bool test = (bool)(isLeftHand);

		RpcStartTrail(test);

		if (isLeftHand == true) {
			foreach (var v in GetComponentsInChildren<GrabWeaponHand>()) {
				if (v.isLeftHand) {
					activeTrailHand = v.gameObject;
					activeTrailHand.GetComponent<TrailRenderer>().enabled = true;
					break;
				}
			}
		} else if (isLeftHand == false) {
			foreach (var v in GetComponentsInChildren<GrabWeaponHand>()) {
				if (!v.isLeftHand) {
					activeTrailHand = v.gameObject;
					activeTrailHand.GetComponent<TrailRenderer>().enabled = true;
					break;
				}
			}
		}

	}

	[ClientRpc]
	internal void RpcStartTrail(bool isLeftHand) {
		if (isServer) {
			return;
		}
		////print("rpc start trail has been called not on server");

		if (isLeftHand == true) {
			foreach (var v in GetComponentsInChildren<GrabWeaponHand>()) {
				////print(v.name + " was found in loop");
				if (v.isLeftHand) {
					////print("left trail renderer is being stored as active, pre enable");

					activeTrailHand = v.gameObject;
					activeTrailHand.GetComponent<TrailRenderer>().enabled = true;
					////print("left trail renderer should be started");
					break;
				}
			}
		} else if (isLeftHand == false) {
			foreach (var v in GetComponentsInChildren<GrabWeaponHand>()) {
				////print(v.name + " was found in loop");

				if (!v.isLeftHand) {
					////print("right trail renderer is being stored as active, pre enable");

					activeTrailHand = v.gameObject;
					activeTrailHand.GetComponent<TrailRenderer>().enabled = true;
					////print("right trail renderer should be started");
					break;
				}
			}
		}
	}
}
