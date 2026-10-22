using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardCardSpawner : MonoBehaviour
{
    public CardGameManager gameManager;
    public GameObject card3DPrefab;

    [Header("位置偏移")]
    public float boardCardYOffset = 0.01f;
    public Vector3 boardCardRotation = new Vector3(90, 0, 0);

    private Dictionary<BoardSlot, GameObject> slotViews = new Dictionary<BoardSlot, GameObject>();

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        gameManager.OnCardPlayed += SpawnCardOnBoard;
        gameManager.OnBoardCardRemoved += OnCardRemoved;
        gameManager.OnUnitAttacked += OnUnitAttacked;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnCardPlayed -= SpawnCardOnBoard;
            gameManager.OnBoardCardRemoved -= OnCardRemoved;
            gameManager.OnUnitAttacked -= OnUnitAttacked;
        }
    }

    private void SpawnCardOnBoard(CardInstance card, BoardSlot slot)
    {
        // 1. 清理旧视觉
        if (slotViews.TryGetValue(slot, out GameObject oldView) && oldView != null)
            Destroy(oldView);

        GameObject go = null;

        // 2. 如果形成了共生对（植物+虫）
        if (slot.HasSymbioticPair)
        {
            // 尝试找专属融合配方
            FusionRecipe recipe = gameManager.GetFusionRecipe(slot.plantCard.data, slot.bugCard.data);

            if (recipe != null && recipe.fusion3DPrefab != null)
            {
                go = Instantiate(recipe.fusion3DPrefab);
            }
            else if (gameManager.defaultFusion3DPrefab != null)
            {
                go = Instantiate(gameManager.defaultFusion3DPrefab);
            }
        }

        // 3. 如果没有形成共生，或者上面没生成成功，就生成普通单卡
        if (go == null)
        {
            go = Instantiate(card3DPrefab);
            CardView view = go.GetComponent<CardView>();
            if (view != null) view.Setup(card);
        }

        // 4. 放置位置
        go.transform.SetPositionAndRotation(
            slot.transform.position + Vector3.up * boardCardYOffset,
            Quaternion.Euler(boardCardRotation));

        // 5. 确保挂载了动画脚本
        if (go.GetComponent<CardAttackAnimator>() == null)
            go.AddComponent<CardAttackAnimator>();

        slotViews[slot] = go;
    }

    private void OnCardRemoved(BoardSlot slot, Faction faction)
    {
        if (slotViews.TryGetValue(slot, out GameObject view) && view != null)
        {
            Destroy(view);
            slotViews.Remove(slot);
        }
    }

    private void OnUnitAttacked(BoardSlot slot, bool isPlayer)
    {
        if (slotViews.TryGetValue(slot, out GameObject view) && view != null)
        {
            CardAttackAnimator animator = view.GetComponent<CardAttackAnimator>();
            if (animator != null)
            {
                // 玩家朝屏幕内（Z正方向）冲，敌人朝屏幕外（Z负方向）冲
                Vector3 direction = isPlayer ? Vector3.forward : Vector3.back;
                animator.PlayAttackAnimation(direction);
            }
        }
    }
}