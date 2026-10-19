using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform rect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalPos;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CardUI ui = GetComponent<CardUI>();
        if (ui != null) ui.isDragging = true;

        originalPos = rect.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CardUI ui = GetComponent<CardUI>();
        if (ui != null) ui.isDragging = false;

        canvasGroup.blocksRaycasts = true;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        Debug.Log($"射线起点：{ray.origin}，方向：{ray.direction}");

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.Log($"射线打到：{hit.collider.name}，路径：{GetPath(hit.collider.transform)}");

            BoardSlot slot = hit.collider.GetComponentInParent<BoardSlot>();
            if (slot != null)
            {
                Debug.Log($"找到格子：{slot.name}");

                if (ui != null && CardGameManager.Instance.PlayCard(ui.Instance, slot))
                {
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                Debug.Log("打到的不是 BoardSlot");
            }
        }
        else
        {
            Debug.Log("射线没打到任何物体");
        }

        // 没放到格子上，回原位
        rect.anchoredPosition = originalPos;
    }

    private string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}

