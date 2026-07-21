using System;
using System.Collections.Generic;
using SpireChess.App;
using SpireChess.Battle;
using SpireChess.Run;
using SpireChess.Shop;

namespace SpireChess.Save
{
    public sealed class RunSaveDocumentV1
    {
        public const string FormatId = "spire-chess-run";
        public const int CurrentSchemaVersion = 1;

        public string Format { get; set; } = FormatId;
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string ContentVersion { get; set; }
        public string RulesVersion { get; set; }
        public string ConfigHash { get; set; }
        public string AppVersion { get; set; }
        public string GitCommit { get; set; }
        public string UnityVersion { get; set; }
        public DateTime SavedAtUtc { get; set; }
        public long Revision { get; set; }
        public RunSaveSummaryV1 Summary { get; set; }
        public RunSavePayloadV1 Payload { get; set; }
        public string PayloadSha256 { get; set; }
    }

    public sealed class RunSaveSummaryV1
    {
        public int Floor { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int ShopTurn { get; set; }
        public RunPhase Phase { get; set; }
    }

    public sealed class RunSavePayloadV1
    {
        public RunStateSnapshotV1 RunState { get; set; }
        public ShopSessionSnapshotV1 ShopSession { get; set; }
        public RandomStreamsSnapshotV1 RandomStreams { get; set; }
        public BattleContextSnapshotV1 PendingBattle { get; set; }
        public BattleContextSnapshotV1 LastBattleContext { get; set; }
        public BattleResultSnapshotV1 LastBattleResult { get; set; }
        public RunSequenceSnapshotV1 Sequences { get; set; }
        public CoreActivationEvidenceSnapshotV1 CoreEvidence { get; set; }
        public bool TurnTenSnapshotRecorded { get; set; }
        public bool RunEndedRecorded { get; set; }
    }

    public sealed class RunStateSnapshotV1
    {
        public int Seed { get; set; }
        public int Floor { get; set; }
        public int ShopTurn { get; set; }
        public int MapStep { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public RunPhase Phase { get; set; }
        public string MapId { get; set; }
        public Dictionary<string, RunNodeStatus> NodeStatuses { get; set; } =
            new Dictionary<string, RunNodeStatus>();
        public string CurrentNodeId { get; set; }
        public NodeAttemptSnapshotV1 CurrentAttempt { get; set; }
        public BattleSettlementSnapshotV1 LastSettlement { get; set; }
        public string LastRewardSummary { get; set; }
        public DelayedShopResourcesSnapshotV1 DelayedShopResources { get; set; }
        public List<PendingCardRewardSnapshotV1> PendingCardRewards { get; set; } =
            new List<PendingCardRewardSnapshotV1>();
        public PendingRewardChoiceSnapshotV1 PendingRewardChoice { get; set; }
        public PendingRelicChoiceSnapshotV1 PendingRelicChoice { get; set; }
        public PendingEventChoiceSnapshotV1 PendingEventChoice { get; set; }
        public PendingEnhanceChoiceSnapshotV1 PendingEnhanceChoice { get; set; }
        public PendingRestChoiceSnapshotV1 PendingRestChoice { get; set; }
        public List<OwnedRelicSnapshotV1> OwnedRelics { get; set; } =
            new List<OwnedRelicSnapshotV1>();
        public RunStatisticsSnapshotV1 Statistics { get; set; }
    }

    public sealed class NodeAttemptSnapshotV1
    {
        public string NodeAttemptId { get; set; }
        public string NodeId { get; set; }
        public RunNodeType NodeType { get; set; }
        public string ContentId { get; set; }
        public string EncounterId { get; set; }
        public int RunTurn { get; set; }
        public bool RunTurnCommitted { get; set; }
        public bool EconomyTurnCommitted { get; set; }
        public bool ContentGenerated { get; set; }
        public bool ChoiceCommitted { get; set; }
        public bool EffectApplied { get; set; }
        public bool BattleSettled { get; set; }
        public bool HealthDamageApplied { get; set; }
        public bool RewardGenerated { get; set; }
        public bool RelicGenerated { get; set; }
        public bool RelicVictoryEffectsApplied { get; set; }
        public bool NodeResolved { get; set; }
    }

    public sealed class BattleSettlementSnapshotV1
    {
        public bool PlayerWon { get; set; }
        public int Damage { get; set; }
        public int SurvivingEnemies { get; set; }
        public int HighestEnemyTier { get; set; }
        public int NodeDamageBonus { get; set; }
        public BattleOutcomeReason OutcomeReason { get; set; }
    }

