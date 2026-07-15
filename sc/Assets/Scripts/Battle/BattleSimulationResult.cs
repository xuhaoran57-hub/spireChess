using System.Collections.Generic;
using System.Linq;

namespace SpireChess.Battle
{
    public enum BattleOutcomeReason
    {
        Victory,
        MutualElimination,
        RoundLimit
    }

    public sealed class BattleSideDiagnostics
    {
        public int OpeningRawDamage { get; internal set; }
        public int OpeningEffectiveDamage { get; internal set; }
        public int RoundOneRawDamage { get; internal set; }
        public int RoundOneEffectiveDamage { get; internal set; }
        public int NormalAttacks { get; internal set; }
        public int ImmediateAttacks { get; internal set; }
        public int CleaveHits { get; internal set; }
        public int SummonAttempts { get; internal set; }
        public int SummonSuccesses { get; internal set; }
        public int SummonFailures { get; internal set; }
        public int TokenDeaths { get; internal set; }
        public int NonTokenDeaths { get; internal set; }
        public int ShieldsGranted { get; internal set; }
        public int ShieldsLost { get; internal set; }
        public int ShieldDamageBlocks { get; internal set; }
        public int FurnaceTransfers { get; internal set; }
        public int ShieldBenefitTriggers { get; internal set; }
        public int NonTokenDeathBenefitTriggers { get; internal set; }
        public int TemporaryAttackGained { get; internal set; }
        public int TemporaryHealthGained { get; internal set; }
        public int PermanentAttackDelta { get; internal set; }
        public int PermanentHealthDelta { get; internal set; }
        public int FlourishGained { get; internal set; }
    }

    public sealed class BattleDiagnostics
    {
        public BattleSideDiagnostics Player { get; } = new BattleSideDiagnostics();
        public BattleSideDiagnostics Enemy { get; } = new BattleSideDiagnostics();
        public int RoundCount { get; internal set; }
        public int ProcessedEffectCount { get; internal set; }
        public bool HitEffectLimit { get; internal set; }

        internal BattleSideDiagnostics For(BattleSide side)
        {
            return side == BattleSide.Player ? Player : Enemy;
        }
    }

    public sealed class BattlePostCombatRewardRequest
    {
        public BattlePostCombatRewardRequest(BattleSide side, string race, int count)
        {
            Side = side;
            Race = race;
            Count = count;
        }

        public BattleSide Side { get; }
        public string Race { get; }
        public int Count { get; }
    }

    public sealed class BattleSimulationResult
    {
        public BattleSimulationResult(BattleBoardState finalState, BattleSide? winner, List<string> log)
            : this(
                finalState,
                winner,
                winner.HasValue
                    ? BattleOutcomeReason.Victory
                    : BattleOutcomeReason.MutualElimination,
                log,
                new List<BattleStep>(),
                null)
        {
        }

        public BattleSimulationResult(
            BattleBoardState finalState,
            BattleSide? winner,
            List<string> log,
            List<BattleStep> steps)
            : this(
                finalState,
                winner,
                winner.HasValue
                    ? BattleOutcomeReason.Victory
                    : BattleOutcomeReason.MutualElimination,
                log,
                steps,
                null)
        {
        }

        public BattleSimulationResult(
            BattleBoardState finalState,
            BattleSide? winner,
            BattleOutcomeReason outcomeReason,
            List<string> log,
            List<BattleStep> steps,
            IEnumerable<BattlePermanentDelta> permanentDeltas = null,
            BattleDiagnostics diagnostics = null,
            IEnumerable<BattlePostCombatRewardRequest> postCombatRewardRequests = null)
        {
            FinalState = finalState;
            Winner = winner;
            OutcomeReason = outcomeReason;
            Log = log;
            Steps = steps;
            PermanentDeltas = (permanentDeltas ?? Enumerable.Empty<BattlePermanentDelta>())
                .ToList().AsReadOnly();
            Diagnostics = diagnostics ?? new BattleDiagnostics();
            PostCombatRewardRequests = (postCombatRewardRequests ??
                    Enumerable.Empty<BattlePostCombatRewardRequest>())
                .ToList().AsReadOnly();
        }

        public BattleBoardState FinalState { get; }
        public BattleSide? Winner { get; }
        public BattleOutcomeReason OutcomeReason { get; }
        public IReadOnlyList<string> Log { get; }
        public IReadOnlyList<BattleStep> Steps { get; }
        public IReadOnlyList<BattlePermanentDelta> PermanentDeltas { get; }
        public BattleDiagnostics Diagnostics { get; }
        public IReadOnlyList<BattlePostCombatRewardRequest> PostCombatRewardRequests { get; }
    }

    public sealed class BattleStep
    {
        public BattleStep(
            BattleBoardState boardState,
            IEnumerable<string> messages,
            BattleSide? attackerSide = null,
            int attackerIndex = -1,
            BattleSide? targetSide = null,
            int targetIndex = -1,
            IEnumerable<int> splashTargetIndexes = null,
            BattleSide? winner = null)
        {
            BoardState = boardState;
            Messages = new List<string>(messages);
            AttackerSide = attackerSide;
            AttackerIndex = attackerIndex;
            TargetSide = targetSide;
            TargetIndex = targetIndex;
            SplashTargetIndexes = new List<int>(splashTargetIndexes ?? new int[0]);
            Winner = winner;
        }

        public BattleBoardState BoardState { get; }
        public IReadOnlyList<string> Messages { get; }
        public BattleSide? AttackerSide { get; }
        public int AttackerIndex { get; }
        public BattleSide? TargetSide { get; }
        public int TargetIndex { get; }
        public IReadOnlyList<int> SplashTargetIndexes { get; }
        public BattleSide? Winner { get; }
        public bool HasAttack => AttackerSide.HasValue && TargetSide.HasValue;
    }
}
