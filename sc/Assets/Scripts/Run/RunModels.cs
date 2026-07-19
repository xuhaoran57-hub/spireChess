using System;
using System.Collections.Generic;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Shop;

namespace SpireChess.Run
{
    public enum RunPhase
    {
        MapSelection,
        EnteringNode,
        Shop,
        Battle,
        BattleResult,
        RewardChoice,
        EventChoice,
        EnhanceChoice,
        RestChoice,
        FloorComplete,
        RunWon,
        RunLost
    }

    public enum RunNodeType
    {
        Shop,
        Normal,
        Elite,
        Enhance,
        Event,
        Rest,
        Boss
    }

    public enum RunNodeStatus
    {
        Locked,
        Reachable,
        Current,
        Resolved
    }

    public enum RunOperationError
    {
        None,
        InvalidPhase,
        InvalidNode,
        NodeNotReachable,
        MissingContent,
        PendingCardRewards,
        NoPendingCardReward,
        BenchFull,
        AttemptMismatch,
        AlreadyCommitted,
        InvalidTiming,
        InvalidChoice,
        InvalidTarget,
        NoBenefit,
        InsufficientPool,
        ChoiceAlreadyResolved
    }

    public sealed class RunOperationResult
    {
        private RunOperationResult(bool success, RunOperationError error, string message)
        {
            Success = success;
            Error = error;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public RunOperationError Error { get; }
        public string Message { get; }

        public static RunOperationResult Succeed(string message = null)
        {
            return new RunOperationResult(true, RunOperationError.None, message);
        }

        public static RunOperationResult Fail(RunOperationError error, string message = null)
        {
            return new RunOperationResult(false, error, message);
        }
    }

    public sealed class NodeAttemptState
    {
        public NodeAttemptState(string attemptId, string nodeId, string encounterId, int runTurn)
            : this(attemptId, nodeId, RunNodeType.Normal, encounterId, runTurn)
        {
        }

        public NodeAttemptState(
            string attemptId,
            string nodeId,
            RunNodeType nodeType,
            string contentId,
            int runTurn)
        {
            NodeAttemptId = attemptId ?? throw new ArgumentNullException(nameof(attemptId));
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            NodeType = nodeType;
            ContentId = contentId;
            RunTurn = runTurn;
            RunTurnCommitted = true;
        }

        public string NodeAttemptId { get; }
        public string NodeId { get; }
        public RunNodeType NodeType { get; }
        public string ContentId { get; internal set; }
        public string EncounterId =>
            NodeType == RunNodeType.Normal ||
            NodeType == RunNodeType.Elite ||
            NodeType == RunNodeType.Boss
                ? ContentId
                : null;
        public int RunTurn { get; }
        public bool RunTurnCommitted { get; internal set; }
        public bool EconomyTurnCommitted { get; internal set; }
        public bool ContentGenerated { get; internal set; }
        public bool ChoiceCommitted { get; internal set; }
        public bool EffectApplied { get; internal set; }
        public bool BattleSettled { get; internal set; }
        public bool HealthDamageApplied { get; internal set; }
        public bool RewardGenerated { get; internal set; }
        public bool NodeResolved { get; internal set; }
    }

    public sealed class DelayedShopResources
    {
        public int GoldBonus { get; internal set; }
        public int FreeRefreshes { get; internal set; }
        public int UpgradeDiscount { get; internal set; }
        public int LastAppliedRunTurn { get; internal set; }

        internal void Clear()
        {
            GoldBonus = 0;
            FreeRefreshes = 0;
            UpgradeDiscount = 0;
        }
    }

    public sealed class PendingCardReward
    {
        public PendingCardReward(
            string rewardInstanceId,
            ShopCardType cardType,
            string configId,
            int reservedPoolCopies = 0)
        {
            RewardInstanceId = rewardInstanceId ??
                throw new ArgumentNullException(nameof(rewardInstanceId));
            CardType = cardType;
            ConfigId = configId ?? throw new ArgumentNullException(nameof(configId));
            ReservedPoolCopies = Math.Max(0, reservedPoolCopies);
        }

