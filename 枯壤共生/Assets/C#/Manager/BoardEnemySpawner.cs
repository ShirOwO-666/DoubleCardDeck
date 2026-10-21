using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人 3D 视觉生成器：监听 CardGameManager 的敌人生命周期事件，
/// 在对应棋盘格子上生成 / 移动 / 销毁敌人的 3D 卡牌视觉。
///
/// 挂载方式：在场景里新建一个空物体（建议与 CardGameManager 同级，命名为 BoardEnemySpawner），
/// 挂上本脚本，并把 enemy 预制体拖到 enemyPrefab 字段上。
/// </summary>
public class BoardEnemySpawner : MonoBehaviour
{
    [Header("引用")]
    public CardGameManager gameManager;
    public GameObject enemyPrefab;    // 敌人 3D 预制体（根节点需挂 EnemyView）

    [Header("位置偏移")]
    public float enemyYOffset = 0.01f;   // 略高于格子，避免穿插

    [Header("旋转角度")]
    public Vector3 enemyRotation = new Vector3(90f, 0f, 0f);  // 与玩家棋盘卡牌一致：平躺朝上

    [Header("推进动画")]
    public float advanceDuration = 0.5f;  // 敌人从待机区前进到敌人区的动画时长（秒）

    // 敌人实例 -> 对应的 3D 视觉 GameObject 的映射，用于推进和死亡时查找
    private Dictionary<EnemyInstance, GameObject> enemyViews =
        new Dictionary<EnemyInstance, GameObject>();

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        // 订阅敌人生命周期事件
        gameManager.OnEnemySpawned += OnEnemySpawned;
        gameManager.OnEnemyAdvanced += OnEnemyAdvanced;
        gameManager.OnEnemyDied += OnEnemyDied;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnEnemySpawned -= OnEnemySpawned;
            gameManager.OnEnemyAdvanced -= OnEnemyAdvanced;
            gameManager.OnEnemyDied -= OnEnemyDied;
        }
    }

    // 敌人在待机行生成时，实例化对应视觉
    private void OnEnemySpawned(EnemyInstance enemy, BoardSlot slot)
    {
        if (enemyPrefab == null || slot == null) return;

        GameObject go = Instantiate(enemyPrefab);
        go.transform.SetPositionAndRotation(
            slot.transform.position + Vector3.up * enemyYOffset,
            Quaternion.Euler(enemyRotation));

        EnemyView view = go.GetComponent<EnemyView>();
        if (view != null) view.Setup(enemy);

        enemyViews[enemy] = go;
    }

    // 敌人从待机行推进到敌人行：播放前进动画（从旧位置平滑滑到新格子）
    private void OnEnemyAdvanced(EnemyInstance enemy, BoardSlot slot)
    {
        if (!enemyViews.TryGetValue(enemy, out GameObject go) || go == null) return;
        StartCoroutine(MoveToSlot(go, slot));
    }

    // 将敌人视觉从当前位置平滑移动到目标格子
    private IEnumerator MoveToSlot(GameObject go, BoardSlot slot)
    {
        Vector3 start = go.transform.position;
        Vector3 end = slot.transform.position + Vector3.up * enemyYOffset;
        Quaternion targetRot = Quaternion.Euler(enemyRotation);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / advanceDuration;
            t = Mathf.Clamp01(t);
            go.transform.position = Vector3.Lerp(start, end, t);
            go.transform.rotation = Quaternion.Slerp(go.transform.rotation, targetRot, t);
            yield return null;
        }

        go.transform.SetPositionAndRotation(end, targetRot);
    }

    // 敌人死亡时，销毁对应视觉
    private void OnEnemyDied(EnemyInstance enemy)
    {
        if (enemyViews.TryGetValue(enemy, out GameObject go))
        {
            if (go != null) Destroy(go);
            enemyViews.Remove(enemy);
        }
    }
}
