using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardCardSpawner : MonoBehaviour
{
    public CardGameManager gameManager;
    public GameObject card3DPrefab;
    public GameObject defaultSymbiosisPrefab; // 默认共生体（没有特定配方时用的）

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
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnCardPlayed -= SpawnCardOnBoard;
            gameManager.OnBoardCardRemoved -= OnCardRemoved;
        }
    }

    private void SpawnCardOnBoard(CardInstance card, BoardSlot slot)
    {
        // 清理旧视觉
        if (slotViews.TryGetValue(slot, out GameObject oldView) && oldView != null)
            Destroy(oldView);

        GameObject go = null;

        if (slot.HasSymbioticPair)
        {
            Debug.Log("检测到同格共生，开始查找融合配方...");
            FusionRecipe recipe = gameManager.GetFusionRecipe(slot.plantCard.data, slot.bugCard.data);

            if (recipe != null && recipe.fusion3DPrefab != null)
            {
                Debug.Log("找到专属配方！生成专属融合预制体。");
                go = Instantiate(recipe.fusion3DPrefab);
            }
            else if (gameManager.defaultFusion3DPrefab != null)
            {
                Debug.Log("没找到专属配方，生成默认融合预制体。");
                go = Instantiate(gameManager.defaultFusion3DPrefab);
            }
            else
            {
                Debug.LogWarning("没找到融合预制体！退化为普通单卡。");
            }
        }

        if (go == null)
        {
            go = Instantiate(card3DPrefab);
            CardView view = go.GetComponent<CardView>();
            if (view != null) view.Setup(card);
        }

        // 放置位置
        go.transform.SetPositionAndRotation(
            slot.transform.position + Vector3.up * boardCardYOffset,
            Quaternion.Euler(boardCardRotation));

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
}