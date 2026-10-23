using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

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

    [Header("玩家状态")]
    public int maxPlayerHealth = 20;
    public int currentPlayerHealth { get; private set; }
    public TextMeshProUGUI healthTextUI;

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
    public event Action<BoardSlot, bool> OnUnitAttacked;
    public event Action<int, int> OnPlayerHealthChanged; // 当前血量, 最大血量
    public event Action<int, int> OnEnergyChanged;       // 当前能量, 最大能量


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
        currentPlayerHealth = maxPlayerHealth;
        CurrentEnergy = maxEnergy;   // 初始就按满能量显示，避免开局 UI 显示 0

    }

    private void Start()
    {
        StartCoroutine(StartGameDelayed());
    }
    // ===== 玩家扣血与 UI 更新 =====
    private void UpdatePlayerHealth(int amount)
    {
        if (currentPlayerHealth <= 0) return; // 如果已经死了就不扣了

        currentPlayerHealth += amount; // 传入负数就是扣血
        if (currentPlayerHealth < 0) currentPlayerHealth = 0;

        // 更新 UI
        if (healthTextUI != null)
        {
            healthTextUI.text = $"玩家血量: {currentPlayerHealth} / {maxPlayerHealth}";
        }

        // 广播血量变化（PlayerHealthUI 与受伤反馈 PlayerDamageFeedback 都靠这个事件驱动）
        OnPlayerHealthChanged?.Invoke(currentPlayerHealth, maxPlayerHealth);

        Debug.Log($"玩家受到攻击！当前血量: {currentPlayerHealth}");

        // 游戏结束判定
        if (currentPlayerHealth <= 0)
        {
            Debug.Log("游戏结束！玩家死亡！");
            // 这里可以暂停游戏或弹出失败面板
            // Time.timeScale = 0; 
        }
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
            OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);   // 回合开始：能量回满
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

        // 1. 收集己方单位
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

        // 2. 收集敌人单位（跳过本回合刚推进到敌人区的敌人：要等一个回合后才攻击）
        for (int col = 0; col < 5; col++)
        {
            BoardSlot slot = GetSlot(1, col);
            if (slot == null || !slot.HasEnemy()) continue;

            if (slot.enemy.arrivedInEnemyRowTurn == currentTurn)
            {
                Debug.Log($"敌人 {slot.enemy.data.enemyName} 本回合刚推进到位，暂不攻击");
                continue;
            }

            units.Add(new CombatUnit(slot.enemy, slot, false, 1, col));
        }

        // 3. 排序规则：1. 攻击力（高到低） 2. 生命值（高到低） 3. 玩家优先
        units.Sort((a, b) =>
        {
            int attackCompare = b.attack.CompareTo(a.attack);
            if (attackCompare != 0) return attackCompare;

            int aHealth = a.isPlayer ? (a.isSymbioticPair ? a.slot.GetCombinedHealth() : a.card.currentHealth) : a.enemy.currentHealth;
            int bHealth = b.isPlayer ? (b.isSymbioticPair ? b.slot.GetCombinedHealth() : b.card.currentHealth) : b.enemy.currentHealth;

            int healthCompare = bHealth.CompareTo(aHealth);
            if (healthCompare != 0) return healthCompare;

            if (a.isPlayer && !b.isPlayer) return -1;
            if (!a.isPlayer && b.isPlayer) return 1;
            return 0;
        });

        // 4. 依次执行攻击
        foreach (var unit in units)
        {
            // 检查单位是否已阵亡（可能在之前的攻击中被反击或打死了）
            if (unit.isPlayer)
            {
                if (unit.isSymbioticPair) { if (unit.slot == null || !unit.slot.HasPlayerCard()) continue; }
                else if (unit.card == null || unit.card.currentHealth <= 0) continue;
            }
            else if (unit.enemy == null || unit.enemy.IsDead) continue;

            // 找同列目标
            BoardSlot targetSlot = unit.isPlayer ? GetSlot(1, unit.col) : GetSlot(2, unit.col);
            if (targetSlot == null) continue;

            // ==========================================
            // 己方攻击逻辑
            // ==========================================
            if (unit.isPlayer)
            {
                if (!targetSlot.HasEnemy()) continue; // 同列没有敌人，跳过

                EnemyInstance target = targetSlot.enemy;

                // 本回合刚推进到敌人区的敌人处于「入场」状态：我方卡牌也不能攻击它
                if (target.arrivedInEnemyRowTurn == currentTurn)
                {
                    Debug.Log($"敌人 {target.data.enemyName} 本回合刚推进到位，我方卡牌无法攻击");
                    continue;
                }

                // 播放己方冲撞动画
                OnUnitAttacked?.Invoke(unit.slot, true);
                yield return new WaitForSeconds(0.15f);

                target.currentHealth -= unit.attack;
                Debug.Log($"{unit.name} 攻击 {target.data.enemyName}，造成 {unit.attack} 伤害，敌人剩余 {target.currentHealth}");

                if (target.IsDead)
                {
                    Debug.Log($"敌人 {target.data.enemyName} 死亡，移除");
                    OnEnemyDied?.Invoke(target);
                    targetSlot.enemy = null;
                }
            }
            // ==========================================
            // 敌人攻击逻辑
            // ==========================================
            else
            {
                // 情况 A：前方没有己方卡牌，直接攻击玩家
                if (!targetSlot.HasPlayerCard())
                {
                    Debug.Log($"{unit.name} 前方没有阻挡，直接攻击玩家！");

                    // 播放敌人冲撞动画
                    OnUnitAttacked?.Invoke(unit.slot, false);
                    yield return new WaitForSeconds(0.15f);

                    // 扣除玩家血量
                    UpdatePlayerHealth(-unit.attack);
                }
                // 情况 B：前方有己方卡牌，正常打牌
                else
                {
                    // 播放敌人冲撞卡牌的动画
                    OnUnitAttacked?.Invoke(unit.slot, false);
                    yield return new WaitForSeconds(0.15f);

                    if (targetSlot.HasSymbioticPair)
                    {
                        // 打共生体
                        targetSlot.ApplyDamageToSymbioticPair(unit.attack);
                        Debug.Log($"{unit.name} 攻击共生体，造成 {unit.attack} 伤害（共享生命池）");

                        if (targetSlot.plantCard != null && targetSlot.plantCard.currentHealth <= 0)
                        {
                            SendToGraveyard(targetSlot.plantCard);
                            targetSlot.plantCard = null;                        // 先清数据
                            OnBoardCardRemoved?.Invoke(targetSlot, Faction.Plant); // 再通知视觉重建
                        }
                        if (targetSlot.bugCard != null && targetSlot.bugCard.currentHealth <= 0)
                        {
                            SendToGraveyard(targetSlot.bugCard);
                            targetSlot.bugCard = null;                          // 先清数据
                            OnBoardCardRemoved?.Invoke(targetSlot, Faction.Bug);  // 再通知视觉重建
                        }
                    }
                    else
                    {
                        // 打普通单卡
                        CardInstance targetCard = targetSlot.plantCard;
                        if (targetCard == null || targetCard.data == null) targetCard = targetSlot.bugCard;
                        if (targetCard == null || targetCard.data == null) targetCard = targetSlot.symbiosisCard;
                        if (targetCard == null || targetCard.data == null) continue;

                        targetCard.currentHealth -= unit.attack;
                        Debug.Log($"{unit.name} 攻击 {targetCard.data.cardName}，造成 {unit.attack} 伤害，卡牌剩余 {targetCard.currentHealth}");

                        if (targetCard.currentHealth <= 0)
                        {
                            SendToGraveyard(targetCard);
                            targetSlot.RemovePlayerCard(targetCard);              // 先清数据
                            OnBoardCardRemoved?.Invoke(targetSlot, targetCard.Faction); // 再通知视觉重建
                        }
                    }
                }
            }

            // 每个单位行动完后，等待一段时间，再轮到下一个单位
            yield return new WaitForSeconds(0.4f);
        }

        // 战斗结束，清理所有死卡
        CleanupDeadPlayerCards();
    }

    private void CleanupDeadPlayerCards()
    {
        foreach (var slot in boardSlots)
        {
            if (slot.plantCard != null && slot.plantCard.currentHealth <= 0)
            {
                SendToGraveyard(slot.plantCard);
                slot.plantCard = null;                        // 先清数据
                OnBoardCardRemoved?.Invoke(slot, Faction.Plant);   // 再通知视觉重建
            }
            if (slot.bugCard != null && slot.bugCard.currentHealth <= 0)
            {
                SendToGraveyard(slot.bugCard);
                slot.bugCard = null;                          // 先清数据
                OnBoardCardRemoved?.Invoke(slot, Faction.Bug);     // 再通知视觉重建
            }
            if (slot.symbiosisCard != null && slot.symbiosisCard.currentHealth <= 0)
            {
                SendToGraveyard(slot.symbiosisCard);
                slot.symbiosisCard = null;                    // 先清数据
                OnBoardCardRemoved?.Invoke(slot, Faction.Symbiosis); // 再通知视觉重建
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
        OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);   // 出牌消耗能量后刷新显示
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
                // 记录到达回合：本回合的战斗阶段该敌人不会攻击，下一回合起才会攻击
                moved.arrivedInEnemyRowTurn = currentTurn;
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