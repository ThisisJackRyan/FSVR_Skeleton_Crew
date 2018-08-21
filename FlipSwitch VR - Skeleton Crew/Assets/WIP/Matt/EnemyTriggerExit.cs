using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyTriggerExit : MonoBehaviour {

    private Enemy enemyScript;

    private void Start() {
        enemyScript = GetComponentInParent<Enemy>();
    }

    private void OnTriggerExit(Collider other) {
        if (other.tag == "Weapon") {
            if (!enemyScript.GetCanBeDamaged()) {
                enemyScript.AllowDamage();
            }
        }

    }

}
