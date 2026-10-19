using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Card Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyId;
    public string enemyName;
    public Sprite artwork;
    public int attack;
    public int health;
    [TextArea] public string description;
}