        public string RewardInstanceId { get; }
        public ShopCardType CardType { get; }
        public string ConfigId { get; }
        public int ReservedPoolCopies { get; }
    }

    public enum RewardCompletionMode
    {
        ReturnToBattleResult,
        ResolveNodeToMap,
        FloorComplete
    }

    public sealed class RewardCandidate
    {
        public RewardCandidate(
            string candidateId,
            string category,
            string type,
            int amount = 0,
            string cardId = null,
            int reservedPoolCopies = 0,
            int attack = 0,
            int health = 0,
            string displayText = null)
        {
            CandidateId = candidateId;
            Category = category;
            Type = type;
            Amount = amount;
            CardId = cardId;
            ReservedPoolCopies = reservedPoolCopies;
            Attack = attack;
            Health = health;
            DisplayText = displayText ?? type;
        }

        public string CandidateId { get; }
        public string Category { get; }
        public string Type { get; }
        public int Amount { get; }
        public string CardId { get; }
        public int ReservedPoolCopies { get; }
        public int Attack { get; }
        public int Health { get; }
        public string DisplayText { get; }
        public bool RequiresOwnedMinionTarget => Type == "PermanentStats";
    }

    public sealed class PendingRewardChoice
    {
        public PendingRewardChoice(
            string choiceId,
            string sourceAttemptId,
            RewardCompletionMode completionMode,
            IEnumerable<RewardCandidate> candidates,
            bool allowSkip)
        {
            ChoiceId = choiceId;
            SourceAttemptId = sourceAttemptId;
            CompletionMode = completionMode;
            Candidates = new List<RewardCandidate>(candidates).AsReadOnly();
            AllowSkip = allowSkip;
        }

        public string ChoiceId { get; }
        public string SourceAttemptId { get; }
        public RewardCompletionMode CompletionMode { get; }
        public IReadOnlyList<RewardCandidate> Candidates { get; }
        public bool AllowSkip { get; }
    }

    public sealed class PendingEventChoice
    {
        public PendingEventChoice(string sourceAttemptId, EventConfig config)
        {
            SourceAttemptId = sourceAttemptId;
            Config = config;
        }

        public string SourceAttemptId { get; }
        public EventConfig Config { get; }
    }

    public sealed class PendingEnhanceChoice
    {
        public PendingEnhanceChoice(
            string sourceAttemptId,
            EnhanceNodeConfig nodeConfig,
            IEnumerable<EnhancementRecipeConfig> recipes)
        {
            SourceAttemptId = sourceAttemptId;
            NodeConfig = nodeConfig;
            Recipes = new List<EnhancementRecipeConfig>(recipes).AsReadOnly();
        }

        public string SourceAttemptId { get; }
        public EnhanceNodeConfig NodeConfig { get; }
        public IReadOnlyList<EnhancementRecipeConfig> Recipes { get; }
    }

    public sealed class PendingRestChoice
    {
        public PendingRestChoice(string sourceAttemptId, RestNodeConfig config)
        {
            SourceAttemptId = sourceAttemptId;
            Config = config;
        }

        public string SourceAttemptId { get; }
        public RestNodeConfig Config { get; }
    }

    public sealed class BattleSettlementResult
    {
        public BattleSettlementResult(
            bool playerWon,
            int damage,
            int survivingEnemies,
            int highestEnemyTier,
            int nodeDamageBonus,
            BattleOutcomeReason outcomeReason)
        {
            PlayerWon = playerWon;
            Damage = damage;
            SurvivingEnemies = survivingEnemies;
            HighestEnemyTier = highestEnemyTier;
            NodeDamageBonus = nodeDamageBonus;
            OutcomeReason = outcomeReason;
        }

        public bool PlayerWon { get; }
        public int Damage { get; }
        public int SurvivingEnemies { get; }
        public int HighestEnemyTier { get; }
        public int NodeDamageBonus { get; }
        public BattleOutcomeReason OutcomeReason { get; }

