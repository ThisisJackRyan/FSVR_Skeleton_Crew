using UnityEngine;
using UnityEngine.SceneManagement;

public enum TargetType {
    Mast,
    Cannon,
    Ratmen,
    Player
};

public class EnemyTargetInit : MonoBehaviour {

    public TargetType targetType;

    private void OnEnable() {
        AddToList();
    }

    private void AddToList() {
        if (VariableHolder.instance != null) {
            if (targetType == TargetType.Cannon && !VariableHolder.instance.cannons.Contains(gameObject)) {
                VariableHolder.instance.cannons.Add(gameObject);
            } else if (targetType == TargetType.Mast && !VariableHolder.instance.mastTargets.Contains(gameObject)) {
                VariableHolder.instance.mastTargets.Add(gameObject);
            } else if (targetType == TargetType.Ratmen && !VariableHolder.instance.ratmen.Contains(gameObject)) {
                VariableHolder.instance.ratmen.Add(gameObject);
            } else if (targetType == TargetType.Player && !VariableHolder.instance.players.Contains(gameObject)) {
                VariableHolder.instance.players.Add(gameObject);
            }
        }
    }

    private void RemoveFromList() {
        switch (targetType) {
            case TargetType.Mast:
                if (VariableHolder.instance.mastTargets.Contains(gameObject)) {
                    VariableHolder.instance.mastTargets.Remove(gameObject);
                }
                break;
            case TargetType.Cannon:
                if (VariableHolder.instance.cannons.Contains(gameObject)) {
                    VariableHolder.instance.cannons.Remove(gameObject);
                }
                break;
            case TargetType.Ratmen:
                if (VariableHolder.instance.ratmen.Contains(gameObject)) {
                    VariableHolder.instance.ratmen.Remove(gameObject);
                }
                break;
            case TargetType.Player:
                if (VariableHolder.instance.players.Contains(gameObject)) {
                    VariableHolder.instance.players.Remove(gameObject);
                }
                break;
            default:
                Debug.LogError(targetType + " is not a valid type. Sent by " + gameObject.name);
                break;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "EnemyWeapon") {
            if (targetType == TargetType.Cannon) {
                int hp = GetComponent<DamagedObject>().ChangeHealth(other.GetComponentInParent<Enemy>().weapon.damage);
                if (hp <= 0) {
                    RemoveFromList();
                }
            } else if (targetType == TargetType.Ratmen) {
                int hp = GetComponentInParent<Ratman>().ChangeHealth(other.GetComponentInParent<Enemy>().weapon.damage);

            } else if (targetType == TargetType.Player) {
                if (transform.parent.GetComponent<PlayerSetup>().isServer) {

                    int hp = GetComponentInParent<ScriptSyncPlayer>().ChangeHealth(other.GetComponentInParent<Enemy>().weapon.damage);
                    if (hp <= 0) {
                        RemoveFromList();
                    }
                }
            }
        }
    }

    private void OnDisable() {
        RemoveFromList();
    }

}
