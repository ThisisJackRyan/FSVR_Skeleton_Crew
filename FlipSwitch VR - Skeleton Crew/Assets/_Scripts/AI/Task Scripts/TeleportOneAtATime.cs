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
                    if(VariableHolder.instance.enemyRangedPositions[key] == false) {
                        teleTarget = key;
                        break;
                    } else {
						teleTarget = null;
					}
                }

                if (teleTarget == gameObject) {
                    Debug.LogWarning(gameObject.name + " has no keys in variableHolder.enemyrangedPositions");
                } else if(teleTarget != null){
                    VariableHolder.instance.enemyRangedPositions[teleTarget] = true;
                    crewman.GetComponent<Enemy>().rangedTeleTarget = teleTarget;
                }

				//teleTarget = null;
            }

            if (teleTarget == null || teleTarget == gameObject) {
                teleTarget = teleportTargets.Value.ToArray()[Random.Range(0, teleportTargets.Value.Count)];
                teleportTargets.Value.Remove(teleTarget);
            }

			crewman.transform.position = teleTarget.transform.position;
            crewman.transform.rotation = Quaternion.Euler(new Vector3(0, -75, 0));
			crewman.GetComponent<Enemy>().UnParentMe();
			crewman.GetComponent<Behavior>().SetVariableValue( "Teleported", true );
			crewman.GetComponent<Enemy>().TellCaptainIveBoarded();
            crewman.GetComponent<Rigidbody>().useGravity = true;
            //Debug.Log("crewman should be teleported");
            yield return new WaitForSeconds(timeBetweenTeleports.Value);

        }

        transform.parent = null;
        GetComponent<Enemy>().UnParentMe();
        transform.position = teleportTargets.Value.ToArray()[Random.Range(0, teleportTargets.Value.Count)].transform.position;
        GetComponent<Rigidbody>().useGravity = true;
        //Debug.Log("captain should be teleported");
    }
}
