using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using System.Reflection;

public enum Side {
    left,
    right
};

public class Captain : SerializedNetworkBehaviour {
    #region Sounds

    [ToggleGroup("FirstToggle", order: -1, groupTitle: "Captain Speech")]
    public bool FirstToggle;

    // Low priority audio clips (reminders everything 30s if needed)
    [ToggleGroup("FirstToggle")]
    public AudioClip leftCannonsDown;
    [ToggleGroup("FirstToggle")]
    public AudioClip leftCannonsAndRatmen;
    [ToggleGroup("FirstToggle")]
    public AudioClip rightCannonsDown;
    [ToggleGroup("FirstToggle")]
    public AudioClip rightCannonsAndRatmen;
    [ToggleGroup("FirstToggle")]
    public AudioClip leftAndRightCannons;
    [ToggleGroup("FirstToggle")]
    public AudioClip leftAndRightCannonsAndRatmen;
    [ToggleGroup("FirstToggle")]
    public AudioClip ratmenOnly;
    [ToggleGroup("FirstToggle")]
    public AudioClip shouldNeverGetHere;

    // High priority audio clips
    [ToggleGroup("FirstToggle")]
    public AudioClip enemiesAtMast;
    [ToggleGroup("FirstToggle")]
    public AudioClip cannonDestroyedLeftSide;
    [ToggleGroup("FirstToggle")]
    public AudioClip cannonDestroyedRightSide;
    [ToggleGroup("FirstToggle")]
    public AudioClip cannonDestroyedBothSides;
    [ToggleGroup("FirstToggle")]
    public AudioClip enemyIncomingLeft;
    [ToggleGroup("FirstToggle")]
    public AudioClip enemyIncomingRight;
    [ToggleGroup("FirstToggle")]
    public AudioClip enemyIncomingBoth;
    [ToggleGroup("FirstToggle")]
    public AudioClip enemyBoardingLeft;
    [ToggleGroup("FirstToggle")]
    public AudioClip enemyBoardingRight;
    [ToggleGroup("FirstToggle")]
    public AudioClip enemyBoardingBoth;

    // End of encounter audio clips
    [ToggleGroup("FirstToggle")]
    public AudioClip endOfEncounterRatmen;
    [ToggleGroup("FirstToggle")]
    public AudioClip endOfEncounterCannons;
    [ToggleGroup("FirstToggle")]
    public AudioClip endOfEncounterBoth;
    [ToggleGroup("FirstToggle")]
    public AudioClip endOfEncounterAllIsWell;

    #endregion

    //public float timeBetweenReminders = 30f;
    //public float timeBetweenPriorityLines = 3f;

    private int numLeftCannonsDamaged;
    private int numRightCannonsDamaged;
    private int numRatmenDead;
    private float timeElapsed = 0f;

    private bool audioTriggered = false;

    public AudioSource mySource, ambientSource;
    //private List<AudioClip> audioQueue;

    /*
	 * TODO: finish audio queue integration
	 *       double check all checks
	 */

    void Start() {
        if (isServer) {
            if (instance == null) {
                ////print("is server, setting as instance");
                instance = this;
            } else {
                ////print("is server with instance, destroying");

                Destroy(gameObject);
            }
        } else {
            ////print("not server");
        }

        foreach (var g in mastRopes) {
            g.enabled = false;
        }

        //PlayDialogue(tutorialSounds[0]);
    }

    bool hasInitialized = false;
    public void Init() {
        hasInitialized = true;
        AssignClipsToDictionary();

        priorityAudioQueue = new Queue<AudioClip>();
        reminderQueue = new Queue<AudioClip>();


        if (!isServer) {
            return;
        }

        DisableCannons();
        DisableFirePrompt();
        DisableRatHatches();
        DisableRopes();
        //DisableGuards();

        eventTimes = new Dictionary<AudioEventType, float>();
        eventTimes.Add(AudioEventType.Cannon, Time.timeSinceLevelLoad);
        eventTimes.Add(AudioEventType.Ratmen, Time.timeSinceLevelLoad);
        eventTimes.Add(AudioEventType.RepairDeck, Time.timeSinceLevelLoad);
        eventTimes.Add(AudioEventType.Respawn, Time.timeSinceLevelLoad);

    }

