using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场上卡牌的 3D 视觉生成器。
///
/// 核心思路：每当格子里的卡牌数据发生变化（出牌、死亡、移除），
/// 都根据格子「当前的真实数据」重建一次视觉，保证视觉与数据永远同步。
/// 这样可以避免「卡牌数据还在、视觉却被销毁」或「视觉残留」的问题。
/// </summary>
public class BoardCardSpawner : MonoBehaviour
{
    public CardGameManager gameManager;
    public GameObject card3DPrefab;

    [Header("位置偏移")]
    public float boardCardYOffset = 0.01f;
    public Vector3 boardCardRotation = new Vector3(90, 0, 0);

    // 每个格子当前使用的视觉物体（一个格子最多显示一个视觉：单卡或融合体）
    private Dictionary<BoardSlot, GameObject> slotViews = new Dictionary<BoardSlot, GameObject>();

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        gameManager.OnCardPlayed += OnCardPlayed;
        gameManager.OnBoardCardRemoved += OnBoardCardRemoved;
        gameManager.OnUnitAttacked += OnUnitAttacked;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnCardPlayed -= OnCardPlayed;
            gameManager.OnBoardCardRemoved -= OnBoardCardRemoved;
            gameManager.OnUnitAttacked -= OnUnitAttacked;
        }
    }

    // 出牌后：按格子最新数据重建
    private void OnCardPlayed(CardInstance card, BoardSlot slot)
    {
        RefreshSlot(slot);
    }

    // 有卡牌死亡/移除后：按格子最新数据重建（若格子空了则销毁视觉）
    private void OnBoardCardRemoved(BoardSlot slot, Faction faction)
    {
        RefreshSlot(slot);
    }

    /// <summary>
    /// 根据格子的当前数据重建视觉：
    /// - 有共生对 → 融合体视觉
    /// - 只有单卡 → 显示剩余的那张卡（植物 → 虫 → 共生卡 依次取第一个在场的）
    /// - 没有卡牌 → 销毁视觉
    /// </summary>
    private void RefreshSlot(BoardSlot slot)
    {
        if (slot == null) return;

        // 1. 先销毁旧视觉
        if (slotViews.TryGetValue(slot, out GameObject oldView))
        {
            if (oldView != null) Destroy(oldView);
            slotViews.Remove(slot);
        }

        // 2. 按当前数据创建新视觉
        GameObject go = CreateViewForSlot(slot);
        if (go == null) return;   // 格子已空，不再显示

        // 3. 摆到格子上
        go.transform.SetPositionAndRotation(
            slot.transform.position + Vector3.up * boardCardYOffset,
            Quaternion.Euler(boardCardRotation));

        // 4. 确保挂有冲撞动画脚本
        if (go.GetComponent<CardAttackAnimator>() == null)
            go.AddComponent<CardAttackAnimator>();

        slotViews[slot] = go;
    }

    // 根据格子数据创建对应的 3D 视觉物体；格子为空返回 null
    private GameObject CreateViewForSlot(BoardSlot slot)
    {
        // 情况 A：植物 + 虫 共生对 → 优先使用融合预制体
        if (slot.HasSymbioticPair)
        {
            FusionRecipe recipe = gameManager.GetFusionRecipe(slot.plantCard.data, slot.bugCard.data);

            if (recipe != null && recipe.fusion3DPrefab != null)
                return Instantiate(recipe.fusion3DPrefab);

            if (gameManager.defaultFusion3DPrefab != null)
                return Instantiate(gameManager.defaultFusion3DPrefab);
        }

        // 情况 B：单卡（共生对里死了一张后，也会走到这里显示剩下的那张）
        CardInstance display = null;
        if (slot.plantCard != null && slot.plantCard.data != null) display = slot.plantCard;
        else if (slot.bugCard != null && slot.bugCard.data != null) display = slot.bugCard;
        else if (slot.symbiosisCard != null && slot.symbiosisCard.data != null) display = slot.symbiosisCard;

        if (display == null || card3DPrefab == null) return null;

        GameObject go = Instantiate(card3DPrefab);
        CardView view = go.GetComponent<CardView>();
        if (view != null) view.Setup(display);
        return go;
    }

    // 单位攻击时，播放对应格子视觉的冲撞动画
    private void OnUnitAttacked(BoardSlot slot, bool isPlayer)
    {
        if (slotViews.TryGetValue(slot, out GameObject view) && view != null)
        {
            CardAttackAnimator animator = view.GetComponent<CardAttackAnimator>();
            if (animator != null)
            {
                // 我方朝屏幕内（+Z）冲，敌人朝屏幕外（-Z）冲
                Vector3 direction = isPlayer ? Vector3.forward : Vector3.back;
                animator.PlayAttackAnimation(direction);
            }
        }
    }
}
