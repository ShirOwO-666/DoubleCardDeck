using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    public CardGameManager gameManager;
    public TextMeshProUGUI healthText;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        // 订阅血量变化事件
        gameManager.OnPlayerHealthChanged += UpdateUI;

        // 初始化显示
        UpdateUI(gameManager.currentPlayerHealth, gameManager.maxPlayerHealth);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnPlayerHealthChanged -= UpdateUI;
    }

    private void UpdateUI(int current, int max)
    {
        healthText.text = $"玩家血量: {current}";
    }
}