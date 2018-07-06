using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Side
{
    left,
    right
};

public class Captain : MonoBehaviour {
    // Low priority audio clips (reminders everything 30s if needed)
    public AudioClip leftCannonsDown;
    public AudioClip leftCannonsAndRatmen;
    public AudioClip rightCannonsDown;
    public AudioClip rightCannonsAndRatmen;
    public AudioClip leftAndRightCannons;
    public AudioClip leftAndRightCannonsAndRatmen;
    public AudioClip ratmenOnly;
    public AudioClip shouldNeverGetHere;

    // High priority audio clips
    public AudioClip enemiesAtMast;
    public AudioClip cannonDestroyedLeftSide;
    public AudioClip cannonDestroyedRightSide;
    public AudioClip cannonDestroyedBothSides;
    public AudioClip enemyIncomingLeft;
    public AudioClip enemyIncomingRight;
    public AudioClip enemyIncomingBoth;
    public AudioClip enemyBoardingLeft;
    public AudioClip enemyBoardingRight;
    public AudioClip enemyBoardingBoth;

    // End of encounter audio clips
    public AudioClip endOfEncounterRatmen;
    public AudioClip endOfEncounterCannons;
    public AudioClip endOfEncounterBoth;
    public AudioClip endOfEncounterAllIsWell;

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

    private void Start()
    {
        mySource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (audioTriggered)
        {
            timeElapsed += Time.deltaTime; // increment the time
            if(timeElapsed >= timeBetweenReminders) // if time between reminders time has passed
            {
                if (audioQueue.Count == 0)
                    PlayGenericVoice();
            }
        }
    }

    private void PlayGenericVoice()
    {
        AudioClip clip = DetermineLine();
        mySource.clip = clip;
        mySource.Play();
    }

    private AudioClip DetermineLine()
    {
        AudioClip retClip;
        
        if(numLeftCannonsDamaged > 0)
        {
            if(numRightCannonsDamaged > 0)
            {
                if(numRatmenDead > 0)
                {
                    retClip = leftAndRightCannonsAndRatmen;
                }
                else
                {
                    retClip = leftAndRightCannons;
                }
            }
            else if (numRatmenDead > 0)
            {
                retClip = leftCannonsAndRatmen;
            }
            else
            {
                retClip = leftCannonsDown;
            }
        }

        else if(numRightCannonsDamaged > 0)
        {
            if(numRatmenDead > 0)
            {
                retClip = rightCannonsAndRatmen;
            }
            else
            {
                retClip = rightCannonsDown;
            }
        }

        // by this point all damaged cannons should be addressed, only ratmen are left

        else if (numRatmenDead > 0) // ratmen only
        {
            retClip = ratmenOnly;
        }

        else
        {
            // SHOULD NEVER GET HERE, insert some random clip for captain to say in case it does, maybe something like everything is good well done.
            retClip = shouldNeverGetHere;
        }

        return retClip;
    }

    private AudioClip DetermineEndOfEncounterLine()
    {
        AudioClip retClip; 

        if(numLeftCannonsDamaged > 0 || numRightCannonsDamaged > 0) // if any cannons destroyed
        {
            if(numRatmenDead > 0) //cannons + ratmen 
            {
                retClip = endOfEncounterBoth; // both need work
            }
            else // cannons
            {
                retClip = endOfEncounterCannons;
            }
        }
        else if(numRatmenDead > 0) // ratmen
        {
            retClip = endOfEncounterRatmen;
        }
        else // nothing needs work
        {
            retClip = endOfEncounterAllIsWell;
        }
        
        return retClip;
    }

    public void EndOfEncounter()
    {
        audioQueue.Clear(); // Get rid of all priority lines
        AudioClip clip = DetermineEndOfEncounterLine();
        mySource.clip = clip;
        mySource.Play();
    }

    public void CannonDamaged(Side side)
    {
        audioTriggered = true;

        if (side == Side.left)
        {
            numLeftCannonsDamaged++;
        }
        else
        {
            numRightCannonsDamaged++;
        }
    }

    public void CannonRepaired(Side side)
    {
        if (side == Side.left)
            numLeftCannonsDamaged--;
        else
            numRightCannonsDamaged--;

        CheckIfNeedReminder();

    }

    public void RatmenKilled()
    {
        numRatmenDead++;
    }

    public void RatmenReplenished()
    {
        numRatmenDead--;

        CheckIfNeedReminder();
    }

    private void CheckIfNeedReminder()
    {
        if(numRatmenDead == 0 && numLeftCannonsDamaged == 0 && numRightCannonsDamaged == 0) // if all non priority variables are zero, don't need reminder line
        {
            audioTriggered = false;
            timeElapsed = 0f;
        }
    }

    public void EnemyIncoming(Side side)
    {
        if (side == Side.left)
        {
            if (!mySource.isPlaying && audioQueue.Count == 0)
            {
                mySource.clip = enemyIncomingLeft;
                mySource.Play();
            }
            else
            {
                audioQueue.Add(enemyIncomingLeft);
            }
        }
        else
        {
            if (!mySource.isPlaying && audioQueue.Count == 0)
            {
                mySource.clip = enemyIncomingRight;
                mySource.Play();
            }
            else
            {
                audioQueue.Add(enemyIncomingRight);
            }
        }
    }

    public void EnemyBoarding(Side side)
    {
        if (side == Side.left)
        {
            if (!mySource.isPlaying && audioQueue.Count == 0)
            {
                mySource.clip = enemyBoardingLeft;
                mySource.Play();
            }
            else
            {
                audioQueue.Add(enemyBoardingLeft);
            }
        }
        else
        {
            if (!mySource.isPlaying && audioQueue.Count == 0)
            {
                mySource.clip = enemyBoardingRight;
                mySource.Play();
            }
            else
            {
                audioQueue.Add(enemyBoardingRight);
            }
        }
    }

    public void CannonDestroyed(Side side)
    {
        if(side == Side.left)
        {
            if (mySource.isPlaying)
            {
                audioQueue.Add(cannonDestroyedLeftSide);
            }
            else
            {
                mySource.clip = cannonDestroyedLeftSide;
                mySource.Play();
            }
        }
        else
        {
            if (mySource.isPlaying)
            {
                audioQueue.Add(cannonDestroyedRightSide);
            }
            else
            {
                mySource.clip = cannonDestroyedRightSide;
                mySource.Play();
            }
        }
    }
}
