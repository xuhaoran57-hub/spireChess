using System.Collections.Generic;
using SpireChess.Save;

namespace SpireChess.UI.MainMenu
{
    public sealed class HeroSelectionOptionState
    {
        public string HeroId { get; set; }
        public string DisplayName { get; set; }
        public string PassiveName { get; set; }
        public string PassiveDescription { get; set; }
        public string UnlockCondition { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsSelected { get; set; }
    }

    public sealed class MainMenuScreenState
    {
        public bool ContinueEnabled { get; set; }
        public string ContinueSummary { get; set; }
        public string StatusMessage { get; set; }
        public bool StatusIsError { get; set; }
        public RunSaveLoadStatus SaveStatus { get; set; }
        public JournalMenuPage Page { get; set; } = JournalMenuPage.Contents;
        public bool IsInputLocked { get; set; }
        public bool HeroSelectionVisible { get; set; }
        public string SelectedHeroId { get; set; }
        public IReadOnlyList<HeroSelectionOptionState> HeroOptions { get; set; } =
            new List<HeroSelectionOptionState>();
    }
}
