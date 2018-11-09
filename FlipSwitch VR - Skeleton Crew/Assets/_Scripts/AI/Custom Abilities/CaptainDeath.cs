using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Opsive.ThirdPersonController.Abilities;

public class CaptainDeath : Ability {

	public override bool CanStartAbility() {
		return true;
	}

	public override bool CanHaveItemEquipped() {
		return true;
	}

	public override string GetDestinationState( int layer ) {
		if(layer != m_AnimatorMonitor.BaseLayerIndex) {
			return string.Empty;
		}

		return "Captain Death.Death";
	}
}
