using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [Header("卡面元素")]
    public MeshRenderer backgroundRenderer;  // 背景 Quad
    public MeshRenderer artworkRenderer;     // 插画 Quad
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text descriptionText;

    public CardInstance Instance { get; private set; }

    public void Setup(CardInstance instance)
    {
        Instance = instance;
        if (instance == null || instance.data == null) return;

        CardData data = instance.data;

        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.cost.ToString();
        if (descriptionText != null) descriptionText.text = data.description;

        // 插画：动态换贴图
        if (artworkRenderer != null && data.artwork != null)
        {
            // 关键：用 .material 会创建实例，避免污染其他卡牌
            artworkRenderer.material.mainTexture = data.artwork.texture;
        }

        // 背景也可以按阵营换色/换图
        if (backgroundRenderer != null)
        {
            if (data.faction == Faction.Plant && data.plantBackgroundTexture != null)
                backgroundRenderer.material.mainTexture = data.plantBackgroundTexture;
            else if (data.faction == Faction.Bug && data.bugBackgroundTexture != null)
                backgroundRenderer.material.mainTexture = data.bugBackgroundTexture;
        }
    }
}
