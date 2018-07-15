using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;

public enum Side {
    left,
    right
};

public class Captain : NetworkBehaviour {

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

    private AudioSource mySource;
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
    public Dictionary<DamagedObject, bool> damagedObjectsRepaired;
    public Dictionary<Ratman, bool> ratmenRespawned;
    public Dictionary<CannonInteraction, bool> playersFiredCannons;
    public bool mastHasBeenPulled = false;
    public MastSwitch mast;
    public AudioClip[] tutorialSounds;


    public void CheckDamagedObjects() {
        bool allObjects = true;
        foreach (var pfc in instance.damagedObjectsRepaired) {
            if (!pfc.Value) {
                allObjects = false;
                break;
            }
        }

        if (allObjects) {
            RpcPlaySoundClip("RepairsComplete");
            print("Damages have been repaired");

        }
    }

    public void CheckRatmenRespawns() {
        bool allRatmen = true;
        foreach (var pfc in instance.ratmenRespawned) {
            if (!pfc.Value) {
                allRatmen = false;
                break;
            }
        }

        if (allRatmen) {
            RpcPlaySoundClip("RatmenComplete");
            print("Ratmen have been replenished");
        }
    }

    public void CheckPlayersCannonFiring() {
        bool allPlayers = true;
        foreach (var pfc in instance.playersFiredCannons) {
            if (!pfc.Value) {
                allPlayers = false;
                break;
            }
        }

        if (allPlayers) {
            RpcPlaySoundClip("CannonsFired");
            print("Players have fired cannons");
            mast.enabled = true;
        }
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
        for (int i = 0; i < tutorialSounds.Length; i++) {
            if (tutorialSounds[i].name == clip) {
                mySource.PlayOneShot(tutorialSounds[i]);
                break;
            }
        }
    }

    #endregion

    private void Start() {
        if (instance == null) {
            instance = this;
            playersFiredCannons = new Dictionary<CannonInteraction, bool>();
            damagedObjectsRepaired = new Dictionary<DamagedObject, bool>();
            ratmenRespawned = new Dictionary<Ratman, bool>();

            mast.enabled = false;


            mySource = GetComponent<AudioSource>();
        } else {
            Destroy(gameObject);
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
