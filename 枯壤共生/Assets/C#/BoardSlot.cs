using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public int slotIndex;

    // 一个格子可以同时放植物和虫
    public CardInstance plantCard;
    public CardInstance bugCard;
    public CardInstance symbiosisCard;

    public bool IsEmpty => plantCard == null && bugCard == null && symbiosisCard == null;

    public bool CanPlace(Faction faction)
    {
        switch (faction)
        {
            case Faction.Plant: return plantCard == null;
            case Faction.Bug: return bugCard == null;
            case Faction.Symbiosis: return symbiosisCard == null;
            default: return false;
        }
    }

    public void Place(CardInstance card)
    {
        switch (card.Faction)
        {
            case Faction.Plant: plantCard = card; break;
            case Faction.Bug: bugCard = card; break;
            case Faction.Symbiosis: symbiosisCard = card; break;
        }
    }
}
