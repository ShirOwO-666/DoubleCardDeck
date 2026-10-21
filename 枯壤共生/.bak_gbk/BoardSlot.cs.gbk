using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public int row;   // 0=待机, 1=敌人, 2=己方
    public int col;   // 0~4

    public CardInstance plantCard;
    public CardInstance bugCard;
    public CardInstance symbiosisCard;
    public EnemyInstance enemy;

    public bool CanPlace(Faction faction)
    {
        switch (faction)
        {
            case Faction.Plant: return plantCard == null || plantCard.data == null;
            case Faction.Bug: return bugCard == null || bugCard.data == null;
            case Faction.Symbiosis: return symbiosisCard == null || symbiosisCard.data == null;
            default: return false;
        }
    }

    public void Place(CardInstance card)
    {
        if (card == null || card.data == null) return;

        switch (card.Faction)
        {
            case Faction.Plant: plantCard = card; break;
            case Faction.Bug: bugCard = card; break;
            case Faction.Symbiosis: symbiosisCard = card; break;
        }
    }

    public bool HasPlayerCard()
    {
        return (plantCard != null && plantCard.data != null)
            || (bugCard != null && bugCard.data != null)
            || (symbiosisCard != null && symbiosisCard.data != null);
    }

    public bool HasEnemy()
    {
        return enemy != null && !enemy.IsDead;
    }

    public void RemovePlayerCard(CardInstance card)
    {
        if (card == null) return;

        if (plantCard == card) plantCard = null;
        else if (bugCard == card) bugCard = null;
        else if (symbiosisCard == card) symbiosisCard = null;
    }
}