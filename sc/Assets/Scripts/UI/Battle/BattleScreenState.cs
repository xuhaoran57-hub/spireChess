using System;
using SpireChess.Battle;
using SpireChess.UI;

namespace SpireChess.UI.Battle
{
    public sealed class BattleScreenState
    {
        public string Title { get; set; } = "战斗";
        public string Status { get; set; } = string.Empty;
        public string RoundText { get; set; } = string.Empty;
        public string LogText { get; set; } = string.Empty;
        public CardViewModel[] EnemyCards { get; set; } =
            new CardViewModel[BattleBoardState.SlotCount];
        public CardViewModel[] PlayerCards { get; set; } =
            new CardViewModel[BattleBoardState.SlotCount];
        public BattleButtonState Start { get; set; } = new BattleButtonState();
        public BattleButtonState Speed { get; set; } = new BattleButtonState();
        public BattleButtonState Skip { get; set; } = new BattleButtonState();
        public BattleButtonState Preset { get; set; } = new BattleButtonState();
        public BattleButtonState Reset { get; set; } = new BattleButtonState();
        public BattleButtonState Return { get; set; } = new BattleButtonState();
    }

    public sealed class BattleButtonState
    {
        public string Label { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public bool IsInteractable { get; set; }
    }
}