    public void PlayDialogue(string clip) {
        for (int i = 0; i < tutorialSounds.Length; i++) {
            if (tutorialSounds[i].name == clip) {
                mySource.PlayOneShot(tutorialSounds[i]);
                RpcPlaySoundClip(clip);
                return;
            }
        }

        Debug.Log(clip + " does not exist in tutorial dialogue collection");
    }

    #region Tutorial shit

    //public bool continueTutorial = true;
    // ^ test against this before any tutorial speach incase of early completion
    //need to turn on mast after last part of tutorial
    public static Captain instance;
    public static Dictionary<DamagedObject, bool> damagedObjectsRepaired = new Dictionary<DamagedObject, bool>();
    public static Dictionary<Ratman, bool> ratmenRespawned = new Dictionary<Ratman, bool>();
    public static Dictionary<CannonInteraction, bool> playersFiredCannons = new Dictionary<CannonInteraction, bool>();
    public static Dictionary<Enemy, bool> enemiesKilled = new Dictionary<Enemy, bool>();

    public bool mastHasBeenPulled = false;
    public Collider[] mastRopes;
    public AudioClip[] tutorialSounds;
    bool guardsComplete, damagedComplete, ratmenComplete, cannonsComplete;

    public List<GameObject> tutorialCannons, actualCannons, cannonFirePrompts, tutorialRatHatch, ratHatch, tutorialGuards, mastPrompts; //todo disable mast prompt etc

    public Transform[] guardPositions;
    public GameObject guardPrefab;

    public void StartTutorial() {
        if (!isServer) {
            return;
        }
        ////print("start tutorial");

        for (int i = 0; i < FindObjectOfType<NumberOfPlayerHolder>().numberOfPlayers; i++) {
            GameObject g = Instantiate(guardPrefab, guardPositions[i].position, guardPositions[i].rotation);
            g.GetComponent<BehaviorDesigner.Runtime.BehaviorTree>().SetVariableValue("target", VariableHolder.instance.players[i]);
            enemiesKilled.Add(g.GetComponent<Enemy>(), false);
            NetworkServer.Spawn(g);
        }

        ambientSource.enabled = true;
        RpcEnableAmbient();

        TutorialIntro();
        //EnableGuardsByPlayerCount();
    }

    [ClientRpc]
    void RpcEnableAmbient() {
        if (isServer) {
            return;
        }

        ambientSource.enabled = true;

    }

    void TutorialIntro() {
        RpcPlaySoundClip("PrepTut_Intro");
        Invoke("TutorialIntro", 35f);
    }

    //#region guards 

    //void EnableGuardsByPlayerCount() {
    //	for (int i = 0; i < tutorialGuards.Count && i < VariableHolder.instance.numPlayers; i++) {
    //		tutorialGuards[i].SetActive(true);
    //		enemiesKilled.Add(tutorialGuards[i].GetComponent<Enemy>(), false);
    //	}

    //	RpcEnableGuards(VariableHolder.instance.numPlayers);
    //}

    //[ClientRpc]
    //void RpcEnableGuards(int count) {
    //	if (isServer) {
    //		return;
    //	}

    //	//print("count is " + count);
    //	for (int i = 0; i < tutorialGuards.Count && i < count; i++) {
    //		tutorialGuards[i].SetActive(true);
    //	}
    //}

    //void DisableGuards() {
    //	for (int i = 0; i < tutorialGuards.Count; i++) {
    //		tutorialGuards[i].SetActive(false);
    //	}

    //	RpcDisableGuards();
    //}

    //[ClientRpc]
    //void RpcDisableGuards() {
    //	if (isServer) {
    //		return;
    //	}
    //	for (int i = 0; i < tutorialGuards.Count; i++) {
    //		tutorialGuards[i].SetActive(false);
    //	}
    //}

    //#endregion

    public void CheckEnemiesKilled() {
        //foreach (var obj in enemiesKilled) {
        //	////print(obj.Key.name + " has a value of " + obj.Value);
        //}
        if (!enemiesKilled.ContainsValue(false) && !guardsComplete) {
            guardsComplete = true;
            CancelInvoke();
            RpcPlaySoundClip("PrepTut_Repair");
            EnableCannons();
        }
    }

