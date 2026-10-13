using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardGameManager : MonoBehaviour
{
    public static CardGameManager Instance { get; private set; }

    [Header("卡组配置")]
    public List<CardData> plantDeckData = new List<CardData>();
    public List<CardData> bugDeckData = new List<CardData>();
    public CardData symbiosisBurstCard;

    [Header("桌面格子")]
    public List<BoardSlot> boardSlots = new List<BoardSlot>();

    [Header("规则参数")]
    public int maxEnergy = 3;
    public int handLimit = 12;
    public int symbiosisMax = 10;

    // ===== 运行时状态 =====
    private List<CardInstance> plantDeck = new List<CardInstance>();
    private List<CardInstance> bugDeck = new List<CardInstance>();
    private List<CardInstance> hand = new List<CardInstance>();

    public int CurrentEnergy { get; private set; }
    public int SymbiosisValue { get; private set; }
    public bool SymbiosisBurstUnlocked { get; private set; }

    private List<Faction> playedThisTurn = new List<Faction>();
    private int nextInstanceId = 0;

    // ===== 事件 =====
    public event Action OnHandChanged;                       // 手牌变化
    public event Action<CardInstance, BoardSlot> OnCardPlayed; // 出牌成功

    // ===== 公开访问 =====
    public IReadOnlyList<CardInstance> Hand => hand;
    public int PlantDeckCount => plantDeck.Count;
    public int BugDeckCount => bugDeck.Count;

    // =========================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    // =========================================================
    // 游戏流程
    // =========================================================

    public void StartGame()
    {
        BuildDecks();
        DrawStartingHand();
        StartTurn();
    }

    private void BuildDecks()
    {
        plantDeck = plantDeckData.Select(d => new CardInstance(d, nextInstanceId++)).ToList();
        bugDeck = bugDeckData.Select(d => new CardInstance(d, nextInstanceId++)).ToList();
        Shuffle(plantDeck);
        Shuffle(bugDeck);
    }

    private void DrawStartingHand()
    {
        for (int i = 0; i < 3; i++) DrawCard(Faction.Plant);
        for (int i = 0; i < 3; i++) DrawCard(Faction.Bug);
    }

    public void StartTurn()
    {
        CurrentEnergy = maxEnergy;
        playedThisTurn.Clear();

        DrawCard(Faction.Plant);
        DrawCard(Faction.Bug);

        Debug.Log($"回合开始：能量 {CurrentEnergy}，共生值 {SymbiosisValue}");
    }

    public void EndTurn()
    {
        Debug.Log("回合结束");
        StartTurn();
    }

    // =========================================================
    // 抽牌
    // =========================================================

    public bool DrawCard(Faction faction)
    {
        if (hand.Count >= handLimit)
        {
            Debug.Log("手牌已满，无法抽牌");
            return false;
        }

        List<CardInstance> deck = faction == Faction.Plant ? plantDeck : bugDeck;
        if (deck.Count == 0)
        {
            Debug.Log($"{faction} 卡组已空");
            return false;
        }

        CardInstance card = deck[0];
        deck.RemoveAt(0);
        hand.Add(card);

        Debug.Log($"抽到 {faction} 卡：{card.data.cardName}");

        NotifyHandChanged();
        return true;
    }

    // =========================================================
    // 出牌
    // =========================================================

    public bool CanPlayCard(CardInstance card, BoardSlot slot)
    {
        if (card == null || slot == null) return false;
        if (!hand.Contains(card)) return false;
        if (CurrentEnergy < card.Cost) return false;
        if (!slot.CanPlace(card.Faction)) return false;
        return true;
    }

    public bool PlayCard(CardInstance card, BoardSlot slot)
    {
        if (!CanPlayCard(card, slot))
        {
            Debug.Log("无法打出该卡牌");
            return false;
        }

        // 扣能量
        CurrentEnergy -= card.Cost;

        // 从手牌移除，放到桌面
        hand.Remove(card);
        slot.Place(card);

        // 交替出牌 → 共生值
        ProcessAlternation(card.Faction);

        // 共生效果
        ProcessSymbiosisEffect(card);

        // 通知手牌变化
        NotifyHandChanged();

        // 通知出牌成功（给桌面生成 3D 卡牌用）
        OnCardPlayed?.Invoke(card, slot);

        Debug.Log($"打出 {card.data.cardName}，剩余能量 {CurrentEnergy}，共生值 {SymbiosisValue}");
        return true;
    }

    private void ProcessAlternation(Faction faction)
    {
        if (faction == Faction.Symbiosis) return;

        if (playedThisTurn.Count > 0)
        {
            Faction last = playedThisTurn[playedThisTurn.Count - 1];
            if (last != faction)
            {
                AddSymbiosis(1);
            }
        }

        playedThisTurn.Add(faction);
    }

    private void ProcessSymbiosisEffect(CardInstance card)
    {
        if (card.Faction == Faction.Symbiosis) return;

        // 本回合此前是否打出过另一阵营
        bool hasOtherFaction = playedThisTurn
            .Take(playedThisTurn.Count - 1)
            .Any(f => f != card.Faction);

        if (hasOtherFaction)
        {
            Debug.Log($"触发 {card.data.cardName} 的共生效果！");
            // 在这里应用额外数值：
            // ApplyDamage(card.data.symbiosisDamage);
            // ApplyHeal(card.data.symbiosisHeal);
            // ApplyBlock(card.data.symbiosisBlock);
        }
    }

    private void AddSymbiosis(int amount)
    {
        SymbiosisValue = Mathf.Min(SymbiosisValue + amount, symbiosisMax);
        Debug.Log($"共生值 +{amount}，当前 {SymbiosisValue}");

        if (SymbiosisValue >= symbiosisMax && !SymbiosisBurstUnlocked)
        {
            UnlockSymbiosisBurst();
        }
    }

    private void UnlockSymbiosisBurst()
    {
        SymbiosisBurstUnlocked = true;
        Debug.Log("共生爆发已解锁！");

        if (symbiosisBurstCard != null)
        {
            hand.Add(new CardInstance(symbiosisBurstCard, nextInstanceId++));
            NotifyHandChanged();
        }
    }

    // =========================================================
    // 工具
    // =========================================================

    private void NotifyHandChanged()
    {
        OnHandChanged?.Invoke();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}