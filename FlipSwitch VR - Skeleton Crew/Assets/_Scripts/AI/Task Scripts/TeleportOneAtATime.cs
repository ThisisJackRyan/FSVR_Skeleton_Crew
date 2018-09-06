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
            crewman.transform.parent = null;
            GameObject teleTarget = new GameObject();

            if (crewman.name.Contains("Archer") || crewman.name.Contains("Mage")) {
                foreach(GameObject key in VariableHolder.instance.enemyRangedPositions.Keys) {
                    if(VariableHolder.instance.enemyRangedPositions[key] == false) {
                        teleTarget = key;
                        VariableHolder.instance.enemyRangedPositions[key] = true;
                    }
                }
            } else {
                teleTarget = teleportTargets.Value.ToArray()[Random.Range(0, teleportTargets.Value.Count)];
                teleportTargets.Value.Remove(teleTarget);
            }

            crewman.transform.position = teleTarget.transform.position;
            crewman.GetComponent<Behavior>().SetVariableValue("Teleported", true);
            yield return new WaitForSeconds(timeBetweenTeleports.Value);
        }

        transform.parent = null;
        transform.position = teleportTargets.Value.ToArray()[Random.Range(0, teleportTargets.Value.Count)].transform.position;
    }
}
