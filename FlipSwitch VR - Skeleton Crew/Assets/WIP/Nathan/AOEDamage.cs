using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOEDamage : MonoBehaviour {

	public float radius = 5f;
	public int damage = 500;

	public void ApplyAoeDamage() {
		var hitColliders = Physics.OverlapSphere(transform.position, radius);
		foreach (var hitCollider in hitColliders) {
			if (hitCollider.GetComponent<EnemyTargetInit>()) {
				switch (hitCollider.GetComponent<EnemyTargetInit>().targetType) {
					case TargetType.Cannon:
						hitCollider.GetComponent<DamagedObject>().ChangeHealth(damage);
						break;
					case TargetType.Player:
						hitCollider.GetComponentInParent<Player>().ChangeHealth(damage);
						break;
				}
			} else if (hitCollider.GetComponent<EnemyDragonkin>()) {
				hitCollider.GetComponent<EnemyDragonkin>().DestroyMe();
			}
		}
	}
}
