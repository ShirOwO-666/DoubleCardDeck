using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GamePhase
{
    Start, Draw, Play, End, Combat
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
    public int initialEnemyCount = 1;
    public int enemyGrowthPerTurn = 1;
    public int maxEnemySpawnPerTurn = 5;

    // ===== 新增：融合配置 =====
    [Header("融合配置")]
    public List<FusionRecipe> allFusionRecipes = new List<FusionRecipe>();
    public GameObject defaultFusion3DPrefab;

    private int nextEnemyId = 0;
    private List<CardInstance> plantGraveyard = new List<CardInstance>();
    private List<CardInstance> bugGraveyard = new List<CardInstance>();

    private List<CardInstance> plantDeck = new List<CardInstance>();
    private List<CardInstance> bugDeck = new List<CardInstance>();
    private List<CardInstance> hand = new List<CardInstance>();

    public int CurrentEnergy { get; private set; }
    public int SymbiosisValue { get; private set; }
    public bool SymbiosisBurstUnlocked { get; private set; }

    private int nextInstanceId = 0;
    private int currentTurn = 0;

    public GamePhase CurrentPhase { get; private set; }
    public event Action<GamePhase> OnPhaseChanged;
    private bool playerEndedPlayPhase = false;

    // ===== 事件 =====
    public event Action OnHandChanged;
    public event Action<CardInstance, BoardSlot> OnCardPlayed;
    public event Action<CardInstance> OnCardDrawn;

    // ===== 新增：场上卡牌移除事件（用于同步销毁视觉） =====
    public event Action<BoardSlot, Faction> OnBoardCardRemoved;

    public event Action<EnemyInstance, BoardSlot> OnEnemySpawned;
    public event Action<EnemyInstance, BoardSlot> OnEnemyAdvanced;
    public event Action<EnemyInstance> OnEnemyDied;

    public IReadOnlyList<CardInstance> Hand => hand;
    public int PlantDeckCount => plantDeck.Count;
    public int BugDeckCount => bugDeck.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        boardSlots = new List<BoardSlot>(FindObjectsOfType<BoardSlot>());
        boardSlots.Sort((a, b) => a.row != b.row ? a.row.CompareTo(b.row) : a.col.CompareTo(b.col));
    }

    private void Start()
    {
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

    private IEnumerator GameLoop()
    {
        SetPhase(GamePhase.Start);
        SpawnEnemiesInWaitingRow();
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(DrawStartingHandRoutine());

        while (true)
        {
            if (currentTurn > 0)
            {
                SetPhase(GamePhase.Draw);
                DrawCard(Faction.Plant);
                yield return new WaitForSeconds(0.5f);
                DrawCard(Faction.Bug);
                yield return new WaitForSeconds(0.5f);
            }

            SetPhase(GamePhase.Play);
            CurrentEnergy = maxEnergy;
            playerEndedPlayPhase = false;
            yield return new WaitUntil(() => playerEndedPlayPhase);

            SetPhase(GamePhase.End);
            yield return StartCoroutine(EndPhaseRoutine());

            AdvanceEnemies();
            yield return new WaitForSeconds(0.3f);
            SpawnEnemiesInWaitingRow();
            yield return new WaitForSeconds(0.3f);

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
        if (CurrentPhase == GamePhase.Play) playerEndedPlayPhase = true;
    }

    private IEnumerator DrawStartingHandRoutine()
    {
        for (int i = 0; i < 3; i++) { DrawCard(Faction.Plant); yield return new WaitForSeconds(0.7f); }
        for (int i = 0; i < 3; i++) { DrawCard(Faction.Bug); yield return new WaitForSeconds(0.7f); }
    }

    private IEnumerator EndPhaseRoutine()
    {
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator CombatPhaseRoutine()
    {
        List<CombatUnit> units = new List<CombatUnit>();

        for (int col = 0; col < 5; col++)
        {
            BoardSlot slot = GetSlot(2, col);
            if (slot == null) continue;

            if (slot.HasSymbioticPair)
            {
                units.Add(new CombatUnit((CardInstance)null, slot, true, 2, col) { isSymbioticPair = true });
            }
            else
            {
                if (slot.plantCard != null && slot.plantCard.data != null) units.Add(new CombatUnit(slot.plantCard, slot, true, 2, col));
                if (slot.bugCard != null && slot.bugCard.data != null) units.Add(new CombatUnit(slot.bugCard, slot, true, 2, col));
                if (slot.symbiosisCard != null && slot.symbiosisCard.data != null) units.Add(new CombatUnit(slot.symbiosisCard, slot, true, 2, col));
            }
        }

        for (int col = 0; col < 5; col++)
        {
            BoardSlot slot = GetSlot(1, col);
            if (slot != null && slot.HasEnemy()) units.Add(new CombatUnit(slot.enemy, slot, false, 1, col));
        }

        units.Sort((a, b) => b.attack.CompareTo(a.attack));

        foreach (var unit in units)
        {
            if (unit.isPlayer)
            {
                if (unit.isSymbioticPair) { if (unit.slot == null || !unit.slot.HasPlayerCard()) continue; }
                else if (unit.card == null || unit.card.currentHealth <= 0) continue;
            }
            else if (unit.enemy == null || unit.enemy.IsDead) continue;

            BoardSlot targetSlot = unit.isPlayer ? GetSlot(1, unit.col) : GetSlot(2, unit.col);
            if (targetSlot == null) continue;

            if (unit.isPlayer)
            {
                if (!targetSlot.HasEnemy()) continue;
                EnemyInstance target = targetSlot.enemy;
                target.currentHealth -= unit.attack;

                if (target.IsDead)
                {
                    OnEnemyDied?.Invoke(target);
                    targetSlot.enemy = null;
                }
            }
            else
            {
                if (!targetSlot.HasPlayerCard()) continue;

                if (targetSlot.HasSymbioticPair)
                {
                    targetSlot.ApplyDamageToSymbioticPair(unit.attack);
                    if (targetSlot.plantCard != null && targetSlot.plantCard.currentHealth <= 0)
                    {
                        SendToGraveyard(targetSlot.plantCard);
                        OnBoardCardRemoved?.Invoke(targetSlot, Faction.Plant);
                        targetSlot.plantCard = null;
                    }
                    if (targetSlot.bugCard != null && targetSlot.bugCard.currentHealth <= 0)
                    {
                        SendToGraveyard(targetSlot.bugCard);
                        OnBoardCardRemoved?.Invoke(targetSlot, Faction.Bug);
                        targetSlot.bugCard = null;
                    }
                }
                else
                {
                    CardInstance targetCard = targetSlot.plantCard;
                    if (targetCard == null || targetCard.data == null) targetCard = targetSlot.bugCard;
                    if (targetCard == null || targetCard.data == null) targetCard = targetSlot.symbiosisCard;
                    if (targetCard == null || targetCard.data == null) continue;

                    targetCard.currentHealth -= unit.attack;
                    if (targetCard.currentHealth <= 0)
                    {
                        SendToGraveyard(targetCard);
                        OnBoardCardRemoved?.Invoke(targetSlot, targetCard.Faction);
                        targetSlot.RemovePlayerCard(targetCard);
                    }
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
                OnBoardCardRemoved?.Invoke(slot, Faction.Plant);
                slot.plantCard = null;
            }
            if (slot.bugCard != null && slot.bugCard.currentHealth <= 0)
            {
                SendToGraveyard(slot.bugCard);
                OnBoardCardRemoved?.Invoke(slot, Faction.Bug);
                slot.bugCard = null;
            }
            if (slot.symbiosisCard != null && slot.symbiosisCard.currentHealth <= 0)
            {
                SendToGraveyard(slot.symbiosisCard);
                OnBoardCardRemoved?.Invoke(slot, Faction.Symbiosis);
                slot.symbiosisCard = null;
            }
        }
    }

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

        bool wasPair = slot.HasSymbioticPair;
        slot.Place(card);

        if (!wasPair && slot.HasSymbioticPair)
            AddSymbiosis(1);

        ProcessSymbiosisEffect(card);
        NotifyHandChanged();
        OnCardPlayed?.Invoke(card, slot);

        return true;
    }

    // ===== 新增：融合配方查找 =====
    public FusionRecipe GetFusionRecipe(CardData plant, CardData bug)
    {
        foreach (var recipe in allFusionRecipes)
        {
            if ((recipe.plantCard == plant && recipe.bugCard == bug) ||
                (recipe.plantCard == bug && recipe.bugCard == plant))
            {
                return recipe;
            }
        }
        return null;
    }

    private void ProcessSymbiosisEffect(CardInstance card) { }
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

    public BoardSlot GetSlot(int row, int col) => boardSlots.FirstOrDefault(s => s.row == row && s.col == col);

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
            if (waiting != null && enemyRow != null && waiting.IsEnemyOccupied() && !enemyRow.IsEnemyOccupied())
            {
                EnemyInstance moved = waiting.enemy;
                enemyRow.enemy = moved;
                waiting.enemy = null;
                OnEnemyAdvanced?.Invoke(moved, enemyRow);
            }
        }
    }

    public void SpawnEnemiesInWaitingRow()
    {
        if (enemyPool.Count == 0) return;
        int spawnCount = Mathf.Min(initialEnemyCount + currentTurn * enemyGrowthPerTurn, maxEnemySpawnPerTurn);
        int spawned = 0;
        List<int> cols = new List<int> { 0, 1, 2, 3, 4 };
        Shuffle(cols);
        foreach (int col in cols)
        {
            if (spawned >= spawnCount) break;
            BoardSlot waiting = GetSlot(0, col);
            if (waiting == null || waiting.IsEnemyOccupied()) continue;
            EnemyData data = enemyPool[UnityEngine.Random.Range(0, enemyPool.Count)];
            waiting.enemy = new EnemyInstance(data, nextEnemyId++);
            OnEnemySpawned?.Invoke(waiting.enemy, waiting);
            spawned++;
        }
    }
}