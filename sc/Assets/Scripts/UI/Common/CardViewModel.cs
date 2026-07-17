using System;

namespace SpireChess.UI
{
    public enum CardDisplayMode
    {
        Full,
        Compact
    }

    public sealed class CardViewModel
    {
        public string InstanceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RaceText { get; set; }
        public string[] AbilityLabels { get; set; } = Array.Empty<string>();
        public string ProgressText { get; set; }
        public string DisabledReason { get; set; }

        public int Tier { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int BaseAttack { get; set; }
        public int BaseHealth { get; set; }
        public int Cost { get; set; }

        public CardDisplayMode DisplayMode { get; set; }
        public bool IsMinion { get; set; }
        public bool ShowCost { get; set; }
        public bool IsGolden { get; set; }
        public bool IsSelected { get; set; }
        public bool IsLegalTarget { get; set; }
        public bool IsInteractable { get; set; }
        public bool IsAffordable { get; set; }
        public bool HasShield { get; set; }
        public bool HasNextCombatShield { get; set; }
        public bool IsTemporary { get; set; }

        public string[] Keywords { get; set; } = Array.Empty<string>();
    }
}
