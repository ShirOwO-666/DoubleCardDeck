using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI 引用")]
    public Image backgroundImage;
    public Image artworkImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;

    [Header("悬停参数")]
    public float hoverYOffset = 80f;      // 向上浮动的像素
    public float hoverScale = 1.15f;      // 放大倍率
    public float lerpSpeed = 10f;         // 插值速度，越大越快

    public CardInstance Instance { get; private set; }

    private RectTransform rect;
    private Vector2 originalPos;
    private Vector3 originalScale;
    private Vector2 targetPos;
    private Vector3 targetScale;

    public bool isDragging = false;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalPos = rect.anchoredPosition;
        originalScale = rect.localScale;
        targetPos = originalPos;
        targetScale = originalScale;
    }

    private void Update()
    {
        // 平滑插值到目标位置和缩放
        if (isDragging) return;   // 拖拽时不动

        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, Time.deltaTime * lerpSpeed);
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.deltaTime * lerpSpeed);
    }

    public void Setup(CardInstance instance)
    {
        Instance = instance;
        if (instance == null || instance.data == null) return;

        CardData data = instance.data;

        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.cost.ToString();
        if (descriptionText != null) descriptionText.text = data.description;
        if (artworkImage != null && data.artwork != null) artworkImage.sprite = data.artwork;

        if (backgroundImage != null)
        {
            if (data.faction == Faction.Plant && data.plantBackground != null)
                backgroundImage.sprite = data.plantBackground;
            else if (data.faction == Faction.Bug && data.bugBackground != null)
                backgroundImage.sprite = data.bugBackground;
        }
    }

    // ===== 悬停 =====

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPos = originalPos + new Vector2(0, hoverYOffset);
        targetScale = originalScale * hoverScale;

        // 移到最前面，避免被其他卡挡住
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = originalPos;
        targetScale = originalScale;
    }

    // HandManager 排列后调用，更新原始位置
    public void UpdateOriginalPosition(Vector2 pos, Vector3 scale)
    {
        originalPos = pos;
        originalScale = scale;
        targetPos = pos;
        targetScale = scale;
    }
}
