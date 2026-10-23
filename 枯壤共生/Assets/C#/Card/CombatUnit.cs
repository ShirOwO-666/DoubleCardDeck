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
    public bool isSymbioticPair;   // 是否为「植物+虫」共生对（合并单位）

    public int attack
    {
        get
        {
            if (isPlayer)
            {
                // 共生对：攻击力实时求和（植物或虫中途死亡时自动降到剩余一方）
                if (isSymbioticPair && slot != null)
                    return slot.GetCombinedAttack();
                return card != null ? card.currentAttack : 0;
            }
            else return enemy != null ? enemy.currentAttack : 0;
        }
    }

    public string name
    {
        get
        {
            if (isPlayer)
            {
                if (isSymbioticPair) return "共生体";
                return card != null ? card.data.cardName : "?";
            }
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