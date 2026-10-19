using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawAnimationManager : MonoBehaviour
{
    [Header("引用")]
    public CardGameManager gameManager;
    public HandManager handManager;
    public GameObject card3DPrefab;

    [Header("卡组起点")]
    public Transform plantDeckSpawn;
    public Transform bugDeckSpawn;

    [Header("飞行终点")]
    public Transform flyTarget;

    [Header("动画参数")]
    public float flyDuration = 0.5f;
    public float arcHeight = 1.5f;
    public float delayBetweenCards = 0.2f;

    [Header("飞行朝向")]
    public Vector3 startRotationEuler = new Vector3(90f, 0f, 0f);   // 起点：平躺
    public Vector3 endRotationEuler = new Vector3(60f, 0f, 0f);     // 终点：立起一点

    private Queue<CardInstance> drawQueue = new Queue<CardInstance>();
    private bool isPlaying = false;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        if (handManager == null)
            handManager = FindObjectOfType<HandManager>();

        gameManager.OnCardDrawn += EnqueueDraw;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnCardDrawn -= EnqueueDraw;
    }

    private void EnqueueDraw(CardInstance card)
    {
        drawQueue.Enqueue(card);

        if (!isPlaying)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isPlaying = true;

        while (drawQueue.Count > 0)
        {
            CardInstance card = drawQueue.Dequeue();

            // 播 3D 动画
            yield return StartCoroutine(DrawRoutine(card));

            // 动画结束，UI 追加一张
            if (handManager != null)
                handManager.AppendCard(card);

            yield return new WaitForSeconds(delayBetweenCards);
        }

        isPlaying = false;
    }

    private IEnumerator DrawRoutine(CardInstance card)
    {
        Transform spawnPoint = card.Faction == Faction.Plant
            ? plantDeckSpawn
            : bugDeckSpawn;

        if (spawnPoint == null)
        {
            Debug.LogWarning($"没有设置 {card.Faction} 的卡组起点");
            yield break;
        }

        GameObject go = Instantiate(card3DPrefab, spawnPoint.position, spawnPoint.rotation);

        CardView view = go.GetComponent<CardView>();
        if (view != null) view.Setup(card);

        Vector3 start = spawnPoint.position;
        Vector3 end = flyTarget.position;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / flyDuration;
            t = Mathf.Clamp01(t);

            // 位置：弧线
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            go.transform.position = pos;

            // 旋转：从起点角度插值到终点角度
            Quaternion rotA = Quaternion.Euler(startRotationEuler);
            Quaternion rotB = Quaternion.Euler(endRotationEuler);
            go.transform.rotation = Quaternion.Slerp(rotA, rotB, t);

            yield return null;
        }

        Destroy(go);
    }
}
