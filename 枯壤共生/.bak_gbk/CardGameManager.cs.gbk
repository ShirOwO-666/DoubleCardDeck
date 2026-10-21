using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GamePhase
{
    Start,    // 开始阶段（仅第一回合起手）
    Draw,     // 抽牌阶段
    Play,     // 出牌阶段
    End,      // 回合结束（弃牌）
    Combat    // 攻击阶段
}

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

    [Header("敌人配置")]
    public List<EnemyData> enemyPool = new List<EnemyData>();
    public int maxEnemiesPerTurn = 2;

    private int nextEnemyId = 0;
    private List<CardInstance> plantGraveyard = new List<CardInstance>();
    private List<CardInstance> bugGraveyard = new List<CardInstance>();

    // ===== 运行时状态 =====
    private List<CardInstance> plantDeck = new List<CardInstance>();
    private List<CardInstance> bugDeck = new List<CardInstance>();
    private List<CardInstance> hand = new List<CardInstance>();

    public int CurrentEnergy { get; private set; }
    public int SymbiosisValue { get; private set; }
    public bool SymbiosisBurstUnlocked { get; private set; }

    private List<Faction> playedThisTurn = new List<Faction>();
    private int nextInstanceId = 0;
    private int currentTurn = 0;

    // ===== 阶段控制 =====
    public GamePhase CurrentPhase { get; private set; }
    public event Action<GamePhase> OnPhaseChanged;
    private bool playerEndedPlayPhase = false;

    // ===== 事件 =====
    public event Action OnHandChanged;
    public event Action<CardInstance, BoardSlot> OnCardPlayed;
    public event Action<CardInstance> OnCardDrawn;

    // ===== 公开访问 =====
    public IReadOnlyList<CardInstance> Hand => hand;
    public int PlantDeckCount => plantDeck.Count;
    public int BugDeckCount => bugDeck.Count;

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
        // 延迟一帧，确保所有 UI 和动画脚本完成订阅
        StartCoroutine(StartGameDelayed());
    }

    private IEnumerator StartGameDelayed()
    {
        yield return null;
        StartGame();
    }

    public void StartGame()
    {
        BuildDecks();
        StartCoroutine(GameLoop());
    }

    // =========================================================
    // 核心回合循环
    // =========================================================
    private IEnumerator GameLoop()
    {
        // 1. 开始阶段（起手抽牌）
        SetPhase(GamePhase.Start);
        yield return StartCoroutine(DrawStartingHandRoutine());

        // 2. 进入主循环
        while (true)
        {
            // 抽牌阶段（第一回合跳过，因为起手已经抽了）
            if (currentTurn > 0)
            {
                SetPhase(GamePhase.Draw);
                DrawCard(Faction.Plant);
                yield return new WaitForSeconds(0.5f);
                DrawCard(Faction.Bug);
                yield return new WaitForSeconds(0.5f);
            }

            // 出牌阶段（等待玩家点击结束回合）
            SetPhase(GamePhase.Play);
            CurrentEnergy = maxEnergy;
            playedThisTurn.Clear();
            Debug.Log($"回合 {currentTurn + 1} 开始：能量 {CurrentEnergy}");

            playerEndedPlayPhase = false;
            yield return new WaitUntil(() => playerEndedPlayPhase);

            // 回合结束阶段（弃牌等逻辑）
            SetPhase(GamePhase.End);
            yield return StartCoroutine(EndPhaseRoutine());

            // 敌人推进和生成
            AdvanceEnemies();
            yield return new WaitForSeconds(0.3f);
            SpawnEnemiesInWaitingRow();
            yield return new WaitForSeconds(0.3f);

            // 攻击阶段
            SetPhase(GamePhase.Combat);
            yield return StartCoroutine(CombatPhaseRoutine());

            currentTurn++;
        }
    }

    private void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        Debug.Log($"===== 进入阶段：{phase} =====");
        OnPhaseChanged?.Invoke(phase);
    }

    public void EndPlayPhase()
    {
        if (CurrentPhase == GamePhase.Play)
        {
            playerEndedPlayPhase = true;
        }
    }

    // =========================================================
    // 各阶段具体逻辑
    // =========================================================
    private IEnumerator DrawStartingHandRoutine()
    {
        for (int i = 0; i < 3; i++)
        {
            DrawCard(Faction.Plant);
            yield return new WaitForSeconds(0.7f);
        }
        for (int i = 0; i < 3; i++)
        {
            DrawCard(Faction.Bug);
            yield return new WaitForSeconds(0.7f);
        }
    }

    private IEnumerator EndPhaseRoutine()
    {
        // 暂时留空，以后要做弃牌 UI 就在这里写
        Debug.Log("回合结束，检查弃牌...");
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator CombatPhaseRoutine()
    {
        List<CombatUnit> units = new List<CombatUnit>();

        // 己方单位
        for (int col = 0; col < 5; col++)
        {
            BoardSlot slot = GetSlot(2, col);
            if (slot == null) continue;

            if (slot.plantCard != null && slot.plantCard.data != null)
                units.Add(new CombatUnit(slot.plantCard, slot, true, 2, col));
            if (slot.bugCard != null && slot.bugCard.data != null)
                units.Add(new CombatUnit(slot.bugCard, slot, true, 2, col));
            if (slot.symbiosisCard != null && slot.symbiosisCard.data != null)
                units.Add(new CombatUnit(slot.symbiosisCard, slot, true, 2, col));
        }

        // 敌人单位
        for (int col = 0; col < 5; col++)
        {
            BoardSlot slot = GetSlot(1, col);
            if (slot != null && slot.enemy != null && !slot.enemy.IsDead)
                units.Add(new CombatUnit(slot.enemy, slot, false, 1, col));
        }

        // 按攻击力降序排序
        units.Sort((a, b) => b.attack.CompareTo(a.attack));

        foreach (var unit in units)
        {
            if (unit.isPlayer && (unit.card == null || unit.card.currentHealth <= 0)) continue;
            if (!unit.isPlayer && (unit.enemy == null || unit.enemy.IsDead)) continue;

            BoardSlot targetSlot = unit.isPlayer ? GetSlot(1, unit.col) : GetSlot(2, unit.col);
            if (targetSlot == null) continue;

            if (unit.isPlayer)
            {
                if (!targetSlot.HasEnemy()) continue;
                EnemyInstance target = targetSlot.enemy;
                target.currentHealth -= unit.attack;
                Debug.Log($"{unit.name} 攻击 {target.data.enemyName}，造成 {unit.attack} 伤害，敌人剩余 {target.currentHealth}");

                if (target.IsDead)
                {
                    Debug.Log($"敌人 {target.data.enemyName} 死亡，移除");
                    targetSlot.enemy = null;
                }
            }
            else
            {
                if (!targetSlot.HasPlayerCard()) continue;

                CardInstance targetCard = targetSlot.plantCard;
                if (targetCard == null || targetCard.data == null) targetCard = targetSlot.bugCard;
                if (targetCard == null || targetCard.data == null) targetCard = targetSlot.symbiosisCard;
                if (targetCard == null || targetCard.data == null) continue;

                targetCard.currentHealth -= unit.attack;
                Debug.Log($"{unit.name} 攻击 {targetCard.data.cardName}，造成 {unit.attack} 伤害，卡牌剩余 {targetCard.currentHealth}");

                if (targetCard.currentHealth <= 0)
                {
                    Debug.Log($"{targetCard.data.cardName} 死亡，进入墓地");
                    SendToGraveyard(targetCard);
                    targetSlot.RemovePlayerCard(targetCard);
                }
            }
            yield return new WaitForSeconds(0.4f);
        }

        CleanupDeadPlayerCards();
    }

    private void CleanupDeadPlayerCards()
    {
        foreach (var slot in boardSlots)
        {
            if (slot.plantCard != null && slot.plantCard.currentHealth <= 0)
            {
                SendToGraveyard(slot.plantCard);
                slot.plantCard = null;
            }
            if (slot.bugCard != null && slot.bugCard.currentHealth <= 0)
            {
                SendToGraveyard(slot.bugCard);
                slot.bugCard = null;
            }
            if (slot.symbiosisCard != null && slot.symbiosisCard.currentHealth <= 0)
            {
                SendToGraveyard(slot.symbiosisCard);
                slot.symbiosisCard = null;
            }
        }
    }

    // =========================================================
    // 基础逻辑（抽牌、出牌、墓地等）
    // =========================================================
    private void BuildDecks()
    {
        plantDeck = plantDeckData.Select(d => new CardInstance(d, nextInstanceId++)).ToList();
        bugDeck = bugDeckData.Select(d => new CardInstance(d, nextInstanceId++)).ToList();
        Shuffle(plantDeck);
        Shuffle(bugDeck);
    }

    public bool DrawCard(Faction faction)
    {
        if (hand.Count >= handLimit) return false;

        List<CardInstance> deck = faction == Faction.Plant ? plantDeck : bugDeck;
        List<CardInstance> graveyard = faction == Faction.Plant ? plantGraveyard : bugGraveyard;

        if (deck.Count == 0)
        {
            if (graveyard.Count == 0) return false;
            deck.AddRange(graveyard);
            graveyard.Clear();
            Shuffle(deck);
        }

        CardInstance card = deck[0];
        deck.RemoveAt(0);
        hand.Add(card);

        OnCardDrawn?.Invoke(card);
        return true;
    }

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
        if (!CanPlayCard(card, slot)) return false;

        CurrentEnergy -= card.Cost;
        hand.Remove(card);
        slot.Place(card);

        ProcessAlternation(card.Faction);
        ProcessSymbiosisEffect(card);

        NotifyHandChanged();
        OnCardPlayed?.Invoke(card, slot);

        Debug.Log($"打出 {card.data.cardName}，剩余能量 {CurrentEnergy}");
        return true;
    }

    // ... 省略部分旧代码（交替、共生、洗牌等保持原样） ...
    private void ProcessAlternation(Faction faction)
    {
        if (faction == Faction.Symbiosis) return;
        if (playedThisTurn.Count > 0)
        {
            Faction last = playedThisTurn[playedThisTurn.Count - 1];
            if (last != faction) AddSymbiosis(1);
        }
        playedThisTurn.Add(faction);
    }

    private void ProcessSymbiosisEffect(CardInstance card) { /* 保持原样 */ }
    private void AddSymbiosis(int amount) { SymbiosisValue = Mathf.Min(SymbiosisValue + amount, symbiosisMax); }
    private void NotifyHandChanged() { OnHandChanged?.Invoke(); }
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public BoardSlot GetSlot(int row, int col)
    {
        return boardSlots.FirstOrDefault(s => s.row == row && s.col == col);
    }

    public void SendToGraveyard(CardInstance card)
    {
        if (card == null || card.data == null) return;
        if (card.Faction == Faction.Plant) plantGraveyard.Add(card);
        else if (card.Faction == Faction.Bug) bugGraveyard.Add(card);
    }

    public void AdvanceEnemies()
    {
        for (int col = 0; col < 5; col++)
        {
            BoardSlot waiting = GetSlot(0, col);
            BoardSlot enemyRow = GetSlot(1, col);
            if (waiting != null && enemyRow != null && waiting.enemy != null && enemyRow.enemy == null)
            {
                enemyRow.enemy = waiting.enemy;
                waiting.enemy = null;
            }
        }
    }

    public void SpawnEnemiesInWaitingRow()
    {
        if (enemyPool.Count == 0) return;
        int spawned = 0;
        List<int> cols = new List<int> { 0, 1, 2, 3, 4 };
        Shuffle(cols);
        foreach (int col in cols)
        {
            if (spawned >= maxEnemiesPerTurn) break;
            BoardSlot waiting = GetSlot(0, col);
            if (waiting == null || waiting.enemy != null) continue;
            EnemyData data = enemyPool[UnityEngine.Random.Range(0, enemyPool.Count)];
            waiting.enemy = new EnemyInstance(data, nextEnemyId++);
            spawned++;
        }
    }
}