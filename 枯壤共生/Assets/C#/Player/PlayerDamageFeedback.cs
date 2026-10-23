using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家受伤时的屏幕反馈：镜头震动 + 全屏红色闪烁。
///
/// 使用方式：在场景里新建一个空物体（建议命名 PlayerDamageFeedback），挂上本脚本即可。
/// 红屏遮罩会在运行时自动创建，无需手动搭 UI；相机留空会自动使用 Camera.main。
/// </summary>
public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("引用")]
    public CardGameManager gameManager;
    public Camera shakeCamera;            // 参与震动的相机，留空则自动取 Camera.main

    [Header("震动参数")]
    public float shakeDuration = 0.25f;   // 震动时长（秒）
    public float shakeAmplitude = 0.25f;  // 震动幅度（世界单位）

    [Header("红屏闪烁")]
    public Color flashColor = new Color(1f, 0f, 0f, 0.35f);  // 受击瞬间的红屏颜色（含透明度）
    public float flashDuration = 0.35f;                       // 红屏淡出时长（秒）

    private Image flashImage;      // 全屏红屏遮罩
    private int lastHealth;        // 上一次的玩家血量，用于判断「掉血」还是「回血」
    private Coroutine shakeRoutine;
    private Coroutine flashRoutine;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        if (shakeCamera == null)
            shakeCamera = Camera.main;

        // 订阅血量变化事件，初始血量记录下来作为比较基准
        if (gameManager != null)
        {
            lastHealth = gameManager.currentPlayerHealth;
            gameManager.OnPlayerHealthChanged += OnPlayerHealthChanged;
        }

        CreateFlashOverlay();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnPlayerHealthChanged -= OnPlayerHealthChanged;
    }

    // 血量变化回调：只有「掉血」才触发反馈，回血不触发
    private void OnPlayerHealthChanged(int current, int max)
    {
        if (current < lastHealth)
            TriggerFeedback();
        lastHealth = current;
    }

    /// <summary>同时触发震动与红屏（会打断上一次未播完的反馈，避免叠加）。</summary>
    public void TriggerFeedback()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    // =========================================================
    // 红屏遮罩：运行时自动创建一个铺满全屏的红色 Image
    // =========================================================
    private void CreateFlashOverlay()
    {
        // 独立 Canvas，Screen Space Overlay + 很高的 sortingOrder，保证盖在所有 UI 之上
        GameObject canvasGo = new GameObject("DamageFlashCanvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        GameObject imageGo = new GameObject("DamageFlash", typeof(Image));
        imageGo.transform.SetParent(canvasGo.transform, false);

        // 拉伸铺满全屏
        RectTransform rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        flashImage = imageGo.GetComponent<Image>();
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        flashImage.raycastTarget = false;   // 不拦截鼠标点击
    }

    // 红屏：从设定的透明度开始，线性淡出
    private IEnumerator FlashRoutine()
    {
        if (flashImage == null) yield break;

        Color c = flashColor;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;   // 用 unscaled，Time.timeScale=0 时也能播放
            c.a = flashColor.a * (1f - Mathf.Clamp01(t / flashDuration));
            flashImage.color = c;
            yield return null;
        }

        c.a = 0f;
        flashImage.color = c;
    }

    // 镜头震动：每帧给相机一个随机偏移，幅度随时间衰减，结束时精确复位
    private IEnumerator ShakeRoutine()
    {
        if (shakeCamera == null) yield break;

        Transform cam = shakeCamera.transform;
        Vector3 basePos = cam.localPosition;   // 记录原始位置，结束后恢复

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;

            float damper = 1f - Mathf.Clamp01(t / shakeDuration);  // 越接近结束，抖动越小
            float x = Random.Range(-1f, 1f) * shakeAmplitude * damper;
            float y = Random.Range(-1f, 1f) * shakeAmplitude * damper;

            cam.localPosition = basePos + new Vector3(x, y, 0f);
            yield return null;
        }

        cam.localPosition = basePos;   // 复位，避免偏移累积
    }
}
