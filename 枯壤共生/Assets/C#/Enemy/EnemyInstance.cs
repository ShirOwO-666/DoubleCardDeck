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

    // 该敌人「推进到敌人区」时所处的回合编号（-1 表示尚未进入敌人区）。
    // 用于实现「刚推进到敌人区的敌人，本回合不攻击，等下一个回合再攻击」。
    public int arrivedInEnemyRowTurn = -1;

    public EnemyInstance(EnemyData data, int id)
    {
        this.data = data;
        this.instanceId = id;
        currentAttack = data.attack;
        currentHealth = data.health;
    }

    public bool IsDead => currentHealth <= 0;
}