    #region cannon swapping

    void EnableCannons() {
        for (int i = 0; i < tutorialCannons.Count; i++) {
            tutorialCannons[i].SetActive(false);
            actualCannons[i].SetActive(true);
        }

        RpcEnableCannons();
    }

    [ClientRpc]
    void RpcEnableCannons() {
        if (isServer) {
            return;
        }
        for (int i = 0; i < tutorialCannons.Count; i++) {
            tutorialCannons[i].SetActive(false);
            actualCannons[i].SetActive(true);
            actualCannons[i].GetComponent<DamagedObject>().ChangeHealth(10000000, true);
        }
    }

    void DisableCannons() {
        for (int i = 0; i < tutorialCannons.Count; i++) {
            tutorialCannons[i].SetActive(true);
            actualCannons[i].SetActive(false);
        }

        RpcDisableCannons();
    }

    [ClientRpc]
    void RpcDisableCannons() {
        if (isServer) {
            return;
        }
        for (int i = 0; i < tutorialCannons.Count; i++) {
            tutorialCannons[i].SetActive(true);
            actualCannons[i].SetActive(false);
        }
    }

    #endregion

    public void CheckDamagedObjects() {
        //foreach (var obj in damagedObjectsRepaired) {
        //	//print(obj.Key.name + " has a value of " + obj.Value);
        //}
        if (!damagedObjectsRepaired.ContainsValue(false) && !damagedComplete) {
            damagedComplete = true;
            RpcPlaySoundClip("PrepTut_Shoot");
            EnableFirePrompt();
        }
    }

    #region fire prompts 

    void EnableFirePrompt() {
        //print("enable fire prompts");
        for (int i = 0; i < cannonFirePrompts.Count; i++) {
            //print( cannonFirePrompts[i].name +  " being enabled" );

            cannonFirePrompts[i].SetActive(true);
        }

        RpcEnableFirePrompt();
    }

    [ClientRpc]
    void RpcEnableFirePrompt() {
        if (isServer) {
            return;
        }
        for (int i = 0; i < cannonFirePrompts.Count; i++) {
            cannonFirePrompts[i].SetActive(true);
        }
    }

    void DisableFirePrompt() {
        for (int i = 0; i < cannonFirePrompts.Count; i++) {
            cannonFirePrompts[i].SetActive(false);
        }

        RpcDisableFirePrompt();
    }

    [ClientRpc]
    void RpcDisableFirePrompt() {
        if (isServer) {
            return;
        }
        for (int i = 0; i < cannonFirePrompts.Count; i++) {
            cannonFirePrompts[i].SetActive(false);
        }
    }

    #endregion

    public void CheckPlayersCannonFiring() {
        //foreach (var obj in playersFiredCannons) {
        //	//print(obj.Key.name + " has a value of " + obj.Value);
        //}
        if (!playersFiredCannons.ContainsValue(false) && !cannonsComplete) {
            cannonsComplete = true;
            RpcPlaySoundClip("PrepTut_Rat");
            EnableRatHatches();
            DisableFirePrompt();
        }
    }

    #region rat hatch

    void EnableRatHatches() {
        for (int i = 0; i < ratHatch.Count; i++) {
            ratHatch[i].SetActive(true);
            tutorialRatHatch[i].SetActive(false);
        }

        HatchActivator.EnableHatch(true);
        HatchActivator.EnableHatch(false);


        RpcEnableRatHatches();
    }

    [ClientRpc]
    void RpcEnableRatHatches() {
        if (isServer) {
            return;
        }
        for (int i = 0; i < ratHatch.Count; i++) {
            ratHatch[i].SetActive(true);
            tutorialRatHatch[i].SetActive(false);
        }

        HatchActivator.EnableHatch(true);
        HatchActivator.EnableHatch(false);
    }

    void DisableRatHatches() {
        for (int i = 0; i < ratHatch.Count; i++) {
            ratHatch[i].SetActive(false);
            tutorialRatHatch[i].SetActive(true);
        }

        RpcDisableRatHatches();
    }

    [ClientRpc]
    void RpcDisableRatHatches() {
        if (isServer) {
            return;
        }
        for (int i = 0; i < tutorialCannons.Count; i++) {
            ratHatch[i].SetActive(false);
            tutorialRatHatch[i].SetActive(true);
        }
    }

