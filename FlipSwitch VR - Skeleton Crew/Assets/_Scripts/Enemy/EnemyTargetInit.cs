using UnityEngine;
using UnityEngine.SceneManagement;
using Opsive.ThirdPersonController.Wrappers;
public enum TargetType {
    Mast,
    Cannon,
    Ratmen,
    Player
};

public class EnemyTargetInit : MonoBehaviour {

    public TargetType targetType;

    private void OnEnable() {
        //Opsive.ThirdPersonController.EventHandler.RegisterEvent<int>("DamagedByMelee", ApplyMeleeDamage);
        AddToList();
    }

    private void AddToList() {
        switch (targetType) {
            case TargetType.Mast:
                if (!VariableHolder.instance.mastTargets.Contains(gameObject)) {
                    VariableHolder.instance.mastTargets.Add(gameObject);
                }
                break;
            case TargetType.Cannon:
                if (!VariableHolder.instance.cannons.Contains(gameObject)) {
                    VariableHolder.instance.cannons.Add(gameObject);
                }
                break;
            case TargetType.Ratmen:
                if (!VariableHolder.instance.ratmen.Contains(gameObject)) {
                    VariableHolder.instance.ratmen.Add(gameObject);
                }
                break;
            case TargetType.Player:
                if (!VariableHolder.instance.players.Contains(gameObject)) {
                    VariableHolder.instance.players.Add(gameObject);
                }
                break;
            default:
                Debug.LogError(targetType + " is not a valid type. Sent by " + gameObject.name);
                break;
        }
    }

    public void ApplyMeleeDamage(int dmg) {
        print("apply melee damage called with " + dmg + " coming in");
        if (targetType == TargetType.Cannon) {
            int hp = GetComponent<DamagedObject>().ChangeHealth(dmg);
            if (hp <= 0) {
                RemoveFromList();
            }
        } else if (targetType == TargetType.Ratmen) {
            GetComponentInParent<Ratman>().ChangeHealth(dmg);
        } else if (targetType == TargetType.Player) {
            if (transform.parent.GetComponent<FSVRPlayer>().isServer) {
                int hp = GetComponentInParent<Player>().ChangeHealth(dmg);
                if (hp <= 0) {
                    RemoveFromList();
                }
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

    private void OnDisable() {
        //Opsive.ThirdPersonController.EventHandler.UnregisterEvent<int>("DamagedByMelee", ApplyMeleeDamage);
        RemoveFromList();
    }

}
