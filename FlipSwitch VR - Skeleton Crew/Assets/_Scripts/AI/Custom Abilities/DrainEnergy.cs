﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Opsive.ThirdPersonController.Abilities;

public class DrainEnergy : Ability {

	public override bool CanStartAbility() {
		return VariableHolder.instance.enemyMeleePositions.ContainsValue(false) || VariableHolder.instance.enemyRangedPositions.ContainsValue(false);
	}

	public override bool CanHaveItemEquipped() {
		return true;
	}

	public override string GetDestinationState( int layer ) {
		if(layer != m_AnimatorMonitor.BaseLayerIndex && layer != m_AnimatorMonitor.UpperLayerIndex ) {
			return string.Empty;
		}

		return "Drain Energy.Drain";
	}
}
