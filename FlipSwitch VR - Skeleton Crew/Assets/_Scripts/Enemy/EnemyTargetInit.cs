using UnityEngine;
using UnityEngine.SceneManagement;

public enum TargetType
{
	Mast,
	Cannon,
	Ratmen,
	Player
};

public class EnemyTargetInit : MonoBehaviour {

	public TargetType targetType;

	private void OnEnable() {
		if(VariableHolder.instance != null) {
			if ( targetType == TargetType.Cannon && !VariableHolder.instance.cannons.Contains( gameObject ) ) {
				VariableHolder.instance.cannons.Add( gameObject );
			} else if ( targetType == TargetType.Mast && !VariableHolder.instance.mastTargets.Contains( gameObject ) ) {
				VariableHolder.instance.mastTargets.Add( gameObject );
			} else if ( targetType == TargetType.Ratmen && !VariableHolder.instance.ratmen.Contains( gameObject ) ) {
				VariableHolder.instance.ratmen.Add( gameObject );
			} else if ( targetType == TargetType.Player && !VariableHolder.instance.players.Contains(gameObject)) { 
				VariableHolder.instance.players.Add( gameObject );
			}
		}
	}

	private void OnTriggerEnter( Collider other ) {
		if(other.tag == "EnemyWeapon" ) {
			if ( targetType == TargetType.Cannon) {
				GetComponent<DamagedObject>().ChangeHealth( other.GetComponentInParent<Enemy>().weapon.damage );
			} else if ( targetType == TargetType.Ratmen) {
                GetComponent<Ratman>().ChangeHealth(other.GetComponentInParent<Enemy>().weapon.damage);
			} else if ( targetType == TargetType.Player) {
				if ( transform.parent.GetComponent<PlayerSetup>().isServer ) {

					GetComponentInParent<ScriptSyncPlayer>().ChangeHealth( other.GetComponentInParent<Enemy>().weapon.damage );
				}
			}
		}
	}
}
