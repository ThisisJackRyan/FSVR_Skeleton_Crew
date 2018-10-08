using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;

public class TeleportOneAtATime : Action {

    public SharedGameObjectList crewmen;
    public SharedGameObjectList teleportTargets;
    public SharedFloat timeBetweenTeleports;

    // Use this for initialization
    public override void OnStart()
    {
        StartCoroutine("TeleportCrewmen");
    }

    private IEnumerator TeleportCrewmen() {
        foreach(var crewman in crewmen.Value) {
            //crewman.transform.parent = null;
            GameObject teleTarget = gameObject;

            if (crewman.name.Contains("Archer") || crewman.name.Contains("Mage")) {
                foreach(GameObject key in VariableHolder.instance.enemyRangedPositions.Keys) {
                    //Debug.Log("looping through ranged teleport target dict");
                    if(VariableHolder.instance.enemyRangedPositions[key] == false) {
                        //Debug.Log("found a ranged teleport target");
                        teleTarget = key;
                        //VariableHolder.instance.enemyRangedPositions[key] = true;
                        //Debug.Log("set tele target to " + teleTarget.name);
                        break;
                    } else {
                        //Debug.Log("ranged teleport target not found");
						teleTarget = null;
					}
                }

                if (teleTarget == gameObject) {
                    Debug.LogWarning(gameObject.name + " has no keys in variableHolder.enemyrangedPositions");
                } else if(teleTarget != null){
                    //Debug.Log("Should be setting the dict instance of teletarget");
                    VariableHolder.instance.enemyRangedPositions[teleTarget] = true;
                    crewman.GetComponent<Enemy>().rangedTeleTarget = teleTarget;
                }

				//teleTarget = null;
            } else if(teleTarget == null || teleTarget == gameObject){
                teleTarget = teleportTargets.Value.ToArray()[Random.Range(0, teleportTargets.Value.Count)];
                teleportTargets.Value.Remove(teleTarget);
            }

			crewman.transform.position = teleTarget.transform.position;
			crewman.GetComponent<Enemy>().UnParentMe();
			crewman.GetComponent<Behavior>().SetVariableValue( "Teleported", true );
			crewman.GetComponent<Enemy>().TellCaptainIveBoarded();
            //Debug.Log("crewman should be teleported");
            yield return new WaitForSeconds(timeBetweenTeleports.Value);

        }

        transform.parent = null;
        GetComponent<Enemy>().UnParentMe();
        transform.position = teleportTargets.Value.ToArray()[Random.Range(0, teleportTargets.Value.Count)].transform.position;
        //Debug.Log("captain should be teleported");
    }
}
