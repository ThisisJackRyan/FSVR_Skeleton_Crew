using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Ratman : NetworkBehaviour {

    [SyncVar (hook="OnHealthChange")] public int health = 100;

    void OnHealthChange(int n)
    {
        health = n;
    }

    private void Start()
    {
        if (!isServer)
        {
            OnHealthChange(health);
        }
    }

    private void OnEnable()
    {        
        VariableHolder.instance.ratmen.Add(gameObject);
	}

    public int GetHealth()
    {
        return health;
    }

    public int ChangeHealth(int amount, bool damage = true)
    {
        if (!isServer)
            return health;

        if (damage)
        {
            health -= Mathf.Abs(amount);
            health = (health < 0) ? 0 : health;
        }
        else
        {
            health += Mathf.Abs(amount);
            health = (health > 100) ? 100 : health;
        }
        return health;
    }
}
