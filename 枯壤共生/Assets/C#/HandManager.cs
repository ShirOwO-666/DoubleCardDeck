using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("引用")]
    public CardGameManager gameManager;
    public GameObject cardUIPrefab;
    public Transform handRoot;          // Canvas 下的空物体

    [Header("弧形排列参数")]
    public float radius = 1200f;        // UI 单位，比 3D 大很多
    public float angleStep = 8f;
    public float heightOffset = -350f;  // 手牌整体下移

    private List<GameObject> spawnedCards = new List<GameObject>();

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        if (gameManager != null)
            gameManager.OnHandChanged += RefreshHand;

        RefreshHand();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnHandChanged -= RefreshHand;
    }

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
            CardUI ui = go.GetComponent<CardUI>();
            if (ui != null) ui.Setup(hand[i]);
            spawnedCards.Add(go);
        }

        ArrangeCardsArc();
    }

    private void ArrangeCardsArc()
    {
        int count = spawnedCards.Count;
        if (count == 0) return;

        float totalAngle = angleStep * (count - 1);
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(rad) * radius;
            float y = Mathf.Cos(rad) * radius - radius + heightOffset;

            RectTransform rt = spawnedCards[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }
}