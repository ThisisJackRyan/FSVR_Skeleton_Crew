using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.ThirdPersonController.Wrappers;
using System.Collections;
using UnityEngine;

public class MakeInvincible : Action {

    public SharedFloat timeInvincible;

	// Use this for initialization
	public override void OnStart () {
        GetComponent<CharacterHealth>().Invincible = true;
        StartCoroutine("Wait");
	}

    private IEnumerator Wait() {
        yield return new WaitForSeconds(timeInvincible.Value);
    }
	
    public override void OnEnd() {
        GetComponent<CharacterHealth>().Invincible = false;
        base.OnEnd();
    }
}
