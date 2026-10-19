using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyInstance
{
    public EnemyData data;
    public int currentAttack;
    public int currentHealth;
    public int instanceId;

    public EnemyInstance(EnemyData data, int id)
    {
        this.data = data;
        this.instanceId = id;
        currentAttack = data.attack;
        currentHealth = data.health;
    }

    public bool IsDead => currentHealth <= 0;
}