        public string BuildDamageText()
        {
            if (Damage <= 0)
            {
                return "未受到伤害";
            }

            if (OutcomeReason != BattleOutcomeReason.Victory)
            {
                return $"平局伤害：{Damage}";
            }

            return $"伤害 {Damage} = 存活 {SurvivingEnemies} + 最高等级 {HighestEnemyTier} + 修正 {NodeDamageBonus}";
        }
    }

    public sealed class RunState
    {
        private readonly List<PendingCardReward> pendingCardRewards =
            new List<PendingCardReward>();

        internal RunState(int seed, MapDefinition map)
        {
            Seed = seed;
            Floor = map?.Floor ?? 1;
            ShopTurn = 0;
            MapStep = 0;
            Health = 20;
            MaxHealth = 20;
            Phase = RunPhase.MapSelection;
            CurrentMap = map;
            MapProgress = map == null ? null : new MapProgressState(map);
            DelayedShopResources = new DelayedShopResources();
            Statistics = new RunStatistics();
        }

        public int Seed { get; }
        public int Floor { get; internal set; }
        public int ShopTurn { get; internal set; }
        public int RunTurn
        {
            get => ShopTurn;
            internal set => ShopTurn = value;
        }
        public int MapStep { get; internal set; }
        public int Health { get; internal set; }
        public int MaxHealth { get; internal set; }
        public RunPhase Phase { get; internal set; }
        public MapDefinition CurrentMap { get; internal set; }
        public MapProgressState MapProgress { get; internal set; }
        public string CurrentNodeId { get; internal set; }
        public NodeAttemptState CurrentAttempt { get; internal set; }
        public BattleSettlementResult LastSettlement { get; internal set; }
        public string LastRewardSummary { get; internal set; }
        public DelayedShopResources DelayedShopResources { get; }
        public IReadOnlyList<PendingCardReward> PendingCardRewards => pendingCardRewards;
        public PendingRewardChoice PendingRewardChoice { get; internal set; }
        public PendingEventChoice PendingEventChoice { get; internal set; }
        public PendingEnhanceChoice PendingEnhanceChoice { get; internal set; }
        public PendingRestChoice PendingRestChoice { get; internal set; }
        public RunStatistics Statistics { get; }

        internal void EnqueueCardReward(PendingCardReward reward)
        {
            pendingCardRewards.Add(reward ?? throw new ArgumentNullException(nameof(reward)));
        }

        internal PendingCardReward PeekCardReward()
        {
            return pendingCardRewards.Count == 0 ? null : pendingCardRewards[0];
        }

        internal PendingCardReward DequeueCardReward()
        {
            if (pendingCardRewards.Count == 0)
            {
                return null;
            }

            var reward = pendingCardRewards[0];
            pendingCardRewards.RemoveAt(0);
            return reward;
        }
    }

    public sealed class RunStatistics
    {
        internal RunStatistics()
        {
            StartedAtUtc = DateTime.UtcNow;
        }

        public DateTime StartedAtUtc { get; }
        public DateTime? CompletedAtUtc { get; internal set; }
        public int BattlesWon { get; internal set; }
        public int BattlesNotWon { get; internal set; }
        public int ElitesAttempted { get; internal set; }
        public int ElitesDefeated { get; internal set; }
        public int BossAttempts { get; internal set; }
        public int BossesDefeated { get; internal set; }
        public int TriplesFormed { get; internal set; }
        public int RefreshesPaid { get; internal set; }
        public int RefreshesFree { get; internal set; }
        public int MinionsBought { get; internal set; }
        public int MinionsSold { get; internal set; }
        public int SpellsUsed { get; internal set; }
        public int TavernUpgrades { get; internal set; }
        public int GoldWasted { get; internal set; }
        public int TargetedDiscoversUsed { get; internal set; }
        public int? FirstCoreTurn { get; internal set; }
        public int? SecondCoreTurn { get; internal set; }
        public TimeSpan Elapsed => (CompletedAtUtc ?? DateTime.UtcNow) - StartedAtUtc;

        internal void Complete()
        {
            if (!CompletedAtUtc.HasValue)
            {
                CompletedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
