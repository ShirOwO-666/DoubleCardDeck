using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatUnit
{
    public CardInstance card;
    public EnemyInstance enemy;
    public BoardSlot slot;
    public bool isPlayer;
    public int row;
    public int col;

    public int attack
    {
        get
        {
            if (isPlayer) return card != null ? card.currentAttack : 0;
            else return enemy != null ? enemy.currentAttack : 0;
        }
    }

    public string name
    {
        get
        {
            if (isPlayer) return card != null ? card.data.cardName : "?";
            else return enemy != null ? enemy.data.enemyName : "?";
        }
    }

    public CombatUnit(CardInstance card, BoardSlot slot, bool isPlayer, int row, int col)
    {
        this.card = card;
        this.slot = slot;
        this.isPlayer = isPlayer;
        this.row = row;
        this.col = col;
    }

    public CombatUnit(EnemyInstance enemy, BoardSlot slot, bool isPlayer, int row, int col)
    {
        this.enemy = enemy;
        this.slot = slot;
        this.isPlayer = isPlayer;
        this.row = row;
        this.col = col;
    }
}