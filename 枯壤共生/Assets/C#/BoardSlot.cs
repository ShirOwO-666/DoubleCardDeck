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

    // 是否有存活的真实敌人（数据有效且未死亡）。
    // 注意：enemy 是 [Serializable] 类，Unity 会把 null 序列化成 data==null 的空实例，
    // 所以必须同时判断 data != null，不能只看 != null。
    public bool HasEnemy()
    {
        return enemy != null && enemy.data != null && !enemy.IsDead;
    }

    // 是否被真实敌人占用（数据有效，不区分死活），用于生成/推进判断
    public bool IsEnemyOccupied()
    {
        return enemy != null && enemy.data != null;
    }

    public void RemovePlayerCard(CardInstance card)
    {
        if (card == null) return;

        if (plantCard == card) plantCard = null;
        else if (bugCard == card) bugCard = null;
        else if (symbiosisCard == card) symbiosisCard = null;
    }

    // =========================================================
    // 同格共生：植物 + 虫 共生对
    // =========================================================

    /// <summary>当前格子是否已形成「植物 + 虫」共生对（两张卡都在场且数据有效）。</summary>
    public bool HasSymbioticPair =>
        plantCard != null && plantCard.data != null &&
        bugCard != null && bugCard.data != null;

    /// <summary>共生对的合并攻击力（植物 + 虫当前攻击力之和）。</summary>
    public int GetCombinedAttack()
    {
        int atk = 0;
        if (plantCard != null) atk += plantCard.currentAttack;
        if (bugCard != null) atk += bugCard.currentAttack;
        return atk;
    }

    /// <summary>共生对的合并生命值（植物 + 虫当前生命之和）。</summary>
    public int GetCombinedHealth()
    {
        int hp = 0;
        if (plantCard != null) hp += plantCard.currentHealth;
        if (bugCard != null) hp += bugCard.currentHealth;
        return hp;
    }

    /// <summary>
    /// 对共生对造成伤害（共享生命池：先扣植物，植物顶不住时溢出到虫）。
    /// 调用前需保证 HasSymbioticPair 为 true。
    /// </summary>
    public void ApplyDamageToSymbioticPair(int damage)
    {
        if (!HasSymbioticPair) return;

        int remaining = damage;

        // 植物优先承受伤害
        int absorbed = Mathf.Min(plantCard.currentHealth, remaining);
        plantCard.currentHealth -= absorbed;
        remaining -= absorbed;

        // 溢出部分由虫承受
        if (remaining > 0)
            bugCard.currentHealth -= remaining;
    }

    // =========================================================
    // 共生效果合并（数据层）：把植物 + 虫的共生效果求和。
    // 实际触发（治疗 / 护盾 / 额外伤害）留待后续步骤接入战斗结算。
    // =========================================================
    public int GetCombinedSymbiosisDamage()
    {
        int v = 0;
        if (plantCard != null && plantCard.data != null) v += plantCard.data.symbiosisDamage;
        if (bugCard != null && bugCard.data != null) v += bugCard.data.symbiosisDamage;
        return v;
    }

    public int GetCombinedSymbiosisHeal()
    {
        int v = 0;
        if (plantCard != null && plantCard.data != null) v += plantCard.data.symbiosisHeal;
        if (bugCard != null && bugCard.data != null) v += bugCard.data.symbiosisHeal;
        return v;
    }

    public int GetCombinedSymbiosisBlock()
    {
        int v = 0;
        if (plantCard != null && plantCard.data != null) v += plantCard.data.symbiosisBlock;
        if (bugCard != null && bugCard.data != null) v += bugCard.data.symbiosisBlock;
        return v;
    }
}