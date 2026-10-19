using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CardInstance
{
    public CardData data;
    public int instanceId;
    public int currentAttack;
    public int currentHealth;

    public CardInstance(CardData data, int id)
    {
        this.data = data;
        this.instanceId = id;
        currentAttack = data.attack;
        currentHealth = data.health;
    }

    public Faction Faction => data.faction;
    public int Cost => data.cost;
}
