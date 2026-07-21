using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Battle;
using SpireChess.UI;

namespace SpireChess.UI.Battle
{
    public static class BattleScreenStateBuilder
    {
        public static BattleScreenState Build(
            BattleBoardState board,
            string title,
            string status,
            IEnumerable<string> log,
            bool isRunBattle,
            bool isRunning,
            bool isResolved,
            float playbackSpeed)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            return new BattleScreenState
            {
                Title = string.IsNullOrWhiteSpace(title) ? "战斗" : title,
                Status = status ?? string.Empty,
                RoundText = ResolveRoundText(log),
                LogText = string.Join("\n", log ?? Enumerable.Empty<string>()),
                EnemyCards = BuildRow(board.Enemy, BattleSide.Enemy),
                PlayerCards = BuildRow(board.Player, BattleSide.Player),
                Start = Button("开始战斗", !isResolved, !isRunning && !isResolved),
                Speed = Button(
                    playbackSpeed > 1f ? "速度 2×" : "速度 1×",
                    true,
                    !isResolved),
                Skip = Button("跳过表现", isRunning, isRunning),
                Preset = Button("切换预设", !isRunBattle, !isRunning && !isResolved),
                Reset = Button("重置", !isRunBattle, !isRunning),
                Return = Button(
                    isRunBattle ? "查看结算" : "返回商店",
                    isResolved,
                    isResolved)
            };
        }

        private static CardViewModel[] BuildRow(
            IReadOnlyList<BattleMinionRuntime> row,
            BattleSide side)
        {
            var cards = new CardViewModel[BattleBoardState.SlotCount];
            for (var index = 0; index < cards.Length; index++)
            {
                if (row[index] != null)
                {
                    cards[index] = BattleCardViewModelFactory.FromRuntime(
                        row[index],
                        side,
                        index);
                }
            }

            return cards;
        }

        private static BattleButtonState Button(
            string label,
            bool visible,
            bool interactable)
        {
            return new BattleButtonState
            {
                Label = label,
                IsVisible = visible,
                IsInteractable = interactable
            };
        }

        private static string ResolveRoundText(IEnumerable<string> log)
        {
            return (log ?? Enumerable.Empty<string>())
                .LastOrDefault(value =>
                    value != null &&
                    value.StartsWith("第 ", StringComparison.Ordinal) &&
                    value.EndsWith(" 轮。", StringComparison.Ordinal)) ??
                   "准备阶段";
        }
    }
}
