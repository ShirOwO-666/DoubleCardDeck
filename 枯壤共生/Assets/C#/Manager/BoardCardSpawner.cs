using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardCardSpawner : MonoBehaviour
{
    public CardGameManager gameManager;
    public GameObject card3DPrefab;

    [Header("Î»ÖÃÆ«ÒÆ")]
    public float boardCardYOffset = 0.01f;

    [Header("Ðý×ª½Ç¶È")]
    public Vector3 boardCardRotation = new Vector3(90, 0, 0);

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        gameManager.OnCardPlayed += SpawnCardOnBoard;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnCardPlayed -= SpawnCardOnBoard;
    }

    private void SpawnCardOnBoard(CardInstance card, BoardSlot slot)
    {
        GameObject go = Instantiate(card3DPrefab);

        go.transform.SetPositionAndRotation(
            slot.transform.position + Vector3.up * boardCardYOffset,
            Quaternion.Euler(boardCardRotation));

        CardView view = go.GetComponent<CardView>();
        if (view != null) view.Setup(card);
    }
}
