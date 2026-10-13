using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Faction
{
    Plant,
    Bug,
    Symbiosis
}

[CreateAssetMenu(fileName = "CardData", menuName = "Card Game/Card Data")]
public class CardData : ScriptableObject
{
    public string cardId;
    public string cardName;
    public Faction faction;
    public int cost;
    public Sprite artwork;
    public Sprite plantBackground;
    public Sprite bugBackground;

    [TextArea] public string description;

    // 基础效果（可自行扩展）
    public int baseDamage;
    public int baseHeal;
    public int baseBlock;

    // 共生效果：若本回合此前打出过另一阵营，则额外触发
    public int symbiosisDamage;
    public int symbiosisHeal;
    public int symbiosisBlock;
}
