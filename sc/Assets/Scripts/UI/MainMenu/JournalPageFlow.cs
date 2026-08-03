namespace SpireChess.UI.MainMenu
{
    /// <summary>
    /// Presentation-only journal navigation. It deliberately owns no run, save,
    /// hero, or unlock state; callers commit those through the existing domain
    /// services before or after a page turn as appropriate.
    /// </summary>
    public enum JournalMenuPage
    {
        Contents = 0,
        Cover = 1,
        HeroSelection = 2,
        Map = 3
    }

    public sealed class JournalPageFlow
    {
        private JournalMenuPage currentPage;
        private JournalMenuPage pendingPage;

        public JournalPageFlow(JournalMenuPage initialPage)
        {
            currentPage = initialPage;
            pendingPage = initialPage;
        }

        public JournalMenuPage CurrentPage => currentPage;
        public JournalMenuPage PendingPage => pendingPage;
        public bool IsInputLocked { get; private set; }

        public bool TryBeginTransition(JournalMenuPage destination)
        {
            if (IsInputLocked || destination == currentPage ||
                !IsAllowedTransition(currentPage, destination))
            {
                return false;
            }

            pendingPage = destination;
            IsInputLocked = true;
            return true;
        }

        public bool CompleteTransition()
        {
            if (!IsInputLocked)
            {
                return false;
            }

            currentPage = pendingPage;
            IsInputLocked = false;
            return true;
        }

        public bool CancelTransition()
        {
            if (!IsInputLocked)
            {
                return false;
            }

            pendingPage = currentPage;
            IsInputLocked = false;
            return true;
        }

        private static bool IsAllowedTransition(
            JournalMenuPage source,
            JournalMenuPage destination)
        {
            return (source == JournalMenuPage.Cover &&
                    destination == JournalMenuPage.Contents) ||
                   (source == JournalMenuPage.Contents &&
                    destination == JournalMenuPage.HeroSelection) ||
                   (source == JournalMenuPage.HeroSelection &&
                    (destination == JournalMenuPage.Contents ||
                     destination == JournalMenuPage.Map));
        }
    }
}
