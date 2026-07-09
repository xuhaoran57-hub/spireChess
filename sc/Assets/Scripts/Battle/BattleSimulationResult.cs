using System.Collections.Generic;

namespace SpireChess.Battle
{
    public sealed class BattleSimulationResult
    {
        public BattleSimulationResult(BattleBoardState finalState, BattleSide? winner, List<string> log)
            : this(finalState, winner, log, new List<BattleStep>())
        {
        }

        public BattleSimulationResult(
            BattleBoardState finalState,
            BattleSide? winner,
            List<string> log,
            List<BattleStep> steps)
        {
            FinalState = finalState;
            Winner = winner;
            Log = log;
            Steps = steps;
        }

        public BattleBoardState FinalState { get; }
        public BattleSide? Winner { get; }
        public IReadOnlyList<string> Log { get; }
        public IReadOnlyList<BattleStep> Steps { get; }
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
            BattleSide? winner = null)
        {
            BoardState = boardState;
            Messages = new List<string>(messages);
            AttackerSide = attackerSide;
            AttackerIndex = attackerIndex;
            TargetSide = targetSide;
            TargetIndex = targetIndex;
            Winner = winner;
        }

        public BattleBoardState BoardState { get; }
        public IReadOnlyList<string> Messages { get; }
        public BattleSide? AttackerSide { get; }
        public int AttackerIndex { get; }
        public BattleSide? TargetSide { get; }
        public int TargetIndex { get; }
        public BattleSide? Winner { get; }
        public bool HasAttack => AttackerSide.HasValue && TargetSide.HasValue;
    }
}
