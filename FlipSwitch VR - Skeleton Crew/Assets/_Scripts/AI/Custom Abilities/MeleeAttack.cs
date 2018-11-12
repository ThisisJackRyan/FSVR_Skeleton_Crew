using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Opsive.ThirdPersonController.Abilities;

public class MeleeAttack : Ability {

	public override bool CanStartAbility() {
		if (GetComponent<Enemy>()) {
			if (GetComponent<Enemy>().isAttacking) {
				return false;
			}
		}

		return true;
	}

	public override bool CanHaveItemEquipped() {
		return true;
	}

	public override string GetDestinationState( int layer ) {
		if(layer !=  m_AnimatorMonitor.UpperLayerIndex ) {
			return string.Empty;
		}

		return "Melee Attack.Attack";
	}
}
