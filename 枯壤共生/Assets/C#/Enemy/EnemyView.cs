using TMPro;
using UnityEngine;

/// <summary>
/// 敌方（怪物）卡牌的 3D 视觉组件，挂载在 enemy 预制体的根节点上。
/// 负责根据 EnemyInstance 的数据更新插画、名字、攻击力、生命值等显示。
///
/// 注意：文字字段（nameText/costText/healthText/descriptionText）目前先留空引用，
/// 后续接入文本组件后再在 Unity 里拖拽赋值即可；空引用会被安全跳过，不影响运行。
/// </summary>
public class EnemyView : MonoBehaviour
{
    [Header("渲染元素")]
    public MeshRenderer backgroundRenderer;   // 背景 Quad（EnemyData 暂无背景贴图字段，材质由预制体自带）
    public MeshRenderer artworkRenderer;      // 插画 Quad

    [Header("文字（可选，后续接入文本组件）")]
    public TMP_Text nameText;         // 敌人名字
    public TMP_Text costText;         // 角落数字（对应预制体 CostText），暂用于显示攻击力
    public TMP_Text healthText;       // 生命值（预制体暂无对应子物体，后续接入）
    public TMP_Text descriptionText;  // 描述

    /// <summary>当前视觉对应的敌人实例。</summary>
    public EnemyInstance Instance { get; private set; }

    /// <summary>根据敌人实例刷新显示。</summary>
    public void Setup(EnemyInstance instance)
    {
        Instance = instance;
        if (instance == null || instance.data == null) return;

        EnemyData data = instance.data;

        // 插画：动态设置材质贴图（.material 会实例化材质，避免污染共享材质）
        if (artworkRenderer != null && data.artwork != null)
            artworkRenderer.material.mainTexture = data.artwork.texture;

        // 文字：字段拖好引用才设置，空则跳过
        if (nameText != null) nameText.text = data.enemyName;
        if (costText != null) costText.text = instance.currentAttack.ToString();
        if (healthText != null) healthText.text = instance.currentHealth.ToString();
        if (descriptionText != null) descriptionText.text = data.description;
    }
}
