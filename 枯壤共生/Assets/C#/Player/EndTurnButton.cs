using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndTurnButton : MonoBehaviour
{
    public CardGameManager gameManager;
    public Button button;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        if (button == null)
            button = GetComponent<Button>();
    }

    private void Start()
    {
        // 绑定按钮点击事件
        button.onClick.AddListener(OnClick);
        // 监听阶段变化，只有在出牌阶段按钮才能点
        gameManager.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnClick()
    {
        // 告诉管理器：玩家结束出牌了
        gameManager.EndPlayPhase();
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        // 只有在 Play 阶段按钮才亮起，其他阶段变灰不可点
        button.interactable = (phase == GamePhase.Play);
    }
}
