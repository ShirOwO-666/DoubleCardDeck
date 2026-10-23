using TMPro;
using UnityEngine;

/// <summary>
/// 费用（能量）显示，挂在左下角 UI 文本上。
/// 显示格式可在 Inspector 里用 format 调整，例如 "{0}/{1}" → "3/3"。
/// 注意：当前使用的 TMP 字体不含中文字形，所以默认只显示数字；
/// 以后导入中文字体后，可以把 format 改成 "费用: {0}/{1}"。
/// </summary>
public class EnergyUI : MonoBehaviour
{
    public CardGameManager gameManager;
    public TMP_Text energyText;

    [Tooltip("显示格式：{0} = 当前能量，{1} = 最大能量")]
    public string format = "{0}/{1}";

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<CardGameManager>();

        if (gameManager != null)
        {
            gameManager.OnEnergyChanged += UpdateUI;
            // 先按当前值初始化一次，避免开局空白
            UpdateUI(gameManager.CurrentEnergy, gameManager.maxEnergy);
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnEnergyChanged -= UpdateUI;
    }

    private void UpdateUI(int current, int max)
    {
        if (energyText != null)
            energyText.text = string.Format(format, current, max);
    }
}
