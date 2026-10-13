using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Image backgroundImage;
    public Image artworkImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;

    public CardInstance Instance { get; private set; }

    public void Setup(CardInstance instance)
    {
        Instance = instance;
        if (instance == null || instance.data == null) return;

        CardData data = instance.data;

        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.cost.ToString();
        if (descriptionText != null) descriptionText.text = data.description;
        if (artworkImage != null && data.artwork != null) artworkImage.sprite = data.artwork;

        // 按阵营换背景色
        if (backgroundImage != null)
        {
            if (data.faction == Faction.Plant && data.plantBackground != null)
                backgroundImage.sprite = data.plantBackground;
            else if (data.faction == Faction.Bug && data.bugBackground != null)
                backgroundImage.sprite = data.bugBackground;
        }
    }
}