    #endregion

    public void CheckRatmenRespawns() {
        //foreach (var obj in ratmenRespawned) {
        //	//print(obj.Key.name + " has a value of " + obj.Value);
        //}
        if (!ratmenRespawned.ContainsValue(false) && !ratmenComplete) {
            ratmenComplete = true;
            RpcPlaySoundClip("PrepTut_Mast");

            EnableRopes();
        }
    }

    void EnableRopes() {
        foreach (var g in mastRopes) {
            g.enabled = true;
        }

        foreach (GameObject go in mastPrompts) {
            go.SetActive(true);
        }

        RpcEnableRopes();
    }

    [ClientRpc]
    void RpcEnableRopes() {
        if (isServer) {
            return;
        }

        foreach (GameObject go in mastPrompts) {
            go.SetActive(true);
        }

        foreach (var g in mastRopes) {
            g.enabled = true;
        }
    }

    void DisableRopes() {
        foreach (var g in mastRopes) {
            g.enabled = false;
        }

        foreach (GameObject go in mastPrompts) {
            go.SetActive(false);
        }

        RpcDisableRopes();
    }

    [ClientRpc]
    void RpcDisableRopes() {
        if (isServer) {
            return;
        }

        foreach (var g in mastRopes) {
            g.enabled = true;
        }

        foreach (GameObject go in mastPrompts) {
            go.SetActive(false);
        }
    }

    public void MastHasBeenPulled() {
        if (!mastHasBeenPulled) {
            RpcPlaySoundClip("MastPulled");
        }
        mastHasBeenPulled = true;
    }

    [ClientRpc]
    public void RpcPlaySoundClip(string clip) {
        if (isServer)
            return;

        ////print("playing sound clip " + clip);

        for (int i = 0; i < tutorialSounds.Length; i++) {
            if (tutorialSounds[i].name == clip) {
                mySource.PlayOneShot(tutorialSounds[i]);
                break;
            }
        }
    }

    float GetClipLength(string clip) {
        for (int i = 0; i < tutorialSounds.Length; i++) {
            if (tutorialSounds[i].name == clip) {
                return tutorialSounds[i].length;
            }
        }

        Debug.LogWarning(clip + " does not match any tutorial clip names for " + name);
        return 0;
    }

    #endregion

    #region Captain audio

    public enum AudioEventType {
        Cannon, Ratmen, Respawn, RepairDeck, OneShot
    }

    Dictionary<AudioEventType, float> eventTimes;
    Dictionary<string, AudioClip> clipNames;
    //Dictionary<AudioClip, string> clipNamesLookup;

    Queue<AudioClip> priorityAudioQueue, reminderQueue;
    public float timeBetweenReminders = 10, lastPlayedTime;

    public AudioClip repairCannonClip, ratmenDeadClip, playerRespawnClip, repairDeckClip;

    private void Update() {
        if (!isServer || !hasInitialized) {
            //print("returning");
            return;
        }

        if (mySource.isPlaying) {
            print("source is playing");

            return;
        }

        if (priorityAudioQueue.Count > 0) {
            print("priority needs to play");

            mySource.PlayOneShot(priorityAudioQueue.First());
            RpcPlayDialogue(priorityAudioQueue.Dequeue().name);
            lastPlayedTime = Time.timeSinceLevelLoad;
        }

        if (lastPlayedTime + timeBetweenReminders <= Time.timeSinceLevelLoad) { //its been atleast aslong as the remindertimer
            print("time for reminder");

            if (reminderQueue.Count > 0) {
                print("reminder needs to play");

                mySource.PlayOneShot(reminderQueue.First());
                RpcPlayDialogue(reminderQueue.Dequeue().name);

                lastPlayedTime = Time.timeSinceLevelLoad;
            }
        }
    }

    [ClientRpc]
    private void RpcPlayDialogue(string clipName) {
        if (isServer) {
            return;
        }

        print("rpc called with " + clipName);

        mySource.PlayOneShot(clipNames[clipName]);
    }

