using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;

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

    public float timeBetweenReminders = 30f;
    public float timeBetweenPriorityLines = 3f;

    private int numLeftCannonsDamaged;
    private int numRightCannonsDamaged;
    private int numRatmenDead;
    private float timeElapsed = 0f;

    private bool audioTriggered = false;

    public AudioSource mySource;
    private List<AudioClip> audioQueue;

    /*
     * TODO: finish audio queue integration
     *       double check all checks
     */

    #region Tutorial shit

    //public bool continueTutorial = true;
    // ^ test against this before any tutorial speach incase of early completion
    //need to turn on mast after last part of tutorial
    public static Captain instance;
    public static Dictionary<DamagedObject, bool> damagedObjectsRepaired = new Dictionary<DamagedObject, bool>();
    public static Dictionary<Ratman, bool> ratmenRespawned = new Dictionary<Ratman, bool>();
    public static Dictionary<CannonInteraction, bool> playersFiredCannons = new Dictionary<CannonInteraction, bool>();
    public bool mastHasBeenPulled = false;
    public Collider[] mastRopes;
    public AudioClip[] tutorialSounds;
	bool damagedComplete, ratmenComplete, cannonsComplete;


    public void CheckDamagedObjects() {
        foreach (var obj in damagedObjectsRepaired) {
            print(obj.Key.name + " has a value of " + obj.Value);
        }
        if (!damagedObjectsRepaired.ContainsValue(false) && !damagedComplete) {
			damagedComplete = true;
            RpcPlaySoundClip("PrepTut_Rat");
        }
    }

    public void CheckRatmenRespawns() {
        foreach (var obj in ratmenRespawned) {
            print(obj.Key.name + " has a value of " + obj.Value);
        }
        if (!ratmenRespawned.ContainsValue(false) && !ratmenComplete) {
			ratmenComplete = true;
            RpcPlaySoundClip("PrepTut_Shoot");
            print("Ratmen have been replenished");
        }
    }

    public void CheckPlayersCannonFiring() {
        foreach (var obj in playersFiredCannons) {
            print(obj.Key.name + " has a value of " + obj.Value);
        }
        if (!playersFiredCannons.ContainsValue(false) && !cannonsComplete) {
			cannonsComplete = true;
            RpcPlaySoundClip("PrepTut_Mast");
            print("Players have fired cannons");
            RpcEnableRopes();
            foreach (var g in mastRopes) {
                g.enabled = true;
            }
        }
    }

    [ClientRpc]
    void RpcEnableRopes() {
        if (isServer) {
            return;
        }

        foreach (var g in mastRopes) {
            g.enabled = true;
        }
    }


    public void StartTutorial() {
        StartCoroutine("TutorialIntro");
    }

    IEnumerator TutorialIntro() {
        RpcPlaySoundClip("PrepTut_Intro");
        yield return new WaitForSecondsRealtime(GetClipLength("PrepTut_Intro") + 2f);
        RpcPlaySoundClip("PrepTut_Repair");
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

    public void MastHasBeenPulled() {
        if (!mastHasBeenPulled) {
            //talkie talkie
        }
        mastHasBeenPulled = true;

        RpcPlaySoundClip("MastPulled");
        print("mast has been pulled");
    }

    [ClientRpc]
    public void RpcPlaySoundClip(string clip) {
        if (isServer)
            return;

        for (int i = 0; i < tutorialSounds.Length; i++) {
            if (tutorialSounds[i].name == clip) {
                mySource.PlayOneShot(tutorialSounds[i]);
                break;
            }
        }
    }

    #endregion

    private void Start() {
        if (isServer) {
            if (instance == null) {
                instance = this;   

            } else {
                Destroy(gameObject);
            }
        }

        foreach (var g in mastRopes) {
            g.enabled = false;
        }
    }

    private void Update() {
        if (audioTriggered) {
            timeElapsed += Time.deltaTime; // increment the time
            if (timeElapsed >= timeBetweenReminders) // if time between reminders time has passed
            {
                if (audioQueue.Count == 0)
                    PlayGenericVoice();
            }
        }
    }

    private void PlayGenericVoice() {
        AudioClip clip = DetermineLine();
        mySource.clip = clip;
        mySource.Play();
    }

    private AudioClip DetermineLine() {
        AudioClip retClip;

        if (numLeftCannonsDamaged > 0) {
            if (numRightCannonsDamaged > 0) {
                if (numRatmenDead > 0) {
                    retClip = leftAndRightCannonsAndRatmen;
                } else {
                    retClip = leftAndRightCannons;
                }
            } else if (numRatmenDead > 0) {
                retClip = leftCannonsAndRatmen;
            } else {
                retClip = leftCannonsDown;
            }
        } else if (numRightCannonsDamaged > 0) {
            if (numRatmenDead > 0) {
                retClip = rightCannonsAndRatmen;
            } else {
                retClip = rightCannonsDown;
            }
        }

          // by this point all damaged cannons should be addressed, only ratmen are left

          else if (numRatmenDead > 0) // ratmen only
          {
            retClip = ratmenOnly;
        } else {
            // SHOULD NEVER GET HERE, insert some random clip for captain to say in case it does, maybe something like everything is good well done.
            retClip = shouldNeverGetHere;
        }

        return retClip;
    }

    private AudioClip DetermineEndOfEncounterLine() {
        AudioClip retClip;

        if (numLeftCannonsDamaged > 0 || numRightCannonsDamaged > 0) // if any cannons destroyed
        {
            if (numRatmenDead > 0) //cannons + ratmen 
            {
                retClip = endOfEncounterBoth; // both need work
            } else // cannons
              {
                retClip = endOfEncounterCannons;
            }
        } else if (numRatmenDead > 0) // ratmen
          {
            retClip = endOfEncounterRatmen;
        } else // nothing needs work
          {
            retClip = endOfEncounterAllIsWell;
        }

        return retClip;
    }

    public void EndOfEncounter() {
        audioQueue.Clear(); // Get rid of all priority lines
        AudioClip clip = DetermineEndOfEncounterLine();
        mySource.clip = clip;
        mySource.Play();
    }

    public void CannonDamaged(Side side) {
        audioTriggered = true;

        if (side == Side.left) {
            numLeftCannonsDamaged++;
        } else {
            numRightCannonsDamaged++;
        }
    }

    public void CannonRepaired(Side side) {
        if (side == Side.left)
            numLeftCannonsDamaged--;
        else
            numRightCannonsDamaged--;

        CheckIfNeedReminder();

    }

    public void RatmenKilled() {
        numRatmenDead++;
    }

    public void RatmenReplenished() {
        numRatmenDead--;

        CheckIfNeedReminder();
    }

    private void CheckIfNeedReminder() {
        if (numRatmenDead == 0 && numLeftCannonsDamaged == 0 && numRightCannonsDamaged == 0) // if all non priority variables are zero, don't need reminder line
        {
            audioTriggered = false;
            timeElapsed = 0f;
        }
    }

    public void EnemyIncoming(Side side) {
        if (side == Side.left) {
            if (!mySource.isPlaying && audioQueue.Count == 0) {
                mySource.clip = enemyIncomingLeft;
                mySource.Play();
            } else {
                audioQueue.Add(enemyIncomingLeft);
            }
        } else {
            if (!mySource.isPlaying && audioQueue.Count == 0) {
                mySource.clip = enemyIncomingRight;
                mySource.Play();
            } else {
                audioQueue.Add(enemyIncomingRight);
            }
        }
    }

    public void EnemyBoarding(Side side) {
        if (side == Side.left) {
            if (!mySource.isPlaying && audioQueue.Count == 0) {
                mySource.clip = enemyBoardingLeft;
                mySource.Play();
            } else {
                audioQueue.Add(enemyBoardingLeft);
            }
        } else {
            if (!mySource.isPlaying && audioQueue.Count == 0) {
                mySource.clip = enemyBoardingRight;
                mySource.Play();
            } else {
                audioQueue.Add(enemyBoardingRight);
            }
        }
    }

    public void CannonDestroyed(Side side) {
        if (side == Side.left) {
            if (mySource.isPlaying) {
                audioQueue.Add(cannonDestroyedLeftSide);
            } else {
                mySource.clip = cannonDestroyedLeftSide;
                mySource.Play();
            }
        } else {
            if (mySource.isPlaying) {
                audioQueue.Add(cannonDestroyedRightSide);
            } else {
                mySource.clip = cannonDestroyedRightSide;
                mySource.Play();
            }
        }
    }
}