    public sealed class DelayedShopResourcesSnapshotV1
    {
        public int GoldBonus { get; set; }
        public int FreeRefreshes { get; set; }
        public int UpgradeDiscount { get; set; }
        public int LastAppliedRunTurn { get; set; }
    }

    public sealed class PendingCardRewardSnapshotV1
    {
        public string RewardInstanceId { get; set; }
        public ShopCardType CardType { get; set; }
        public string ConfigId { get; set; }
        public int ReservedPoolCopies { get; set; }
    }

    public sealed class RewardCandidateSnapshotV1
    {
        public string CandidateId { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public int Amount { get; set; }
        public string CardId { get; set; }
        public int ReservedPoolCopies { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public string DisplayText { get; set; }
    }

    public sealed class PendingRewardChoiceSnapshotV1
    {
        public string ChoiceId { get; set; }
        public string SourceAttemptId { get; set; }
        public RewardCompletionMode CompletionMode { get; set; }
        public List<RewardCandidateSnapshotV1> Candidates { get; set; } =
            new List<RewardCandidateSnapshotV1>();
        public bool AllowSkip { get; set; }
    }

    public sealed class RelicCandidateSnapshotV1
    {
        public string CandidateId { get; set; }
        public string RelicId { get; set; }
    }

    public sealed class PendingRelicChoiceSnapshotV1
    {
        public string ChoiceId { get; set; }
        public string SourceAttemptId { get; set; }
        public string Grade { get; set; }
        public RelicCompletionMode CompletionMode { get; set; }
        public List<RelicCandidateSnapshotV1> Candidates { get; set; } =
            new List<RelicCandidateSnapshotV1>();
        public int HealthCost { get; set; }
        public bool AllowSkip { get; set; }
    }

    public sealed class PendingEventChoiceSnapshotV1
    {
        public string SourceAttemptId { get; set; }
        public string EventId { get; set; }
    }

    public sealed class PendingEnhanceChoiceSnapshotV1
    {
        public string SourceAttemptId { get; set; }
        public string NodeConfigId { get; set; }
        public List<string> RecipeIds { get; set; } = new List<string>();
    }

    public sealed class PendingRestChoiceSnapshotV1
    {
        public string SourceAttemptId { get; set; }
        public string RestNodeId { get; set; }
    }

    public sealed class OwnedRelicSnapshotV1
    {
        public string RelicId { get; set; }
        public string SourceType { get; set; }
        public string SourceId { get; set; }
        public int AcquiredFloor { get; set; }
        public int AcquiredShopTurn { get; set; }
        public int ShopProgress { get; set; }
        public int LastResolvedShopTurn { get; set; }
        public int ActivationCount { get; set; }
    }

    public sealed class RunStatisticsSnapshotV1
    {
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public int BattlesWon { get; set; }
        public int BattlesNotWon { get; set; }
        public int ElitesAttempted { get; set; }
        public int ElitesDefeated { get; set; }
        public int BossAttempts { get; set; }
        public int BossesDefeated { get; set; }
        public int TriplesFormed { get; set; }
        public int RefreshesPaid { get; set; }
        public int RefreshesFree { get; set; }
        public int MinionsBought { get; set; }
        public int MinionsSold { get; set; }
        public int SpellsUsed { get; set; }
        public int TavernUpgrades { get; set; }
        public int GoldWasted { get; set; }
        public int TargetedDiscoversUsed { get; set; }
        public int? FirstCoreTurn { get; set; }
        public int? SecondCoreTurn { get; set; }
    }

    public sealed class ShopSessionSnapshotV1
    {
        public int Round { get; set; }
        public int Gold { get; set; }
        public int TavernTier { get; set; }
        public int RefreshCount { get; set; }
        public int FreeRefreshes { get; set; }
        public bool IsShopOpen { get; set; }
        public bool IsFrozen { get; set; }
        public bool UpgradedThisRound { get; set; }
        public List<string> MinionOfferIds { get; set; } = new List<string>();
        public string SpellOfferId { get; set; }
        public Dictionary<string, int> MinionPoolRemainingCopies { get; set; } =
            new Dictionary<string, int>();
        public List<ShopCardSnapshotV1> Bench { get; set; } =
            new List<ShopCardSnapshotV1>();
        public List<ShopCardSnapshotV1> Battle { get; set; } =
            new List<ShopCardSnapshotV1>();
        public ShopDiscoverSnapshotV1 PendingDiscover { get; set; }
        public PendingEffectChoiceSnapshotV1 PendingChoice { get; set; }
        public List<ActiveShopEffectSnapshotV1> ActiveShopEffects { get; set; } =
            new List<ActiveShopEffectSnapshotV1>();
        public Dictionary<string, int> PerShopEffectUsage { get; set; } =
            new Dictionary<string, int>();
        public Dictionary<string, ValueConfigSnapshotV1> PendingPostCombatBuffs { get; set; } =
            new Dictionary<string, ValueConfigSnapshotV1>();
        public List<EffectReferenceSnapshotV1> PendingBattleStartEffects { get; set; } =
            new List<EffectReferenceSnapshotV1>();
        public ShopPhaseStatsSnapshotV1 PhaseStats { get; set; }
        public int FlourishStacks { get; set; }
        public int CardInstanceSequence { get; set; }
        public int RoundsWithoutUpgradeAtCurrentTier { get; set; }
        public int PendingUpgradeDiscount { get; set; }
        public int ScheduledGold { get; set; }
        public ShopRuleModifiersSnapshotV1 RuleModifiers { get; set; }
        public bool FirstPurchaseFreeAvailable { get; set; }
        public bool FirstPaidRefreshFreeAvailable { get; set; }
        public bool FirstMinionSaleBonusAvailable { get; set; }
    }

    public sealed class ShopCardSnapshotV1
    {
        public string InstanceId { get; set; }
        public ShopCardType CardType { get; set; }
        public string ConfigId { get; set; }
        public bool IsGolden { get; set; }
        public int PermanentAttackBonus { get; set; }
        public int PermanentHealthBonus { get; set; }
        public int FlourishAttackBonus { get; set; }
        public List<string> PermanentKeywords { get; set; } = new List<string>();
        public bool TripleDiscoveryPending { get; set; }
        public bool ExpiresAtShopEnd { get; set; }
        public List<PendingCombatModifierSnapshotV1> PendingCombatModifiers { get; set; } =
            new List<PendingCombatModifierSnapshotV1>();
    }

    public sealed class PendingCombatModifierSnapshotV1
    {
        public string EffectId { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public string Keyword { get; set; }
        public bool AddShield { get; set; }
    }

    public sealed class ShopDiscoverSnapshotV1
    {
        public string SourceInstanceId { get; set; }
        public int BenchIndex { get; set; }
        public List<string> CandidateIds { get; set; } = new List<string>();
        public bool CanCancel { get; set; }
    }

    public sealed class PendingEffectChoiceSnapshotV1
    {
        public EffectChoiceType ChoiceType { get; set; }
        public string SourceInstanceId { get; set; }
        public int BenchIndex { get; set; }
        public EffectReferenceSnapshotV1 Effect { get; set; }
        public List<EffectChoiceCandidateSnapshotV1> Candidates { get; set; } =
            new List<EffectChoiceCandidateSnapshotV1>();
        public bool ReplaceSourceCard { get; set; }
        public int RemainingChoices { get; set; }
        public int TotalChoices { get; set; }
    }

    public sealed class EffectChoiceCandidateSnapshotV1
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string MinionId { get; set; }
        public string SpellId { get; set; }
        public string TargetInstanceId { get; set; }
    }

    public sealed class ActiveShopEffectSnapshotV1
    {
        public string SourceInstanceId { get; set; }
        public string SourceConfigId { get; set; }
        public EffectReferenceSnapshotV1 Effect { get; set; }
        public int ActivationRefreshCount { get; set; }
        public int TriggerCount { get; set; }
    }

    public sealed class EffectReferenceSnapshotV1
    {
        public string OwnerConfigId { get; set; }
        public string EffectId { get; set; }
    }

    public sealed class ValueConfigSnapshotV1
    {
        public int Attack { get; set; }
        public int Health { get; set; }
        public int Amount { get; set; }
        public string Duration { get; set; }
        public string Keyword { get; set; }
        public string Resource { get; set; }
        public string CardId { get; set; }
        public int Count { get; set; }
        public bool Temporary { get; set; }
        public int SummonEffectMultiplier { get; set; }
        public int PermanentAttack { get; set; }
        public int PermanentHealth { get; set; }
    }

    public sealed class ShopPhaseStatsSnapshotV1
    {
        public int RefreshCount { get; set; }
        public int SpellUsedCount { get; set; }
        public int SpellBoughtCount { get; set; }
        public int MinionBoughtCount { get; set; }
    }

    public sealed class ShopRuleModifiersSnapshotV1
    {
        public bool FirstPurchaseFree { get; set; }
        public bool FirstPaidRefreshFree { get; set; }
        public int FirstMinionSaleBonusGold { get; set; }
        public int ExtraBattlecryTriggers { get; set; }
    }

    public sealed class RandomStreamsSnapshotV1
    {
        public RandomStreamSnapshotV1 Shop { get; set; }
        public RandomStreamSnapshotV1 Reward { get; set; }
        public RandomStreamSnapshotV1 Event { get; set; }
        public RandomStreamSnapshotV1 Relic { get; set; }
    }

    public sealed class RandomStreamSnapshotV1
    {
        public int Seed { get; set; }
        public List<RecordedRandomEntry> Entries { get; set; } =
            new List<RecordedRandomEntry>();
    }

    public sealed class BattleContextSnapshotV1
    {
        public BattleBoardSnapshotV1 Board { get; set; }
        public string EncounterName { get; set; }
        public GameSceneId ReturnScene { get; set; }
        public string NodeAttemptId { get; set; }
        public string EncounterId { get; set; }
        public int? BattleSeed { get; set; }
    }

    public sealed class BattleResultSnapshotV1
    {
        public BattleBoardSnapshotV1 FinalBoard { get; set; }
        public BattleSide? Winner { get; set; }
        public BattleOutcomeReason OutcomeReason { get; set; }
        public List<string> Log { get; set; } = new List<string>();
    }

    public sealed class BattleBoardSnapshotV1
    {
        public List<BattleMinionSnapshotV1> Player { get; set; } =
            new List<BattleMinionSnapshotV1>();
        public List<BattleMinionSnapshotV1> Enemy { get; set; } =
            new List<BattleMinionSnapshotV1>();
        public List<BattleStartEffectSnapshotV1> BattleStartEffects { get; set; } =
            new List<BattleStartEffectSnapshotV1>();
        public BattleRuleModifiersSnapshotV1 RuleModifiers { get; set; }
        public int PlayerFlourishStacks { get; set; }
        public int EnemyFlourishStacks { get; set; }
    }

    public sealed class BattleMinionSnapshotV1
    {
        public string ConfigId { get; set; }
        public string SourceInstanceId { get; set; }
        public string RuntimeInstanceId { get; set; }
        public bool IsGolden { get; set; }
        public int CurrentAttack { get; set; }
        public int CurrentHealth { get; set; }
        public int CombatMaxHealth { get; set; }
        public int PermanentAttackBonus { get; set; }
        public int PermanentHealthBonus { get; set; }
        public bool HasShield { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public int SummonEffectMultiplier { get; set; }
    }

    public sealed class BattleStartEffectSnapshotV1
    {
        public BattleSide Side { get; set; }
        public EffectReferenceSnapshotV1 Effect { get; set; }
    }

    public sealed class BattleRuleModifiersSnapshotV1
    {
        public int PlayerExtraDeathrattleTriggers { get; set; }
        public int PlayerFirstNonTokenDeathSummonCount { get; set; }
        public string PlayerFirstNonTokenDeathTokenId { get; set; }
        public int PlayerFirstNonTokenDeathTokenAttack { get; set; }
        public int PlayerFirstNonTokenDeathTokenHealth { get; set; }
        public int PlayerBattleStartShieldTargets { get; set; }
        public int PlayerDistinctRaceStatBonus { get; set; }
    }

    public sealed class RunSequenceSnapshotV1
    {
        public int AttemptSequence { get; set; }
        public int RewardSequence { get; set; }
        public int ChoiceSequence { get; set; }
        public int RelicChoiceSequence { get; set; }
        public int RelicCandidateSequence { get; set; }
    }

    public sealed class CoreActivationEvidenceSnapshotV1
    {
        public int ShieldEvents { get; set; }
        public int ShieldBenefitEvents { get; set; }
        public int SummonSuccesses { get; set; }
        public int NonTokenDeathBenefitEvents { get; set; }
        public int SpellsUsed { get; set; }
        public int Refreshes { get; set; }
    }
}