    void AssignClipsToDictionary() {
        clipNames = new Dictionary<string, AudioClip>();
        clipNames.Add(repairCannonClip.name, repairCannonClip);
        clipNames.Add(ratmenDeadClip.name, ratmenDeadClip);
        clipNames.Add(repairDeckClip.name, repairDeckClip);
        clipNames.Add(playerRespawnClip.name, playerRespawnClip);

        //clipNamesLookup = new Dictionary<AudioClip, string>();
        //clipNamesLookup.Add( repairCannonClip, "Cannon");
        //clipNamesLookup.Add( ratmenDeadClip,"Ratmen");
        //clipNamesLookup.Add(repairDeckClip,"Deck" );
        //clipNamesLookup.Add(playerRespawnClip,"Respawn" );
    }

    bool CheckAndUpdateEventTimePriority(AudioEventType eventType) {
        bool toReturn = false;

        switch (eventType) {
            case AudioEventType.Cannon:

                eventTimes[AudioEventType.Cannon] = Time.timeSinceLevelLoad;
                priorityAudioQueue.Enqueue(repairCannonClip);

                break;
            case AudioEventType.Ratmen:

                eventTimes[AudioEventType.Ratmen] = Time.timeSinceLevelLoad;
                priorityAudioQueue.Enqueue(ratmenDeadClip);

                break;
            case AudioEventType.Respawn:

                eventTimes[AudioEventType.Respawn] = Time.timeSinceLevelLoad;
                priorityAudioQueue.Enqueue(playerRespawnClip);

                break;
            case AudioEventType.RepairDeck:

                eventTimes[AudioEventType.RepairDeck] = Time.timeSinceLevelLoad;
                priorityAudioQueue.Enqueue(repairDeckClip);

                break;
            case AudioEventType.OneShot:
                if (priorityAudioQueue.Count >= 0) {
                    toReturn = false;
                } else {
                    toReturn = true;
                }
                break;
            default:
                Debug.LogWarning("Captain is checking a nonexistent audio event type");
                break;
        }

        return toReturn;
    }

    public AudioEventType typeToQueue;
    [Button]
    public void Tester() {
        CheckAndUpdateEventTimePriority(typeToQueue);
    }

    void UpdateReminderQueue(AudioEventType type) {
        //checkl queue, if event type exist then move it up in queue?
    }

    public void AddEventToQueue(AudioEventType type) {
        CheckAndUpdateEventTimePriority(type);
    }

    bool CheckAndUpdateEventTimeReminder(AudioEventType eventType) {
        bool toReturn = false;

        switch (eventType) {
            case AudioEventType.Cannon:
                if ((Time.time - eventTimes.TryGetValue(AudioEventType.Cannon)) > timeBetweenReminders) {
                    //its been long enough
                    eventTimes[AudioEventType.Cannon] = Time.timeSinceLevelLoad;
                    reminderQueue.Enqueue(repairCannonClip);
                }
                break;
            case AudioEventType.Ratmen:
                if ((Time.time - eventTimes.TryGetValue(AudioEventType.Ratmen)) > timeBetweenReminders) {
                    //its been long enough
                    eventTimes[AudioEventType.Ratmen] = Time.timeSinceLevelLoad;
                    reminderQueue.Enqueue(ratmenDeadClip);
                }
                break;
            case AudioEventType.Respawn:

                if ((Time.time - eventTimes.TryGetValue(AudioEventType.Respawn)) > timeBetweenReminders) {
                    //its been long enough
                    eventTimes[AudioEventType.Respawn] = Time.timeSinceLevelLoad;
                    reminderQueue.Enqueue(playerRespawnClip);
                }
                break;
            case AudioEventType.RepairDeck:

                if ((Time.time - eventTimes.TryGetValue(AudioEventType.RepairDeck)) > timeBetweenReminders) {
                    //its been long enough
                    eventTimes[AudioEventType.RepairDeck] = Time.timeSinceLevelLoad;
                    reminderQueue.Enqueue(repairDeckClip);
                }
                break;
            case AudioEventType.OneShot:
                if (priorityAudioQueue.Count >= 0) {
                    toReturn = false;
                } else {
                    toReturn = true;
                }
                break;
            default:
                Debug.LogWarning("Captain is checking a nonexistent audio event type");
                break;
        }

        return toReturn;
    }
    #endregion

}