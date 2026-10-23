using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFusion", menuName = "Card Game/Fusion Recipe")]
public class FusionRecipe : ScriptableObject
{
    public CardData plantCard;        // 需要的植物卡
    public CardData bugCard;          // 需要的虫卡
    public GameObject fusion3DPrefab; // 融合后的专属 3D 预制体
}
