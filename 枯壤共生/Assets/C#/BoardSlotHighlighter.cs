using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 棋盘格子悬停提示：鼠标悬停己方格子（row = 2）时显示白色边框。
/// 待机区（row 0）和敌人区（row 1）不会显示。
///
/// 使用方式：场景中新建一个空物体（如 BoardSlotHighlighter）挂上本脚本即可，
/// 白色边框会在运行时自动生成（程序化纹理 + 四边形），无需手动搭模型或 UI。
/// </summary>
public class BoardSlotHighlighter : MonoBehaviour
{
    [Header("引用（留空会自动查找）")]
    public CardGameManager gameManager;
    public Camera rayCamera;                 // 用于射线检测的相机，留空取 Camera.main

    [Header("显示规则")]
    public int playerRow = 2;                // 只有这一行的格子显示白框
    public bool onlyDuringPlayPhase = true;  // 只在出牌阶段显示
    public bool ignoreWhenPointerOnUI = true; // 鼠标在手牌等 UI 上时不显示格子白框

    [Header("白框外观")]
    public Color frameColor = Color.white;        // 边框颜色（默认白色）
    public float frameScaleMultiplier = 1.12f;    // 边框相对格子的大小
    [Range(0.01f, 0.2f)]
    public float borderThickness = 0.06f;         // 边框粗细（占纹理边长的比例）
    public float cameraOffset = 0.02f;            // 朝相机方向抬高，避免与格子/卡牌穿插闪烁

    private GameObject frame;        // 白框物体（单个，跟随悬停的格子移动）
    private Material frameMaterial;
    private Texture2D frameTexture;
    private Mesh frameMesh;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();
        if (rayCamera == null)
            rayCamera = Camera.main;

        CreateFrame();
    }

    private void OnDestroy()
    {
        // 运行时创建的材质/纹理/网格要手动销毁，避免内存泄漏
        if (frameMaterial != null) Destroy(frameMaterial);
        if (frameTexture != null) Destroy(frameTexture);
        if (frameMesh != null) Destroy(frameMesh);
    }

    private void Update()
    {
        if (frame == null) return;

        if (!TryGetHoveredPlayerSlot(out BoardSlot slot))
        {
            if (frame.activeSelf) frame.SetActive(false);
            return;
        }

        ShowAt(slot);
    }

    // 用射线检测当前鼠标下的「己方格子」；被卡牌等物体挡住时也能正确命中
    private bool TryGetHoveredPlayerSlot(out BoardSlot result)
    {
        result = null;

        if (rayCamera == null)
        {
            rayCamera = Camera.main;
            if (rayCamera == null) return false;
        }

        // 只在出牌阶段显示
        if (onlyDuringPlayPhase)
        {
            if (gameManager == null)
                gameManager = FindObjectOfType<CardGameManager>();
            if (gameManager == null || gameManager.CurrentPhase != GamePhase.Play)
                return false;
        }

        // 鼠标停在 UI（手牌 / 按钮）上时，不显示格子白框
        if (ignoreWhenPointerOnUI && EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return false;

        Ray ray = rayCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 200f);

        float bestDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            BoardSlot slot = hit.collider.GetComponentInParent<BoardSlot>();
            if (slot == null || slot.row != playerRow) continue;

            // 多个格子重叠时取离相机最近的那个
            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                result = slot;
            }
        }

        return result != null;
    }

    // 把白框摆到指定格子上并显示
    private void ShowAt(BoardSlot slot)
    {
        frame.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);

        // 跟随格子大小（用 lossyScale 兼容带父级缩放的层级）
        frame.transform.localScale = slot.transform.lossyScale * frameScaleMultiplier;

        // 朝相机方向抬高一点：避免与格子共面闪烁，也能压在场上卡牌上方
        if (rayCamera != null)
        {
            Vector3 toCamera = (rayCamera.transform.position - slot.transform.position).normalized;
            frame.transform.position += toCamera * cameraOffset;
        }

        if (!frame.activeSelf) frame.SetActive(true);
    }

    // =========================================================
    // 运行时生成白框：程序化四边形 + 程序化边框纹理
    // =========================================================
    private void CreateFrame()
    {
        frame = new GameObject("SlotHighlightFrame");

        frameMesh = CreateQuadMesh();
        MeshFilter filter = frame.AddComponent<MeshFilter>();
        filter.mesh = frameMesh;

        MeshRenderer renderer = frame.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        frameMaterial = CreateFrameMaterial();
        renderer.material = frameMaterial;

        frame.SetActive(false);
    }

    // 1x1 的四边形（中心在原点，面朝 +Z）
    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "HighlightQuad";
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),   // 左下
            new Vector3( 0.5f, -0.5f, 0f),   // 右下
            new Vector3(-0.5f,  0.5f, 0f),   // 左上
            new Vector3( 0.5f,  0.5f, 0f)    // 右上
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        // 绕序保证从 +Z 方向看是正面
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    // 生成「白边框、中心透明」的贴图
    private Texture2D CreateFrameTexture(int size)
    {
        frameTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        frameTexture.name = "HighlightFrameTexture";
        frameTexture.wrapMode = TextureWrapMode.Clamp;
        frameTexture.filterMode = FilterMode.Bilinear;

        int border = Mathf.Max(1, Mathf.RoundToInt(size * borderThickness));
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 clear = new Color32(255, 255, 255, 0);

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < border || y < border ||
                                x >= size - border || y >= size - border;
                pixels[y * size + x] = isBorder ? white : clear;
            }
        }

        frameTexture.SetPixels32(pixels);
        frameTexture.Apply();
        return frameTexture;
    }

    // 透明无光照材质（URP 下 Sprites/Default 可用；找不到时逐个回退）
    private Material CreateFrameMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogWarning("[BoardSlotHighlighter] 找不到可用 Shader，白框可能无法显示");
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = "HighlightFrameMaterial";
        material.color = frameColor;
        material.mainTexture = CreateFrameTexture(128);
        return material;
    }
}
