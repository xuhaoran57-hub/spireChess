using System;
using SpireChess.Battle;

namespace SpireChess.Run
{
    public sealed class BattleContext
    {
        public BattleContext(
            BattleBoardState boardState,
            string encounterName,
            string returnSceneName)
            : this(boardState, encounterName, returnSceneName, null, null, null)
        {
        }

        public BattleContext(
            BattleBoardState boardState,
            string encounterName,
            string returnSceneName,
            string nodeAttemptId,
            string encounterId,
            int? battleSeed = null)
        {
            BoardState = (boardState ?? throw new ArgumentNullException(nameof(boardState))).Clone();
            EncounterName = string.IsNullOrWhiteSpace(encounterName)
                ? "测试遭遇"
                : encounterName;
            ReturnSceneName = string.IsNullOrWhiteSpace(returnSceneName)
                ? "ShopTest"
                : returnSceneName;
            NodeAttemptId = nodeAttemptId;
            EncounterId = encounterId;
            BattleSeed = battleSeed;
        }

        public BattleBoardState BoardState { get; }
        public string EncounterName { get; }
        public string ReturnSceneName { get; }
        public string NodeAttemptId { get; }
        public string EncounterId { get; }
        public int? BattleSeed { get; }
    }
}
