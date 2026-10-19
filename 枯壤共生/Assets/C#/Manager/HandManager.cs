using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("引用")]
    public CardGameManager gameManager;
    public GameObject cardUIPrefab;
    public Transform handRoot;

    [Header("弧形排列参数")]
    public float radius = 1200f;
    public float angleStep = 8f;
    public float heightOffset = -350f;

    [Header("手牌缩放")]
    public Vector3 handCardScale = Vector3.one;

    private List<GameObject> spawnedCards = new List<GameObject>();

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        //if (gameManager != null)
        //    gameManager.OnHandChanged += RefreshHand;

        //RefreshHand();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnHandChanged -= RefreshHand;
    }

    // 全量刷新（用于出牌后）
    public void RefreshHand()
    {
        if (gameManager == null || cardUIPrefab == null || handRoot == null) return;

        foreach (var go in spawnedCards)
            if (go != null) Destroy(go);
        spawnedCards.Clear();

        var hand = gameManager.Hand;
        for (int i = 0; i < hand.Count; i++)
        {
            GameObject go = Instantiate(cardUIPrefab, handRoot);
            go.transform.localScale = handCardScale;

            CardUI ui = go.GetComponent<CardUI>();
            if (ui != null) ui.Setup(hand[i]);

            spawnedCards.Add(go);
        }

        ArrangeCardsArc();
    }

    // 追加一张（用于抽牌动画）
    public void AppendCard(CardInstance card)
    {
        if (cardUIPrefab == null || handRoot == null) return;

        // 清理一遍旧幽灵
        spawnedCards.RemoveAll(item => item == null);

        GameObject go = Instantiate(cardUIPrefab, handRoot);
        go.transform.localScale = handCardScale;

        CardUI ui = go.GetComponent<CardUI>();
        if (ui != null) ui.Setup(card);

        spawnedCards.Add(go);

        ArrangeCardsArc();
    }

    private void ArrangeCardsArc()
    {
        int count = spawnedCards.Count;
        if (count == 0) return;

        // 清理掉已经被销毁（幽灵）的卡牌引用
        spawnedCards.RemoveAll(item => item == null);

        // 重新获取数量（因为可能清理掉了一些）
        count = spawnedCards.Count;
        if (count == 0) return;

        float totalAngle = angleStep * (count - 1);
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            // 双重保险：如果又被销毁了，直接跳过
            if (spawnedCards[i] == null) continue;

            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(rad) * radius;
            float y = Mathf.Cos(rad) * radius - radius + heightOffset;

            RectTransform rt = spawnedCards[i].GetComponent<RectTransform>();
            if (rt == null) continue; // 防御性编程

            Vector2 pos = new Vector2(x, y);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0, 0, -angle);

            CardUI ui = spawnedCards[i].GetComponent<CardUI>();
            if (ui != null)
                ui.UpdateOriginalPosition(pos, handCardScale);
        }
    }
}