using SpireChess.Save;

namespace SpireChess.UI.MainMenu
{
    public sealed class MainMenuScreenState
    {
        public bool ContinueEnabled { get; set; }
        public string ContinueSummary { get; set; }
        public string StatusMessage { get; set; }
        public bool StatusIsError { get; set; }
        public RunSaveLoadStatus SaveStatus { get; set; }
    }
}
